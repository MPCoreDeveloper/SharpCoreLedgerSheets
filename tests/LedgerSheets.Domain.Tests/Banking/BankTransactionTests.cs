using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Banking;

public sealed class BankTransactionTests
{
    private static readonly DateOnly Date = new(2026, 4, 2);

    [Fact]
    public void New_CreatesAnOpenTransaction()
    {
        var transaction = Create();

        Assert.Equal(BankTransactionStatus.Open, transaction.Status);
        Assert.Null(transaction.JournalEntryId);
        Assert.True(transaction.IsDepreciation);
        Assert.False(transaction.IsCredit);
        Assert.Equal(12.50m, transaction.AmountWithoutSign.Value);
    }

    [Fact]
    public void Credit_IsPositive()
    {
        var transaction = Create(amount: 250m);

        Assert.True(transaction.IsCredit);
        Assert.False(transaction.IsDepreciation);
        Assert.Equal(250m, transaction.AmountWithoutSign.Value);
        Assert.Equal(Currency.Eur, transaction.AmountWithoutSign.Currency);
    }

    [Fact]
    public void WithoutId_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => Create(id: Guid.Empty));

        Assert.Contains("uniek id", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithoutBankAccount_IsRejected(string bankAccount)
    {
        var exception = Assert.Throws<DomainException>(() => Create(bankAccount: bankAccount));

        Assert.Contains("grootboekrekening van de bank", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithoutReference_IsRejected(string reference)
    {
        var exception = Assert.Throws<DomainException>(() => Create(reference: reference));

        Assert.Contains("referentie", exception.Message);
    }

    [Fact]
    public void WithoutAmount_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => Create(amount: 0m));

        Assert.Contains("zonder bedrag", exception.Message);
    }

    [Fact]
    public void Key_IsCaseInsensitive()
    {
        var first = Create(bankAccount: "1100", reference: "abc-123");
        var second = Create(bankAccount: "1100", reference: "ABC-123");

        Assert.Equal(first.Key, second.Key);
    }

    [Fact]
    public void Key_DiffersPerBankAccount()
    {
        var first = Create(bankAccount: "1100");
        var second = Create(bankAccount: "1200");

        Assert.NotEqual(first.Key, second.Key);
    }

    [Fact]
    public void SearchText_NormalizesCounterpartyIbanAndDescription()
    {
        var transaction = Create();

        Assert.Equal("ALBERT HEIJN NL91ABNA0417164300 BOODSCHAPPEN", transaction.SearchText);
    }

    [Fact]
    public void CounterpartyAndDescription_AreTrimmed()
    {
        var transaction = Create(name: "  Albert Heijn  ", description: "   ");

        Assert.Equal("Albert Heijn", transaction.CounterpartyName);
        Assert.Null(transaction.Description);
    }

    [Fact]
    public void MarkPosted_SetStatusAndJournalEntry()
    {
        var post = Guid.NewGuid();

        var posted = Create().MarkPosted(post);

        Assert.Equal(BankTransactionStatus.Posted, posted.Status);
        Assert.Equal(post, posted.JournalEntryId);
    }

    [Fact]
    public void MarkPosted_LeavesTheOriginalUnchanged()
    {
        var transaction = Create();

        _ = transaction.MarkPosted(Guid.NewGuid());

        Assert.Equal(BankTransactionStatus.Open, transaction.Status);
        Assert.Null(transaction.JournalEntryId);
    }

    [Fact]
    public void MarkPosted_WithoutJournalEntry_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => Create().MarkPosted(Guid.Empty));

        Assert.Contains("journaalpost", exception.Message);
    }

    [Fact]
    public void Ignore_SetStatusAndErasesTheJournalEntry()
    {
        var ignored = Create().MarkPosted(Guid.NewGuid()).Ignore();

        Assert.Equal(BankTransactionStatus.Ignored, ignored.Status);
        Assert.Null(ignored.JournalEntryId);
    }

    private static BankTransaction Create(
        Guid? id = null,
        string bankAccount = "1100",
        decimal amount = -12.50m,
        string reference = "BANK-2026-0001",
        string? name = "Albert Heijn",
        string? iban = "NL91ABNA0417164300",
        string? description = "Boodschappen") =>
        new(
            id ?? Guid.NewGuid(),
            bankAccount,
            Date,
            new Amount(amount, Currency.Eur),
            reference,
            name,
            iban,
            description);
}
