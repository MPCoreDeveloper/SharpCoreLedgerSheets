using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Models;

/// <summary>
/// Command to create a new general-ledger account.
/// </summary>
/// <param name="Number">The unique ledger number.</param>
/// <param name="Name">The display name.</param>
/// <param name="Type">The account type.</param>
/// <param name="Current">Whether the account is a current asset/liability.</param>
/// <param name="CashEquivalent">Whether the account represents cash or a cash equivalent.</param>
/// <param name="Inventory">Whether the account represents inventory.</param>
public sealed record AddAccountCommand(
    string Number,
    string Name,
    AccountType Type,
    bool Current = false,
    bool CashEquivalent = false,
    bool Inventory = false);
