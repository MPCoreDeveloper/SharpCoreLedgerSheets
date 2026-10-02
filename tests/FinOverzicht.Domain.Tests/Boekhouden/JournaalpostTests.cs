using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

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

        Assert.NotEqual(Guid.Empty, post.Id);
        Assert.Equal(Datum, post.Datum);
        Assert.Equal("Testboeking", post.Omschrijving);
        Assert.Equal("REF-1", post.Referentie);
    }

    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        Assert.Throws<DomeinException>(() => new Journaalpost(Guid.Empty, Datum, "Test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankDescription(string omschrijving)
    {
        Assert.Throws<DomeinException>(() => new Journaalpost(Guid.NewGuid(), Datum, omschrijving));
    }

    [Fact]
    public void Constructor_NormalisesBlankReferenceToNull()
    {
        var post = new Journaalpost(Guid.NewGuid(), Datum, "Test", "   ");

        Assert.Null(post.Referentie);
    }

    [Fact]
    public void VoegRegelToe_ThrowsForNegativeAmounts()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        Assert.Throws<DomeinException>(() => post.VoegRegelToe(RekeningA, -1m, 0m));
    }

    [Fact]
    public void VoegRegelToe_ThrowsWhenBothDebitAndCredit()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        Assert.Throws<DomeinException>(() => post.VoegRegelToe(RekeningA, 10m, 10m));
    }

    [Fact]
    public void VoegRegelToe_ThrowsWhenNeitherDebitNorCredit()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        Assert.Throws<DomeinException>(() => post.VoegRegelToe(RekeningA, 0m, 0m));
    }

    [Fact]
    public void VoegRegelToe_ThrowsForEmptyAccountId()
    {
        var post = Journaalpost.Nieuw(Datum, "Test");

        Assert.Throws<DomeinException>(() => post.VoegRegelToe(Guid.Empty, 10m, 0m));
    }

    [Fact]
    public void DebetAndCredit_AddLinesAndTotals()
    {
        var post = Journaalpost.Nieuw(Datum, "Test")
            .Debet(RekeningA, 100m)
            .Credit(RekeningB, 100m);

        Assert.Equal(100m, post.TotaalDebet);
        Assert.Equal(100m, post.TotaalCredit);
        Assert.Equal(2, post.Regels.Count);
        Assert.True(post.IsInBalans);
    }

    [Fact]
    public void Regels_AreReadOnly()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 1m).Credit(RekeningB, 1m);

        Assert.IsAssignableFrom<IReadOnlyList<JournaalpostRegel>>(post.Regels);
    }

    [Fact]
    public void IsInBalans_IsFalseForUnbalancedEntry()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 90m);

        Assert.False(post.IsInBalans);
    }

    [Fact]
    public void Valideer_ThrowsForFewerThanTwoLines()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m);

        var exceptie = Assert.Throws<DomeinException>(() => post.Valideer());

        Assert.Contains("twee regels", exceptie.Message);
    }

    [Fact]
    public void Valideer_ThrowsWhenUnbalanced()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 90m);

        var exceptie = Assert.Throws<DomeinException>(() => post.Valideer());

        Assert.Contains("niet in balans", exceptie.Message);
    }

    [Fact]
    public void Valideer_AcceptsBalancedEntryWithTwoLines()
    {
        var post = Journaalpost.Nieuw(Datum, "Test").Debet(RekeningA, 100m).Credit(RekeningB, 100m);

        var exceptie = Record.Exception(() => post.Valideer());

        Assert.Null(exceptie);
    }
}
