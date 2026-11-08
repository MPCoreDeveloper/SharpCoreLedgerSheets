using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// Journal entries: list the entries of a book year, post a simple two-line entry, or book an invoice
/// amount with VAT.
/// </summary>
public sealed class JournalModel(
    IJournalEntryService journalEntries,
    IAccountService accounts,
    IAdministrationService administration,
    IVatService vat) : AppPageModel
{
    public IReadOnlyList<JournalEntry> Entries { get; private set; } = [];

    public IReadOnlyList<Account> AllAccounts { get; private set; } = [];

    public IReadOnlyList<Account> CounterAccounts { get; private set; } = [];

    /// <summary>The VAT rates of the administration, for the VAT form.</summary>
    public VatScheme VatScheme { get; private set; } = VatScheme.Dutch;

    public int Year { get; private set; }

    [BindProperty]
    public DateOnly Date { get; set; }

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public string? Reference { get; set; }

    [BindProperty]
    public string DebitAccount { get; set; } = string.Empty;

    [BindProperty]
    public string CreditAccount { get; set; } = string.Empty;

    [BindProperty]
    public decimal Amount { get; set; }

    /// <summary>True for a sale (VAT owed), false for a purchase (VAT reclaimable).</summary>
    [BindProperty]
    public bool IsSale { get; set; } = true;

    [BindProperty]
    public string VatNetAccount { get; set; } = string.Empty;

    [BindProperty]
    public string VatCounterAccount { get; set; } = string.Empty;

    [BindProperty]
    public decimal VatNetAmount { get; set; }

    [BindProperty]
    public string VatRate { get; set; } = "21";

    public async Task OnGetAsync(int? year, CancellationToken ct)
    {
        Year = year ?? DateTime.Today.Year;
        Date = DateOnly.FromDateTime(DateTime.Today);
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(int? year, CancellationToken ct)
    {
        Year = year ?? Date.Year;

        await TryAsync(async () =>
        {
            var lines = new List<JournalLineCommand>
            {
                new(DebitAccount, Amount, 0m),
                new(CreditAccount, 0m, Amount),
            };

            await journalEntries.PostAsync(
                new PostJournalEntryCommand(Date, Description, lines, Reference), ct);

            Description = string.Empty;
            Reference = null;
            Amount = 0m;
        });

        await LoadAsync(ct);
        return Page();
    }

    /// <summary>Books an invoice amount with VAT, in one balanced entry.</summary>
    public async Task<IActionResult> OnPostVatAsync(int? year, CancellationToken ct)
    {
        Year = year ?? Date.Year;

        await TryAsync(async () =>
        {
            var command = new VatInvoiceCommand(
                Date, Description, VatNetAccount, VatCounterAccount, VatNetAmount, VatRate, Reference);

            if (IsSale)
            {
                await vat.PostSaleAsync(command, ct);
            }
            else
            {
                await vat.PostPurchaseAsync(command, ct);
            }

            Description = string.Empty;
            Reference = null;
            VatNetAmount = 0m;
        });

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            var settings = await administration.GetSettingsAsync(ct);
            VatScheme = settings.Vat;
            VatRate = VatScheme.Rates.Count > 0 ? VatScheme.Rates[0].Code : string.Empty;
            Entries = await journalEntries.AllAsync(settings.PeriodFor(Year), ct);
        });

        await TryAsync(async () => AllAccounts = await accounts.AllAsync(ct));

        CounterAccounts = AllAccounts
            .Where(account => account.Type is AccountType.Assets or AccountType.Liabilities)
            .ToList();
    }
}

