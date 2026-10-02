namespace FinOverzicht.Application.Models;

/// <summary>
/// Command to post a new journal entry.
/// </summary>
/// <param name="Datum">The booking date.</param>
/// <param name="Omschrijving">The description of the entry.</param>
/// <param name="Regels">The debit/credit lines; total debit must equal total credit.</param>
/// <param name="Referentie">Optional external reference.</param>
public sealed record JournaalpostBoekenCommand(
    DateOnly Datum,
    string Omschrijving,
    IReadOnlyList<BoekingsregelCommand> Regels,
    string? Referentie = null);
