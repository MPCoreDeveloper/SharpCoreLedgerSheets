using LedgerSheets.Application.Exceptions;
using LedgerSheets.Application.Models;
using LedgerSheets.Application.Services;
using LedgerSheets.Application.Tests.Fakes;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Application.Tests.Services;

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
