namespace SharpCoreLedgerSheets.Application.Models;

/// <summary>
/// One line of the opening balance of a book year: the amount an account starts the year with.
/// </summary>
/// <param name="AccountNumber">The ledger number of the account.</param>
/// <param name="Debit">The debit balance it starts with (0 for a credit balance).</param>
/// <param name="Credit">The credit balance it starts with (0 for a debit balance).</param>
public sealed record OpeningBalanceLine(string AccountNumber, decimal Debit, decimal Credit);
