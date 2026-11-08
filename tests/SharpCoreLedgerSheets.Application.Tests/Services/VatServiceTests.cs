using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Application.Tests.Fakes;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class VatServiceTests
{
    private static readonly DateOnly Date = new(2026, 6, 15);

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryJournalEntryRepository _entries = new();
    private readonly InMemorySettingsRepository _settings = new();
    private readonly VatService _service;

    public VatServiceTests() =>
        _service = new VatService(_settings, new JournalEntryService(_accounts, _entries));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PostSaleAsync_BooksTheNetRevenueAndTheVatOnItsOwnAccount()
    {
        await SeedChartAsync();
        var debtor = await AccountIdAsync("1300");
        var revenue = await AccountIdAsync("8000");
        var vat = await AccountIdAsync("2100");

        var entry = await _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Verkoop", "8000", "1300", 100m, "21"), Ct);

        Assert.Equal(3, entry.Lines.Count);
        Assert.Equal(121m, entry.TotalDebit);
        Assert.Equal(121m, entry.TotalCredit);
        Assert.Equal(121m, entry.Lines.Single(line => line.AccountId == debtor).Debit);
        Assert.Equal(100m, entry.Lines.Single(line => line.AccountId == revenue).Credit);
        Assert.Equal(21m, entry.Lines.Single(line => line.AccountId == vat).Credit);
        Assert.Single(await _entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task PostPurchaseAsync_BooksTheNetCostAndReclaimsTheVat()
    {
        await SeedChartAsync();
        var costs = await AccountIdAsync("7000");
        var creditor = await AccountIdAsync("2000");
        var input = await AccountIdAsync("1111");

        var entry = await _service.PostPurchaseAsync(
            new VatInvoiceCommand(Date, "Inkoop", "7000", "2000", 100m, "9"), Ct);

        Assert.Equal(3, entry.Lines.Count);
        Assert.Equal(109m, entry.TotalDebit);
        Assert.Equal(109m, entry.TotalCredit);
        Assert.Equal(100m, entry.Lines.Single(line => line.AccountId == costs).Debit);
        Assert.Equal(9m, entry.Lines.Single(line => line.AccountId == input).Debit);
        Assert.Equal(109m, entry.Lines.Single(line => line.AccountId == creditor).Credit);
    }

    [Fact]
    public async Task PostSaleAsync_ZeroRate_KeepsTheEntryToTwoLines()
    {
        await SeedChartAsync();

        var entry = await _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Vrijgesteld", "8000", "1300", 100m, "0"), Ct);

        Assert.Equal(2, entry.Lines.Count);
        Assert.Equal(100m, entry.TotalDebit);
    }

    [Fact]
    public async Task PostSaleAsync_RoundsTheVatToCents()
    {
        await SeedChartAsync();
        var vat = await AccountIdAsync("2100");

        var entry = await _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Verkoop", "8000", "1300", 99.99m, "21"), Ct);

        // 99.99 * 21% = 20.9979 -> 21.00, so the gross amount is 120.99.
        Assert.Equal(21.00m, entry.Lines.Single(line => line.AccountId == vat).Credit);
        Assert.Equal(120.99m, entry.TotalDebit);
    }

    [Theory]
    [InlineData(1, 21, 0)]
    [InlineData(2, 21, 0)]
    [InlineData(3, 21, 1)]
    [InlineData(10000, 21, 2100)]
    [InlineData(10000, 9, 900)]
    [InlineData(10000, 0, 0)]
    public void VatOn_RoundsHalfAwayFromZero(int netCents, int percentage, int expectedCents)
    {
        // The amounts are passed as cents so the theory parameters stay exact: InlineData carries doubles,
        // and 0.03 as a double is not 0.03 as a decimal.
        var vat = VatService.VatOn(netCents / 100m, percentage);

        Assert.Equal(expectedCents / 100m, vat);
    }

    [Fact]
    public async Task PostSaleAsync_RejectsAnUnknownRate()
    {
        await SeedChartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Verkoop", "8000", "1300", 100m, "6"), Ct));

        Assert.Equal(MessageCode.VatRateUnknown, exception.Code);
    }

    [Fact]
    public async Task PostSaleAsync_RejectsAnAdministrationWithoutVat()
    {
        await SeedChartAsync();
        _settings.Set(AdministrationSettings.Default.WithVatScheme(VatScheme.None));

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Verkoop", "8000", "1300", 100m, "21"), Ct));

        Assert.Equal(MessageCode.VatSchemeEmpty, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostSaleAsync_RejectsANonPositiveNetAmount(int net)
    {
        await SeedChartAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => _service.PostSaleAsync(
            new VatInvoiceCommand(Date, "Verkoop", "8000", "1300", net, "21"), Ct));

        Assert.Equal(MessageCode.AmountMustBePositive, exception.Code);
        Assert.Empty(await _entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task PostSaleAsync_RejectsANullCommand() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.PostSaleAsync(null!, Ct));

    private async Task<Guid> AccountIdAsync(string number) =>
        (await _accounts.FindOnNumberAsync(number, Ct))?.Id
            ?? throw new InvalidOperationException($"Account {number} was not seeded.");

    private async Task SeedChartAsync()
    {
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "1100", "Bank", AccountType.Assets, isCurrent: true, isCashEquivalent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "1110", "Voorbelasting 21%", AccountType.Assets, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "1111", "Voorbelasting 9%", AccountType.Assets, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "1300", "Debiteuren", AccountType.Assets, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "2000", "Crediteuren", AccountType.Liabilities, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "2100", "Te betalen btw 21%", AccountType.Liabilities, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "2101", "Te betalen btw 9%", AccountType.Liabilities, isCurrent: true), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "7000", "Inkoopwaarde", AccountType.Costs), Ct);
        await _accounts.AddAsync(new Account(Guid.NewGuid(), "8000", "Omzet", AccountType.Revenue), Ct);
    }
}
