using LedgerSheets.Domain.Banking;

namespace LedgerSheets.Application.Models;

/// <summary>
/// The outcome of importing a bank statement.
/// </summary>
/// <param name="Format">The format that was used to read the file.</param>
/// <param name="Read">The number of movements in the file.</param>
/// <param name="Added">The number of movements that were actually stored.</param>
/// <param name="Skipped">The number of movements that were skipped as duplicates.</param>
/// <param name="New">The movements that were stored.</param>
public sealed record BankStatementImportResult(
    BankStatementFormat Format,
    int Read,
    int Added,
    int Skipped,
    IReadOnlyList<BankTransaction> New)
{
    /// <summary>True when the file held movements, but every one of them was already known.</summary>
    public bool AllAlreadyKnown => Read > 0 && Added == 0;
}
