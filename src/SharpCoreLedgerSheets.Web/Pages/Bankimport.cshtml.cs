using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace SharpCoreLedgerSheets.Web.Pages;

/// <summary>
/// Bank import: upload a statement, review the movements the matching assistant proposes an account
/// for, and book or ignore them one by one.
/// </summary>
public sealed class BankImportModel(IBankStatementService bankStatement, IAccountService accounts) : AppPageModel
{
    /// <summary>The movements that still have to be booked or ignored.</summary>
    public IReadOnlyList<BankImportLine> Lines { get; private set; } = [];

    /// <summary>The ledger accounts a movement can be booked against.</summary>
    public IReadOnlyList<Account> AllAccounts { get; private set; } = [];

    /// <summary>The outcome of the last import, when the page was reached through one.</summary>
    public BankStatementImportResult? Result { get; private set; }

    /// <summary>The formats a statement can be forced into; 'automatic' is not one of them.</summary>
    public BankStatementFormat[] Formaten { get; } = Enum.GetValues<BankStatementFormat>();

    [BindProperty]
    public IFormFile? StatementFile { get; set; }

    [BindProperty]
    public string BankAccountNumber { get; set; } = string.Empty;

    [BindProperty]
    public BankStatementFormat? Format { get; set; }

    [BindProperty]
    public Guid TransactionId { get; set; }

    [BindProperty]
    public string CounterAccountNumber { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    public Task OnGetAsync(CancellationToken ct) => LoadAsync(ct);

    public async Task<IActionResult> OnPostImportAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
        {
            if (StatementFile is null)
            {
                throw new DomainException(MessageCode.StatementFileRequired, "Kies eerst een bankafschrift (CAMT.053 of CSV) om te importeren.");
            }

            using var reader = new StreamReader(StatementFile.OpenReadStream());
            var content = await reader.ReadToEndAsync(ct);

            Result = await bankStatement.ImportAsync(
                new BankStatementImportCommand(StatementFile.FileName, content, BankAccountNumber, Format),
                ct);
        });

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostPostAsync(CancellationToken ct)
    {
        await TryAsync(async () =>
            await bankStatement.PostAsync(TransactionId, CounterAccountNumber, Description, ct));

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostIgnoreAsync(CancellationToken ct)
    {
        await TryAsync(async () => await bankStatement.IgnoreAsync(TransactionId, ct));

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        await TryAsync(async () => AllAccounts = await accounts.AllAsync(ct));
        await TryAsync(async () => Lines = await OpenLinesAsync(ct));
    }

    /// <summary>Reads the open movements and asks the assistant for an account per movement.</summary>
    private async Task<IReadOnlyList<BankImportLine>> OpenLinesAsync(CancellationToken ct)
    {
        var open = await bankStatement.AllAsync(BankTransactionStatus.Open, ct);

        if (open.Count == 0)
        {
            return [];
        }

        // All proposals in one pass: the assistant reads the ledger and the learned rules once for the
        // whole list, so a statement with many movements does not cost a lookup per movement.
        var proposals = new Dictionary<Guid, PostingProposal>();

        foreach (var proposal in await bankStatement.ProposalsAsync(open, ct))
        {
            // Indexer rather than ToDictionary: a duplicate id from storage must not take the page down.
            proposals[proposal.TransactionId] = proposal;
        }

        return [.. open.Select(transaction => new BankImportLine(
            transaction,
            proposals.GetValueOrDefault(transaction.Id)))];
    }
}
