using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>Chart of accounts: list the ledger accounts and add new ones.</summary>
public sealed class AccountsModel(IAccountService accounts) : AppPageModel
{
    public IReadOnlyList<Account> AllAccounts { get; private set; } = [];

    public AccountType[] Types { get; } = Enum.GetValues<AccountType>();

    [BindProperty]
    public string Number { get; set; } = string.Empty;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public AccountType Type { get; set; } = AccountType.Assets;

    [BindProperty]
    public bool Current { get; set; }

    [BindProperty]
    public bool CashEquivalent { get; set; }

    [BindProperty]
    public bool Inventory { get; set; }

    /// <summary>The starter chart the user selected (Dutch MKB by default).</summary>
    [BindProperty]
    public StarterChart Chart { get; set; } = StarterChart.DutchMkb;

    /// <summary>Every starter chart the engine offers, for the picker.</summary>
    public StarterChart[] StarterCharts { get; } = Enum.GetValues<StarterChart>();

    /// <summary>The number of accounts the last starter-chart action created, or <see langword="null"/>.</summary>
    public int? CreatedCount { get; private set; }

    public Task OnGetAsync(CancellationToken ct) => LoadAsync(ct);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            await accounts.AddAsync(
                new AddAccountCommand(Number, Name, Type, Current, CashEquivalent, Inventory), ct);

            Number = string.Empty;
            Name = string.Empty;
            Current = false;
            CashEquivalent = false;
            Inventory = false;
        });

        await LoadAsync(ct);
        return Page();
    }

    /// <summary>Applies a starter chart: the Dutch MKB chart by default, or the international variant.</summary>
    public async Task<IActionResult> OnPostStarterAsync(CancellationToken ct)
    {
        await TryAsync(async () => CreatedCount = await accounts.ApplyStarterChartAsync(Chart, ct));

        await LoadAsync(ct);
        return Page();
    }

    private Task LoadAsync(CancellationToken ct) =>
        TryAsync(async () => AllAccounts = await accounts.AllAsync(ct));
}
