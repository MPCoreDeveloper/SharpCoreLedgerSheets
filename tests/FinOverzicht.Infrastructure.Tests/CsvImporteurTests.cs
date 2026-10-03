using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FinOverzicht.Infrastructure.Bankzaken;

namespace FinOverzicht.Infrastructure.Tests;

/// <summary>
/// Tests for <see cref="CsvImporteur"/>: the reader recognises columns by their header names, so it
/// has to cope with the many header spellings, delimiters and number notations banks use.
/// </summary>
public sealed class CsvImporteurTests
{
    private const string Bankrekening = "1100";

    private static readonly CsvImporteur Importeur = new();

    [Fact]
    public void Formaat_IsCsv() => Assert.Equal(BankstaatFormaat.Csv, Importeur.Formaat);

    [Theory]
    [InlineData("afschrift.csv")]
    [InlineData("AFSCHRIFT.CSV")]
    [InlineData("afschrift.txt")]
    public void KanVerwerken_RecognisesDelimitedFiles(string bestandsnaam) =>
        Assert.True(Importeur.KanVerwerken(bestandsnaam, "Datum;Bedrag\r\n02-04-2026;12,50"));

    [Fact]
    public void KanVerwerken_RejectsFilesWithoutContent()
    {
        Assert.False(Importeur.KanVerwerken("afschrift.csv", string.Empty));
        Assert.False(Importeur.KanVerwerken("afschrift.xlsx", "Datum;Bedrag\r\n02-04-2026;12,50"));
        Assert.False(Importeur.KanVerwerken("afschrift.xml", "<Document><BkToCstmrStmt/></Document>"));
    }

    [Fact]
    public void Lees_ReadsADutchSemicolonExport()
    {
        var inhoud = Csv(
            "Datum;Bedrag;Tegenpartij;IBAN;Omschrijving;Referentie",
            "02-04-2026;-12,50;Albert Heijn;NL02ABNA0123456789;Boodschappen week 14;REF-1",
            "03-04-2026;1.234,56;Klant BV;NL91ABNA0417164300;Factuur 2026-014;REF-2");

        var transacties = Importeur.Lees(inhoud, Bankrekening);

        Assert.Equal(2, transacties.Count);

        var betaling = transacties[0];
        Assert.Equal(new DateOnly(2026, 4, 2), betaling.Datum);
        Assert.Equal(-12.50m, betaling.Mutatie.Waarde);
        Assert.Equal(Valuta.Eur, betaling.Mutatie.Valuta);
        Assert.Equal("Albert Heijn", betaling.TegenpartijNaam);
        Assert.Equal("NL02ABNA0123456789", betaling.TegenpartijIban);
        Assert.Equal("Boodschappen week 14", betaling.Omschrijving);
        Assert.Equal("REF-1", betaling.Referentie);
        Assert.Equal(Bankrekening, betaling.BankRekeningNummer);
        Assert.Equal(BankTransactieStatus.Open, betaling.Status);

        var ontvangst = transacties[1];
        Assert.Equal(new DateOnly(2026, 4, 3), ontvangst.Datum);
        Assert.Equal(1234.56m, ontvangst.Mutatie.Waarde);
        Assert.True(ontvangst.IsBijschrijving);
        Assert.Equal("REF-2", ontvangst.Referentie);
    }

    [Fact]
    public void Lees_ReadsAnEnglishCommaExportWithQuotedFields()
    {
        var inhoud = Csv(
            "Date,Amount,Counterparty,Description,Reference",
            "2026-04-02,\"1,234.56\",\"Klant, BV\",\"Zeg \"\"hallo\"\"\",REF-2");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(new DateOnly(2026, 4, 2), transactie.Datum);
        Assert.Equal(1234.56m, transactie.Mutatie.Waarde);
        Assert.Equal("Klant, BV", transactie.TegenpartijNaam);
        Assert.Equal("Zeg \"hallo\"", transactie.Omschrijving);
        Assert.Equal("REF-2", transactie.Referentie);
    }

    [Fact]
    public void Lees_ReadsATabSeparatedExport()
    {
        var inhoud = Csv(
            "Datum\tBedrag\tTegenpartij",
            "02-04-2026\t-12,50\tAlbert Heijn");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
        Assert.Equal("Albert Heijn", transactie.TegenpartijNaam);
    }

