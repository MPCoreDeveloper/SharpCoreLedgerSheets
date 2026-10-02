namespace FinOverzicht.Application.Models;

/// <summary>
/// A single debit or credit line of a <see cref="JournaalpostBoekenCommand"/>.
/// </summary>
/// <param name="RekeningNummer">The ledger number the line posts to.</param>
/// <param name="Debet">The debit amount (0 for a credit line).</param>
/// <param name="Credit">The credit amount (0 for a debit line).</param>
/// <param name="Omschrijving">Optional line description.</param>
public sealed record BoekingsregelCommand(
    string RekeningNummer,
    decimal Debet,
    decimal Credit,
    string? Omschrijving = null);
