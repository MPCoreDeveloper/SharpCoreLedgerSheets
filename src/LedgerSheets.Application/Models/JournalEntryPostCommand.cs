namespace LedgerSheets.Application.Models;

/// <summary>
/// Command to post a new journal entry.
/// </summary>
/// <param name="Date">The booking date.</param>
/// <param name="Description">The description of the entry.</param>
/// <param name="Lines">The debit/credit lines; total debit must equal total credit.</param>
/// <param name="Reference">Optional external reference.</param>
public sealed record PostJournalEntryCommand(
    DateOnly Date,
    string Description,
    IReadOnlyList<JournalLineCommand> Lines,
    string? Reference = null);