    [Fact]
    public void Lees_RecognisesHeaderNamesInAnyForm()
    {
        var inhoud = Csv(
            "BEDRAG (EUR);Boekdatum;Naam tegenpartij;Mededelingen",
            "-12,50;02-04-2026;Albert Heijn;Boodschappen");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(new DateOnly(2026, 4, 2), transactie.Datum);
        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
        Assert.Equal("Albert Heijn", transactie.TegenpartijNaam);
        Assert.Equal("Boodschappen", transactie.Omschrijving);
    }

    [Fact]
    public void Lees_UsesTheDirectionColumnForTheSign()
    {
        var inhoud = Csv(
            "Datum;Bedrag;Af/Bij;Tegenpartij",
            "02-04-2026;12,50;Af;Albert Heijn",
            "03-04-2026;1.234,56;Bij;Klant BV");

        var transacties = Importeur.Lees(inhoud, Bankrekening);

        Assert.Collection(
            transacties,
            betaling => Assert.Equal(-12.50m, betaling.Mutatie.Waarde),
            ontvangst => Assert.Equal(1234.56m, ontvangst.Mutatie.Waarde));
    }

    [Fact]
    public void Lees_ReadsThousandsSeparatorWithoutDecimals()
    {
        var inhoud = Csv(
            "Datum;Bedrag",
            "02-04-2026;1.234");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(1234m, transactie.Mutatie.Waarde);
    }

    [Theory]
    [InlineData("2026-04-02")]
    [InlineData("02-04-2026")]
    [InlineData("2-4-2026")]
    [InlineData("02/04/2026")]
    [InlineData("20260402")]
    public void Lees_ReadsTheDateNotationsBanksUse(string datum)
    {
        var inhoud = Csv("Datum;Bedrag", $"{datum};-12,50");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(new DateOnly(2026, 4, 2), transactie.Datum);
    }

    [Fact]
    public void Lees_SkipsRowsWithoutADate()
    {
        var inhoud = Csv(
            "Datum;Bedrag;Tegenpartij",
            "02-04-2026;-12,50;Albert Heijn",
            ";1.234,56;Totaal");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));

        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
    }

    [Fact]
    public void Lees_DerivesAStableReferenceWhenTheColumnIsMissing()
    {
        var inhoud = Csv(
            "Datum;Bedrag;Tegenpartij;Omschrijving",
            "02-04-2026;-12,50;Albert Heijn;Boodschappen week 14",
            "02-04-2026;-12,50;Albert Heijn;Boodschappen week 14");

        var transacties = Importeur.Lees(inhoud, Bankrekening);

        Assert.Equal(2, transacties.Count);
        Assert.StartsWith("SYN-", transacties[0].Referentie, StringComparison.Ordinal);
        Assert.Equal(transacties[0].Referentie, transacties[1].Referentie);
    }

    [Fact]
    public void Lees_UsesTheStandardCurrencyWhenTheFileOmitsIt()
    {
        var inhoud = Csv("Datum;Bedrag", "02-04-2026;-12,50");

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening, Valuta.Usd));

        Assert.Equal(Valuta.Usd, transactie.Mutatie.Valuta);
    }

    [Fact]
    public void Lees_ThrowsWhenTheAmountColumnIsMissing()
    {
        var inhoud = Csv("Datum;Omschrijving", "02-04-2026;Boodschappen");

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(inhoud, Bankrekening));

        Assert.Contains("Bedrag", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsWhenTheDateColumnIsMissing()
    {
        var inhoud = Csv("Bedrag;Omschrijving", "-12,50;Boodschappen");

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(inhoud, Bankrekening));

        Assert.Contains("Datum", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsForAnUnreadableAmountAndNamesTheLine()
    {
        var inhoud = Csv(
            string.Empty,
            "Datum;Bedrag",
            "02-04-2026;12,50",
            string.Empty,
            "03-04-2026;twaalf");

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(inhoud, Bankrekening));

        Assert.Contains("Regel 5", fout.Message, StringComparison.Ordinal);
        Assert.Contains("twaalf", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsWhenNoRowIsAMovement()
    {
        var inhoud = Csv("Datum;Bedrag", ";1.234,56");

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(inhoud, Bankrekening));

        Assert.Contains("geen bankmutaties", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsForBlankArguments()
    {
        Assert.Throws<ArgumentException>(() => Importeur.Lees(string.Empty, Bankrekening));
        Assert.Throws<ArgumentException>(() => Importeur.Lees("Datum;Bedrag", "  "));
    }

    private static string Csv(params string[] regels) => string.Join('\n', regels);
}
