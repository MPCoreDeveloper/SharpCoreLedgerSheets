using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// Administration settings: the company name, the month the book year starts in, and the opening balances
/// of a book year.
/// </summary>
public sealed class SettingsModel(
    IAdministrationService administration,
    IAccountService accounts) : AppPageModel
{
    /// <summary>The settings currently stored.</summary>
    public AdministrationSettings Current { get; private set; } = AdministrationSettings.Default;

    /// <summary>The accounts an opening balance can be entered for (assets, liabilities and equity).</summary>
    public IReadOnlyList<Account> BalanceAccounts { get; private set; } = [];

    /// <summary>The opening balances of <see cref="Year"/> as entered.</summary>
    public IReadOnlyList<OpeningBalanceLine> OpeningBalances { get; private set; } = [];

    /// <summary>True when the last action was stored successfully.</summary>
    public bool Saved { get; private set; }

    [BindProperty]
    public int Year { get; set; } = DateTime.Today.Year;

    [BindProperty]
    public string CompanyName { get; set; } = string.Empty;

    [BindProperty]
    public int FiscalYearStartMonth { get; set; } = AdministrationSettings.CalendarYearStart;

    [BindProperty]
    public List<OpeningBalanceRow> OpeningRows { get; set; } = [];

    /// <summary>One editable opening-balance row in the page.</summary>
    public sealed class OpeningBalanceRow
    {
        /// <summary>The ledger number of the account.</summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>The name of the account, for display only.</summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>The debit amount the account starts the year with.</summary>
        public decimal Debit { get; set; }

        /// <summary>The credit amount the account starts the year with.</summary>
        public decimal Credit { get; set; }
    }

    public async Task OnGetAsync(int? year, CancellationToken ct)
    {
        Year = year ?? DateTime.Today.Year;
        await LoadAsync(ct);
    }

    /// <summary>Stores the company name and the start month of the book year.</summary>
    public async Task<IActionResult> OnPostSettingsAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            await administration.SaveSettingsAsync(
                new AdministrationSettings(CompanyName, FiscalYearStartMonth), ct);

            Saved = true;
        });

        await LoadAsync(ct);
        return Page();
    }

    /// <summary>Replaces the opening balances of the selected book year.</summary>
    public async Task<IActionResult> OnPostOpeningAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            var lines = OpeningRows
                .Where(row => !string.IsNullOrWhiteSpace(row.AccountNumber) && (row.Debit != 0m || row.Credit != 0m))
                .Select(row => new OpeningBalanceLine(row.AccountNumber, row.Debit, row.Credit))
                .ToList();

            await administration.ReplaceOpeningBalancesAsync(Year, lines, ct);
            Saved = true;
        });

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            Current = await administration.GetSettingsAsync(ct);
            CompanyName = Current.CompanyName;
            FiscalYearStartMonth = Current.FiscalYearStartMonth;

            var all = await accounts.AllAsync(ct);
            BalanceAccounts = all
                .Where(account => account.Type is AccountType.Assets or AccountType.Liabilities or AccountType.Equity)
                .ToList();

            OpeningBalances = await administration.OpeningBalancesAsync(Year, ct);

            OpeningRows = BalanceAccounts
                .Select(account =>
                {
                    var existing = OpeningBalances.FirstOrDefault(
                        line => string.Equals(line.AccountNumber, account.Number, StringComparison.OrdinalIgnoreCase));

                    return new OpeningBalanceRow
                    {
                        AccountNumber = account.Number,
                        AccountName = account.Name,
                        Debit = existing?.Debit ?? 0m,
                        Credit = existing?.Credit ?? 0m,
                    };
                })
                .ToList();
        });
    }
}
