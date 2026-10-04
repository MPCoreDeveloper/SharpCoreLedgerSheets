using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Accounting;

public sealed class JournalEntryTests
{
    private static readonly DateOnly Date = new(2026, 1, 15);
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();

    [Fact]
    public void New_GeneratesIdentifierAndKeepsData()
    {
        var post = JournalEntry.New(Date, "Testboeking", "REF-1");

        Assert.NotEqual(Guid.Empty, post.Id);
        Assert.Equal(Date, post.Date);
        Assert.Equal("Testboeking", post.Description);
        Assert.Equal("REF-1", post.Reference);
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
        var post = new JournalEntry(Guid.NewGuid(), Date, "Test", "   ");

        Assert.Null(post.Reference);
    }

    [Fact]
    public void AddLine_ThrowsForNegativeAmounts()
    {
        var post = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => post.AddLine(AccountA, -1m, 0m));
    }

    [Fact]
    public void AddLine_ThrowsWhenBothDebitAndCredit()
    {
        var post = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => post.AddLine(AccountA, 10m, 10m));
    }

    [Fact]
    public void AddLine_ThrowsWhenNeitherDebitNorCredit()
    {
        var post = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => post.AddLine(AccountA, 0m, 0m));
    }

    [Fact]
    public void AddLine_ThrowsForEmptyAccountId()
    {
        var post = JournalEntry.New(Date, "Test");

        Assert.Throws<DomainException>(() => post.AddLine(Guid.Empty, 10m, 0m));
    }

    [Fact]
    public void DebitAndCredit_AddLinesAndTotals()
    {
        var post = JournalEntry.New(Date, "Test")
            .Debit(AccountA, 100m)
            .Credit(AccountB, 100m);

        Assert.Equal(100m, post.TotalDebit);
        Assert.Equal(100m, post.TotalCredit);
        Assert.Equal(2, post.Lines.Count);
        Assert.True(post.IsBalanced);
    }

    [Fact]
    public void Lines_AreReadOnly()
    {
        var post = JournalEntry.New(Date, "Test").Debit(AccountA, 1m).Credit(AccountB, 1m);

        Assert.IsAssignableFrom<IReadOnlyList<JournalEntryLine>>(post.Lines);
    }

    [Fact]
    public void IsBalanced_IsFalseForUnbalancedEntry()
    {
        var post = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 90m);

        Assert.False(post.IsBalanced);
    }

    [Fact]
    public void Validate_ThrowsForFewerThanTwoLines()
    {
        var post = JournalEntry.New(Date, "Test").Debit(AccountA, 100m);

        var exception = Assert.Throws<DomainException>(() => post.Validate());

        Assert.Contains("twee regels", exception.Message);
    }

    [Fact]
    public void Validate_ThrowsWhenUnbalanced()
    {
        var post = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 90m);

        var exception = Assert.Throws<DomainException>(() => post.Validate());

        Assert.Contains("niet in balans", exception.Message);
    }

    [Fact]
    public void Validate_AcceptsBalancedEntryWithTwoLines()
    {
        var post = JournalEntry.New(Date, "Test").Debit(AccountA, 100m).Credit(AccountB, 100m);

        var exception = Record.Exception(() => post.Validate());

        Assert.Null(exception);
    }
}
