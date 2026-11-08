using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Application.Tests.Fakes;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class AccountServiceTests
{
    private readonly InMemoryAccountRepository _repository = new();
    private readonly AccountService _service;

    public AccountServiceTests() => _service = new AccountService(_repository);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddAsync_PersistsAccountAndReturnsIt()
    {
        var account = await _service.AddAsync(
            new AddAccountCommand("1000", "Kas", AccountType.Assets, Current: true, CashEquivalent: true), Ct);

        Assert.Equal("1000", account.Number);
        Assert.True(account.IsCashEquivalent);
        Assert.Single(await _service.AllAsync(Ct));
    }

    [Fact]
    public async Task AddAsync_ThrowsForDuplicateNumber()
    {
        await _service.AddAsync(new AddAccountCommand("1000", "Kas", AccountType.Assets), Ct);

        await Assert.ThrowsAsync<DomainException>(
            () => _service.AddAsync(new AddAccountCommand("1000", "Kas 2", AccountType.Assets), Ct));
    }

    [Fact]
    public async Task AddAsync_ThrowsForNullCommand()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!, Ct));
    }

    [Fact]
    public async Task ApplyStarterChartAsync_CreatesTheDutchChart()
    {
        var created = await _service.ApplyStarterChartAsync(StarterChart.DutchMkb, Ct);

        Assert.Equal(StarterCharts.DutchMkb.Count, created);

        var accounts = await _service.AllAsync(Ct);
        Assert.Equal(StarterCharts.DutchMkb.Count, accounts.Count);
        Assert.Contains(accounts, account => account.Number == "1000" && account.IsCashEquivalent);
        Assert.Contains(accounts, account => account.Number == "1400" && account.IsInventory);
        Assert.Contains(accounts, account => account.Number == "8000" && account.Type == AccountType.Revenue);
    }

    [Fact]
    public async Task ApplyStarterChartAsync_IsIdempotent()
    {
        await _service.ApplyStarterChartAsync(StarterChart.DutchMkb, Ct);

        Assert.Equal(0, await _service.ApplyStarterChartAsync(StarterChart.DutchMkb, Ct));
        Assert.Equal(StarterCharts.DutchMkb.Count, (await _service.AllAsync(Ct)).Count);
    }

    [Fact]
    public async Task ApplyStarterChartAsync_KeepsAccountsTheAdministrationAlreadyHas()
    {
        await _service.AddAsync(
            new AddAccountCommand("1000", "Mijn kas", AccountType.Assets, Current: true, CashEquivalent: true), Ct);

        var created = await _service.ApplyStarterChartAsync(StarterChart.DutchMkb, Ct);

        Assert.Equal(StarterCharts.DutchMkb.Count - 1, created);
        Assert.Equal("Mijn kas", (await _service.AllAsync(Ct)).Single(a => a.Number == "1000").Name);
    }

    [Fact]
    public async Task ApplyStarterChartAsync_International_UsesEnglishNames()
    {
        await _service.ApplyStarterChartAsync(StarterChart.International, Ct);

        var accounts = await _service.AllAsync(Ct);
        Assert.Equal("Cash", accounts.Single(a => a.Number == "1000").Name);
        Assert.Equal("Revenue", accounts.Single(a => a.Number == "8000").Name);
    }

    [Fact]
    public async Task RenameAsync_UpdatesStoredAccount()
    {
        await _service.AddAsync(new AddAccountCommand("1000", "Kas", AccountType.Assets), Ct);

        await _service.RenameAsync("1000", "Kas en bank", Ct);

        var account = await _repository.FindOnNumberAsync("1000", Ct);
        Assert.Equal("Kas en bank", account!.Name);
    }

    [Fact]
    public async Task RenameAsync_ThrowsWhenAccountIsUnknown()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RenameAsync("9999", "Unknown", Ct));
    }

    [Fact]
    public async Task DeactivateAsync_MarksAccountInactive()
    {
        await _service.AddAsync(new AddAccountCommand("1000", "Kas", AccountType.Assets), Ct);

        await _service.DeactivateAsync("1000", Ct);

        Assert.False((await _repository.FindOnNumberAsync("1000", Ct))!.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_ThrowsWhenAccountIsUnknown()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeactivateAsync("9999", Ct));
    }
}
