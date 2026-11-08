namespace SharpCoreLedgerSheets.Application.Models;

/// <summary>
/// A single debit or credit line of a <see cref="PostJournalEntryCommand"/>.
/// </summary>
/// <param name="AccountNumber">The ledger number the line posts to.</param>
/// <param name="Debit">The debit amount (0 for a credit line).</param>
/// <param name="Credit">The credit amount (0 for a debit line).</param>
/// <param name="Description">Optional line description.</param>
public sealed record JournalLineCommand(
    string AccountNumber,
    decimal Debit,
    decimal Credit,
    string? Description = null);
