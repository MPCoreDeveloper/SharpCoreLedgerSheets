using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Models;

/// <summary>
/// The outcome of importing a bank statement.
/// </summary>
/// <param name="Formaat">The format that was used to read the file.</param>
/// <param name="Gelezen">The number of movements in the file.</param>
/// <param name="Toegevoegd">The number of movements that were actually stored.</param>
/// <param name="Overgeslagen">The number of movements that were skipped as duplicates.</param>
/// <param name="Nieuw">The movements that were stored.</param>
public sealed record BankstaatImportResultaat(
    BankstaatFormaat Formaat,
    int Gelezen,
    int Toegevoegd,
    int Overgeslagen,
    IReadOnlyList<BankTransactie> Nieuw)
{
    /// <summary>True when the file held movements, but every one of them was already known.</summary>
    public bool AllesAlBekend => Gelezen > 0 && Toegevoegd == 0;
}
