using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Application.Abstractions;

/// <summary>
/// Port for reading a bank statement file into bank transactions.
/// </summary>
/// <remarks>
/// The format-specific readers (CAMT.053, CSV) live in the infrastructure layer, so supporting a
/// new bank format never touches the domain or the use cases.
/// </remarks>
public interface IBankStatementImporter
{
    /// <summary>The format this reader understands.</summary>
    BankStatementFormat Format { get; }

    /// <summary>
    /// Indicates whether this reader recognises the file, based on its name and content.
    /// </summary>
    /// <param name="fileName">The file name as it was uploaded.</param>
    /// <param name="content">The full text of the file.</param>
    bool CanProcess(string fileName, string content);

    /// <summary>
    /// Reads every movement from a statement. A file the reader cannot make sense of results in a
    /// domain exception with a message that can be shown to the user as-is.
    /// </summary>
    /// <param name="content">The full text of the file.</param>
    /// <param name="bankAccountNumber">The ledger number of the bank account the statement belongs to.</param>
    /// <param name="defaultCurrency">The currency to assume when the file does not state one.</param>
    /// <returns>The movements, in file order.</returns>
    IReadOnlyList<BankTransaction> Read(
        string content,
        string bankAccountNumber,
        Currency? defaultCurrency = null);
}
