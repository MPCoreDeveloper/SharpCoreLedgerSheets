using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Domain.Tests.Accounting;

public sealed class JournalEntryTests
{
    private static readonly DateOnly Date = new(2026, 1, 15);
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();

    [Fact]
    public void New_GeneratesIdentifierAndKeepsData()
    {
        var entry = JournalEntry.New(Date, "Testboeking", "REF-1");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(Date, entry.Date);
        Assert.Equal("Testboeking", entry.Description);
        Assert.Equal("REF-1", entry.Reference);
    }

    [Fact]
    public void Constructor_ThrowsForEmptyId()
    {
        Assert.Throws<DomainException>(() => new JournalEntry(Guid.Empty, Date, "Test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsForBlankDescription(string description)
    {
        Assert.Throws<DomainException>(() => new JournalEntry(Guid.NewGuid(), Date, description));
    }

    [Fact]
    public void Constructor_NormalisesBlankReferenceToNull()
    {
        var entry = new JournalEntry(Guid.NewGuid(), Date, "Test", "   ");

        Assert.Null(entry.Reference);
    }

    [Fact]
    public void AddLine_ThrowsForNegativeAmounts()
    {
        var entry = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => entry.AddLine(AccountA, -1m, 0m));
    }

    [Fact]
    public void AddLine_ThrowsWhenBothDebitAndCredit()
    {
        var entry = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => entry.AddLine(AccountA, 10m, 10m));
    }

    [Fact]
    public void AddLine_ThrowsWhenNeitherDebitNorCredit()
    {
        var entry = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => entry.AddLine(AccountA, 0m, 0m));
    }

    [Fact]
    public void AddLine_ThrowsForEmptyAccountId()
    {
        var entry = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => entry.AddLine(Guid.Empty, 10m, 0m));
    }

    [Fact]
    public void DebitAndCredit_AddLinesAndTotals()
    {
        var entry = JournalEntry.New(Date, "Test")
            .Debit(AccountA, 100m)
            .Credit(AccountB, 100m);

        Assert.Equal(100m, entry.TotalDebit);
        Assert.Equal(100m, entry.TotalCredit);
        Assert.Equal(2, entry.Lines.Count);
        Assert.True(entry.IsBalanced);
    }

    [Fact]
    public void Lines_AreReadOnly()
    {
        var entry = JournalEntry.New(Date, "Test").Debit(AccountA, 1m).Credit(AccountB, 1m);

        Assert.IsAssignableFrom<IReadOnlyList<JournalEntryLine>>(entry.Lines);
    }

    [Fact]
    public void IsBalanced_IsFalseForUnbalancedEntry()
    {
        var entry = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 90m);

        Assert.False(entry.IsBalanced);
    }

    [Fact]
    public void Validate_ThrowsForFewerThanTwoLines()
    {
        var entry = JournalEntry.New(Date, "Test").Debit(AccountA, 100m);

        var exception = Assert.Throws<DomainException>(() => entry.Validate());

        Assert.Contains("twee regels", exception.Message);
    }

    [Fact]
    public void Validate_ThrowsWhenUnbalanced()
    {
        var entry = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 90m);

        var exception = Assert.Throws<DomainException>(() => entry.Validate());

        Assert.Contains("niet in balans", exception.Message);
    }

    [Fact]
    public void Validate_AcceptsBalancedEntryWithTwoLines()
    {
        var entry = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 100m);

        var exception = Record.Exception(() => entry.Validate());

        Assert.Null(exception);
    }
}
