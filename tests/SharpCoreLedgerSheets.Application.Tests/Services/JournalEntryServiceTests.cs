using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class JournalEntryServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<TestAdministration> WithAccountsAsync()
    {
        var administration = new TestAdministration();
        await administration.AccountService.AddAsync(
            new AddAccountCommand("1000", "Kas", AccountType.Assets, Current: true, CashEquivalent: true), Ct);
        await administration.AccountService.AddAsync(
            new AddAccountCommand("8000", "Omzet", AccountType.Revenue), Ct);
        return administration;
    }

    [Fact]
    public async Task PostAsync_ResolvesAccountNumbersAndPersistsEntry()
    {
        var administration = await WithAccountsAsync();
        var command = new PostJournalEntryCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 250m)]);

        var entry = await administration.JournalEntryService.PostAsync(command, Ct);

        Assert.Equal(2, entry.Lines.Count);
        Assert.True(entry.IsBalanced);
        Assert.Single(await administration.JournalEntryService.AllAsync(null, Ct));
    }

    [Fact]
    public async Task PostAsync_ThrowsWhenAccountNumberIsUnknown()
    {
        var administration = await WithAccountsAsync();
        var command = new PostJournalEntryCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("9999", 0m, 250m)]);

        await Assert.ThrowsAsync<NotFoundException>(
            () => administration.JournalEntryService.PostAsync(command, Ct));
    }

    [Fact]
    public async Task PostAsync_ThrowsWhenAccountIsInactive()
    {
        var administration = await WithAccountsAsync();
        await administration.AccountService.DeactivateAsync("8000", Ct);
        var command = new PostJournalEntryCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 250m)]);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.JournalEntryService.PostAsync(command, Ct));

        Assert.Contains("niet actief", exception.Message);
    }

    [Fact]
    public async Task PostAsync_ThrowsWhenEntryIsUnbalanced()
    {
        var administration = await WithAccountsAsync();
        var command = new PostJournalEntryCommand(
            new DateOnly(2026, 3, 1),
            "Verkoop",
            [new("1000", 250m, 0m), new("8000", 0m, 100m)]);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.JournalEntryService.PostAsync(command, Ct));

        Assert.Contains("niet in balans", exception.Message);
    }

    [Fact]
    public async Task PostAsync_ThrowsForNullCommand()
    {
        var administration = await WithAccountsAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() => administration.JournalEntryService.PostAsync(null!, Ct));
    }

    [Fact]
    public async Task AllAsync_FiltersByPeriod()
    {
        var administration = await WithAccountsAsync();
        await administration.JournalEntryService.PostAsync(
            new PostJournalEntryCommand(new DateOnly(2025, 12, 31), "Previous year",
                [new("1000", 10m, 0m), new("8000", 0m, 10m)]), Ct);
        await administration.JournalEntryService.PostAsync(
            new PostJournalEntryCommand(new DateOnly(2026, 3, 1), "Dit year",
                [new("1000", 20m, 0m), new("8000", 0m, 20m)]), Ct);

        var result = await administration.JournalEntryService.AllAsync(Period.ForYear(2026), Ct);

        Assert.Single(result, p => p.Description == "Dit year");
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        var administration = await WithAccountsAsync();
        var entry = await administration.JournalEntryService.PostAsync(
            new PostJournalEntryCommand(new DateOnly(2026, 3, 1), "Verkoop",
                [new("1000", 10m, 0m), new("8000", 0m, 10m)]), Ct);

        await administration.JournalEntryService.RemoveAsync(entry.Id, Ct);

        Assert.Empty(await administration.JournalEntryService.AllAsync(null, Ct));
    }

    [Fact]
    public async Task RemoveAsync_ThrowsForEmptyId()
    {
        var administration = await WithAccountsAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administration.JournalEntryService.RemoveAsync(Guid.Empty, Ct));
    }
}
