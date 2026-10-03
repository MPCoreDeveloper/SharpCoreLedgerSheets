using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Models;

/// <summary>
/// Command to import a bank statement file.
/// </summary>
/// <param name="Bestandsnaam">The name of the uploaded file, including its extension.</param>
/// <param name="Inhoud">The full text of the file.</param>
/// <param name="BankRekeningNummer">The ledger number of the bank account the statement belongs to.</param>
/// <param name="Formaat">
/// The format to force, or <see langword="null"/> to let FinOverzicht detect it from the file.
/// </param>
public sealed record BankstaatImportCommand(
    string Bestandsnaam,
    string Inhoud,
    string BankRekeningNummer,
    BankstaatFormaat? Formaat = null);
