using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Infrastructure.Tests;

public sealed class SharpCoreDbJournalEntryRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static JournalEntry CreatePost(Guid cash, Guid revenue, DateOnly date, decimal amount, string description, string? reference = null) =>
        JournalEntry.New(date, description, reference)
            .Debit(cash, amount)
            .Credit(revenue, amount);

    [Fact]
    public async Task AddAsync_And_AllAsync_RoundTripEntryWithLines()
    {
        await using var context = new SharpCoreDbTestContext();
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var entry = CreatePost(cash, revenue, new DateOnly(2026, 3, 1), 1234.56m, "Contante verkoop", "F-2026-0001");

        await context.Entries.AddAsync(entry, Ct);
        var entries = await context.Entries.AllAsync(null, Ct);

        var read = Assert.Single(entries);
        Assert.Equal(entry.Id, read.Id);
        Assert.Equal(new DateOnly(2026, 3, 1), read.Date);
        Assert.Equal("Contante verkoop", read.Description);
        Assert.Equal("F-2026-0001", read.Reference);
        Assert.Equal(2, read.Lines.Count);
        Assert.Equal(1234.56m, read.TotalDebit);
        Assert.Equal(1234.56m, read.TotalCredit);
        Assert.Equal(cash, read.Lines[0].AccountId);
        Assert.Equal(1234.56m, read.Lines[0].Debit);
        Assert.Equal(revenue, read.Lines[1].AccountId);
        Assert.Equal(1234.56m, read.Lines[1].Credit);
    }

    [Fact]
    public async Task AddAsync_StoresBlankReferenceAsNull()
    {
        await using var context = new SharpCoreDbTestContext();
        var entry = CreatePost(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), 10m, "Zonder referentie");

        await context.Entries.AddAsync(entry, Ct);
        var entries = await context.Entries.AllAsync(null, Ct);

        Assert.Null(entries[0].Reference);
    }

    [Fact]
    public async Task AllAsync_FiltersByPeriod()
    {
        await using var context = new SharpCoreDbTestContext();
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        await context.Entries.AddAsync(CreatePost(cash, revenue, new DateOnly(2025, 12, 31), 100m, "Previous year"), Ct);
        await context.Entries.AddAsync(CreatePost(cash, revenue, new DateOnly(2026, 6, 15), 200m, "Dit year"), Ct);

        var entries = await context.Entries.AllAsync(Period.ForYear(2026), Ct);

        Assert.Single(entries, p => p.Description == "Dit year");
    }

    [Fact]
    public async Task AllAsync_OrdersByDate()
    {
        await using var context = new SharpCoreDbTestContext();
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        await context.Entries.AddAsync(CreatePost(cash, revenue, new DateOnly(2026, 8, 1), 1m, "Augustus"), Ct);
        await context.Entries.AddAsync(CreatePost(cash, revenue, new DateOnly(2026, 2, 1), 2m, "Februari"), Ct);

        var entries = await context.Entries.AllAsync(null, Ct);

        Assert.Collection(
            entries,
            p => Assert.Equal("Februari", p.Description),
            p => Assert.Equal("Augustus", p.Description));
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntryAndItsLines()
    {
        await using var context = new SharpCoreDbTestContext();
        var entry = CreatePost(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), 100m, "Te verwijderen");
        await context.Entries.AddAsync(entry, Ct);

        await context.Entries.RemoveAsync(entry.Id, Ct);

        Assert.Empty(await context.Entries.AllAsync(null, Ct));
    }

    [Fact]
    public async Task Data_SurvivesReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        await context.Entries.AddAsync(CreatePost(cash, revenue, new DateOnly(2026, 3, 1), 250m, "Verkoop"), Ct);

        await context.HeropenAsync();

        var entries = await context.Entries.AllAsync(null, Ct);
        Assert.Single(entries);
        Assert.Equal(250m, entries[0].TotalDebit);
    }

    [Fact]
    public async Task AllAsync_ReturnsEmptyForEmptyDatabase()
    {
        await using var context = new SharpCoreDbTestContext();

        var entries = await context.Entries.AllAsync(null, Ct);

        Assert.Empty(entries);
    }
}
