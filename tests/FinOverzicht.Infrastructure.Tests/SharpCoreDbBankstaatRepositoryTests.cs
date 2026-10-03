using System.Globalization;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Infrastructure.Tests;

/// <summary>
/// Tests for the SharpCoreDB bank statement store: a movement is written once and only ever changes
/// status afterwards, and both have to survive a restart.
/// </summary>
public sealed class SharpCoreDbBankstaatRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ToevoegenAsync_And_AlleAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();
        var transactie = Transactie();

        await context.Bankstaat.ToevoegenAsync([transactie], Ct);

        var opgeslagen = Assert.Single(await context.Bankstaat.AlleAsync(ct: Ct));
        Assert.Equal(transactie.Id, opgeslagen.Id);
        Assert.Equal(transactie.BankRekeningNummer, opgeslagen.BankRekeningNummer);
        Assert.Equal(transactie.Datum, opgeslagen.Datum);
        Assert.Equal(-12.50m, opgeslagen.Mutatie.Waarde);
        Assert.Equal(transactie.Mutatie.Valuta, opgeslagen.Mutatie.Valuta);
        Assert.Equal(transactie.Referentie, opgeslagen.Referentie);
        Assert.Equal(transactie.TegenpartijNaam, opgeslagen.TegenpartijNaam);
        Assert.Equal(transactie.TegenpartijIban, opgeslagen.TegenpartijIban);
        Assert.Equal(transactie.Omschrijving, opgeslagen.Omschrijving);
        Assert.Equal(BankTransactieStatus.Open, opgeslagen.Status);
        Assert.Null(opgeslagen.JournaalpostId);
        Assert.Equal(transactie.Sleutel, opgeslagen.Sleutel);
    }

    [Fact]
    public async Task AlleAsync_OrdersByDate()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Bankstaat.ToevoegenAsync(
            [
                Transactie("REF-B", datum: "2026-04-03"),
                Transactie("REF-C", datum: "2026-04-05"),
                Transactie("REF-A", datum: "2026-04-02"),
            ],
            Ct);

        var transacties = await context.Bankstaat.AlleAsync(ct: Ct);

        Assert.Collection(
            transacties,
            transactie => Assert.Equal("REF-A", transactie.Referentie),
            transactie => Assert.Equal("REF-B", transactie.Referentie),
            transactie => Assert.Equal("REF-C", transactie.Referentie));
    }

    [Fact]
    public async Task AlleAsync_FiltersOnStatus()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Bankstaat.ToevoegenAsync(
            [
                Transactie("REF-OPEN"),
                Transactie("REF-GEBOEKT").MarkeerGeboekt(Guid.NewGuid()),
                Transactie("REF-NEGEER").Negeer(),
            ],
            Ct);

        var open = await context.Bankstaat.AlleAsync(BankTransactieStatus.Open, Ct);

        var transactie = Assert.Single(open);
        Assert.Equal("REF-OPEN", transactie.Referentie);
        Assert.Equal(3, (await context.Bankstaat.AlleAsync(ct: Ct)).Count);
    }

    [Fact]
    public async Task BijwerkenAsync_PersistsTheBooking()
    {
        await using var context = new SharpCoreDbTestContext();
        var transactie = Transactie();
        await context.Bankstaat.ToevoegenAsync([transactie], Ct);
        var postId = Guid.NewGuid();

        await context.Bankstaat.BijwerkenAsync(transactie.MarkeerGeboekt(postId), Ct);

        var opgeslagen = Assert.Single(await context.Bankstaat.AlleAsync(BankTransactieStatus.Geboekt, Ct));
        Assert.Equal(postId, opgeslagen.JournaalpostId);
    }

    [Fact]
    public async Task BijwerkenAsync_PersistsAnIgnoredMovement()
    {
        await using var context = new SharpCoreDbTestContext();
        var transactie = Transactie().MarkeerGeboekt(Guid.NewGuid());
        await context.Bankstaat.ToevoegenAsync([transactie], Ct);

        await context.Bankstaat.BijwerkenAsync(transactie.Negeer(), Ct);

        var opgeslagen = Assert.Single(await context.Bankstaat.AlleAsync(BankTransactieStatus.Genegeerd, Ct));
        Assert.Null(opgeslagen.JournaalpostId);
    }

    [Fact]
    public async Task ZoekAsync_FindsTheMovement()
    {
        await using var context = new SharpCoreDbTestContext();
        var transactie = Transactie();
        await context.Bankstaat.ToevoegenAsync([transactie], Ct);

        var gevonden = await context.Bankstaat.ZoekAsync(transactie.Id, Ct);

        Assert.NotNull(gevonden);
        Assert.Equal(transactie.Id, gevonden.Id);
        Assert.Equal(transactie.Referentie, gevonden.Referentie);
    }

    [Fact]
    public async Task ZoekAsync_ReturnsNullWhenMissing()
    {
        await using var context = new SharpCoreDbTestContext();

        Assert.Null(await context.Bankstaat.ZoekAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task BestaandeSleutelsAsync_ReturnsTheImportKeys()
    {
        await using var context = new SharpCoreDbTestContext();
        var eerste = Transactie("REF-1");
        var tweede = Transactie("REF-2");
        await context.Bankstaat.ToevoegenAsync([eerste, tweede], Ct);

        var sleutels = await context.Bankstaat.BestaandeSleutelsAsync(Ct);

        Assert.Equal(2, sleutels.Count);
        Assert.Contains(eerste.Sleutel, sleutels);
        Assert.Contains(tweede.Sleutel, sleutels);
    }

    [Fact]
    public async Task ToevoegenAsync_IgnoresAnEmptyBatch()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.Bankstaat.ToevoegenAsync([], Ct);

        Assert.Empty(await context.Bankstaat.AlleAsync(ct: Ct));
    }

    [Fact]
    public async Task Movements_SurviveReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        var postId = Guid.NewGuid();
        await context.Bankstaat.ToevoegenAsync([Transactie().MarkeerGeboekt(postId)], Ct);

        await context.HeropenAsync();

        var opgeslagen = Assert.Single(await context.Bankstaat.AlleAsync(BankTransactieStatus.Geboekt, Ct));
        Assert.Equal(postId, opgeslagen.JournaalpostId);
        Assert.Equal("Albert Heijn", opgeslagen.TegenpartijNaam);
    }

    [Fact]
    public async Task ToevoegenAsync_ThrowsForNull()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Bankstaat.ToevoegenAsync(null!, Ct));
    }

    private static BankTransactie Transactie(
        string referentie = "REF-1",
        decimal bedrag = -12.50m,
        string datum = "2026-04-02",
        string tegenpartij = "Albert Heijn",
        string iban = "NL02ABNA0123456789",
        string omschrijving = "Boodschappen week 14") =>
        new(
            Guid.NewGuid(),
            "1100",
            DateOnly.ParseExact(datum, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            new Bedrag(bedrag, Valuta.Eur),
            referentie,
            tegenpartij,
            iban,
            omschrijving);
}
