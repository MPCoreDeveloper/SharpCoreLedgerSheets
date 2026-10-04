using LedgerSheets.Application.Models;
using LedgerSheets.Application.Services;
using LedgerSheets.Domain.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace LedgerSheets.Web.Pages;

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

    private Task LoadAsync(CancellationToken ct) =>
        TryAsync(async () => AllAccounts = await accounts.AllAsync(ct));
}
