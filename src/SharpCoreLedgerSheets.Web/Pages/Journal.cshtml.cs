using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>Journal entries: list the entries of a year and post a simple two-line entry.</summary>
public sealed class JournalModel(IJournalEntryService journalEntries, IAccountService accounts) : AppPageModel
{
    public IReadOnlyList<JournalEntry> Entries { get; private set; } = [];

    public IReadOnlyList<Account> AllAccounts { get; private set; } = [];

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

    private async Task LoadAsync(CancellationToken ct)
    {
        await TryAsync(async () => Entries = await journalEntries.AllAsync(Period.ForYear(Year), ct));
        await TryAsync(async () => AllAccounts = await accounts.AllAsync(ct));
    }
}
