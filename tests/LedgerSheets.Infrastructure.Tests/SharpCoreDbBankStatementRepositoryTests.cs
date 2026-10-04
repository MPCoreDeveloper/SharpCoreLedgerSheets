using System.Globalization;
using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Infrastructure.Tests;

/// <summary>
/// Tests for the SharpCoreDB bank statement store: a movement is written once and only ever changes
/// status afterwards, and both have to survive a restart.
/// </summary>
public sealed class SharpCoreDbBankStatementRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddAsync_And_AllAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();
        var transaction = Transaction();

        await context.BankStatement.AddAsync([transaction], Ct);

        var stored = Assert.Single(await context.BankStatement.AllAsync(ct: Ct));
        Assert.Equal(transaction.Id, stored.Id);
        Assert.Equal(transaction.BankAccountNumber, stored.BankAccountNumber);
        Assert.Equal(transaction.Date, stored.Date);
        Assert.Equal(-12.50m, stored.Movement.Value);
        Assert.Equal(transaction.Movement.Currency, stored.Movement.Currency);
        Assert.Equal(transaction.Reference, stored.Reference);
        Assert.Equal(transaction.CounterpartyName, stored.CounterpartyName);
        Assert.Equal(transaction.CounterpartyIban, stored.CounterpartyIban);
        Assert.Equal(transaction.Description, stored.Description);
        Assert.Equal(BankTransactionStatus.Open, stored.Status);
        Assert.Null(stored.JournalEntryId);
        Assert.Equal(transaction.Key, stored.Key);
    }

    [Fact]
    public async Task AllAsync_OrdersByDate()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.BankStatement.AddAsync(
            [
                Transaction("REF-B", date: "2026-04-03"),
                Transaction("REF-C", date: "2026-04-05"),
                Transaction("REF-A", date: "2026-04-02"),
            ],
            Ct);

        var transactions = await context.BankStatement.AllAsync(ct: Ct);

        Assert.Collection(
            transactions,
            transaction => Assert.Equal("REF-A", transaction.Reference),
            transaction => Assert.Equal("REF-B", transaction.Reference),
            transaction => Assert.Equal("REF-C", transaction.Reference));
    }

    [Fact]
    public async Task AllAsync_FiltersOnStatus()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.BankStatement.AddAsync(
            [
                Transaction("REF-OPEN"),
                Transaction("REF-GEBOEKT").MarkPosted(Guid.NewGuid()),
                Transaction("REF-NEGEER").Ignore(),
            ],
            Ct);

        var open = await context.BankStatement.AllAsync(BankTransactionStatus.Open, Ct);

        var transaction = Assert.Single(open);
        Assert.Equal("REF-OPEN", transaction.Reference);
        Assert.Equal(3, (await context.BankStatement.AllAsync(ct: Ct)).Count);
    }

    [Fact]
    public async Task UpdateAsync_PersistsTheBooking()
    {
        await using var context = new SharpCoreDbTestContext();
        var transaction = Transaction();
        await context.BankStatement.AddAsync([transaction], Ct);
        var postId = Guid.NewGuid();

        await context.BankStatement.UpdateAsync(transaction.MarkPosted(postId), Ct);

        var stored = Assert.Single(await context.BankStatement.AllAsync(BankTransactionStatus.Posted, Ct));
        Assert.Equal(postId, stored.JournalEntryId);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAnIgnoredMovement()
    {
        await using var context = new SharpCoreDbTestContext();
        var transaction = Transaction().MarkPosted(Guid.NewGuid());
        await context.BankStatement.AddAsync([transaction], Ct);

        await context.BankStatement.UpdateAsync(transaction.Ignore(), Ct);

        var stored = Assert.Single(await context.BankStatement.AllAsync(BankTransactionStatus.Ignored, Ct));
        Assert.Null(stored.JournalEntryId);
    }

    [Fact]
    public async Task FindAsync_FindsTheMovement()
    {
        await using var context = new SharpCoreDbTestContext();
        var transaction = Transaction();
        await context.BankStatement.AddAsync([transaction], Ct);

        var found = await context.BankStatement.FindAsync(transaction.Id, Ct);

        Assert.NotNull(found);
        Assert.Equal(transaction.Id, found.Id);
        Assert.Equal(transaction.Reference, found.Reference);
    }

    [Fact]
    public async Task FindAsync_ReturnsNullWhenMissing()
    {
        await using var context = new SharpCoreDbTestContext();

        Assert.Null(await context.BankStatement.FindAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task ExistingKeysAsync_ReturnsTheImportKeys()
    {
        await using var context = new SharpCoreDbTestContext();
        var first = Transaction("REF-1");
        var second = Transaction("REF-2");
        await context.BankStatement.AddAsync([first, second], Ct);

        var keys = await context.BankStatement.ExistingKeysAsync(Ct);

        Assert.Equal(2, keys.Count);
        Assert.Contains(first.Key, keys);
        Assert.Contains(second.Key, keys);
    }

    [Fact]
    public async Task AddAsync_IgnoresAnEmptyBatch()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.BankStatement.AddAsync([], Ct);

        Assert.Empty(await context.BankStatement.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task Movements_SurviveReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        var postId = Guid.NewGuid();
        await context.BankStatement.AddAsync([Transaction().MarkPosted(postId)], Ct);

        await context.HeropenAsync();

        var stored = Assert.Single(await context.BankStatement.AllAsync(BankTransactionStatus.Posted, Ct));
        Assert.Equal(postId, stored.JournalEntryId);
        Assert.Equal("Albert Heijn", stored.CounterpartyName);
    }

    [Fact]
    public async Task AddAsync_ThrowsForNull()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.BankStatement.AddAsync(null!, Ct));
    }

    private static BankTransaction Transaction(
        string reference = "REF-1",
        decimal amount = -12.50m,
        string date = "2026-04-02",
        string counterparty = "Albert Heijn",
        string iban = "NL02ABNA0123456789",
        string description = "Boodschappen week 14") =>
        new(
            Guid.NewGuid(),
            "1100",
            DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            new Amount(amount, Currency.Eur),
            reference,
            counterparty,
            iban,
            description);
}
