using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Models;

/// <summary>
/// Command to create a new general-ledger account.
/// </summary>
/// <param name="Nummer">The unique ledger number.</param>
/// <param name="Naam">The display name.</param>
/// <param name="Soort">The account type.</param>
/// <param name="Vlottend">Whether the account is a current asset/liability.</param>
/// <param name="LiquideMiddel">Whether the account represents cash or a cash equivalent.</param>
/// <param name="Voorraad">Whether the account represents inventory.</param>
public sealed record RekeningToevoegenCommand(
    string Nummer,
    string Naam,
    RekeningSoort Soort,
    bool Vlottend = false,
    bool LiquideMiddel = false,
    bool Voorraad = false);
