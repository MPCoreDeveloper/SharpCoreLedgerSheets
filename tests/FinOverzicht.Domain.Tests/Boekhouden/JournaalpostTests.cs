using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FluentAssertions;

namespace FinOverzicht.Domain.Tests.Boekhouden;

public sealed class JournaalpostTests
{
    private static readonly DateOnly Datum = new(2026, 1, 15);
    private static readonly Guid RekeningA = Guid.NewGuid();
    private static readonly Guid RekeningB = Guid.NewGuid();

    [Fact]
    public void Nieuw_GeneratesIdentifierAndKeepsData()
    {
        var post = Journaalpost.Nieuw(Datum, "Testboeking", "REF-1");

        post.Id.Should().NotBe(Guid.Empty);
        post.Datum.Should().Be(Datum);
        post.Omschrijving.Should().Be("Testboeking");
        post.Referentie.Should().Be("REF-1");
    }

    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        var act = () => new Journaalpost(Guid.Empty, Datum, "Test");

        act.Should().Throw<DomeinException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankDescription(string omschrijving)
    {
        var act = () => new Journaalpost(Guid.NewGuid(), Datum, omschrijving);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void Constructor_NormalisesBlankReferenceToNull()
    {
        var post = new Journaalpost(Guid.NewGuid(), Datum, "Test", "   ");

        post.Referentie.Should().BeNull();
    }

    [Fact]
    public void VoegRegelToe_ThrowsForNegativeAmounts()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        var act = () => post.VoegRegelToe(RekeningA, -1m, 0m);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void VoegRegelToe_ThrowsWhenBothDebitAndCredit()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        var act = () => post.VoegRegelToe(RekeningA, 10m, 10m);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void VoegRegelToe_ThrowsWhenNeitherDebitNorCredit()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        var act = () => post.VoegRegelToe(RekeningA, 0m, 0m);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void VoegRegelToe_ThrowsForEmptyAccountId()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        var act = () => post.VoegRegelToe(Guid.Empty, 10m, 0m);

        act.Should().Throw<DomeinException>();
    }

    [Fact]
    public void DebetAndCredit_AddLinesAndTotals()
    {
        var post = Journaalpost.Nieuw(Datum, "Test")
            .Debet(RekeningA, 100m)
            .Credit(RekeningB, 100m);

        post.TotaalDebet.Should().Be(100m);
        post.TotaalCredit.Should().Be(100m);
        post.Regels.Should().HaveCount(2);
        post.IsInBalans.Should().BeTrue();
    }

    [Fact]
    public void Regels_AreReadOnly()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 1m).Credit(RekeningB, 1m);

        post.Regels.Should().BeAssignableTo<IReadOnlyList<JournaalpostRegel>>();
    }

    [Fact]
    public void IsInBalans_IsFalseForUnbalancedEntry()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 90m);

        post.IsInBalans.Should().BeFalse();
    }

    [Fact]
    public void Valideer_ThrowsForFewerThanTwoLines()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m);

        var act = () => post.Valideer();

        act.Should().Throw<DomeinException>().WithMessage("*twee regels*");
    }

    [Fact]
    public void Valideer_ThrowsWhenUnbalanced()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 90m);

        var act = () => post.Valideer();

        act.Should().Throw<DomeinException>().WithMessage("*niet in balans*");
    }

    [Fact]
    public void Valideer_AcceptsBalancedEntryWithTwoLines()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 100m);

        var act = () => post.Valideer();

        act.Should().NotThrow();
    }
}
