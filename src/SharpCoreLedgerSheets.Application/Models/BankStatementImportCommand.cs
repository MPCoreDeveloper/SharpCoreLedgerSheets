using SharpCoreLedgerSheets.Domain.Banking;

namespace SharpCoreLedgerSheets.Application.Models;

/// <summary>
/// Command to import a bank statement file.
/// </summary>
/// <param name="FileName">The name of the uploaded file, including its extension.</param>
/// <param name="Content">The full text of the file.</param>
/// <param name="BankAccountNumber">The ledger number of the bank account the statement belongs to.</param>
/// <param name="Format">
/// The format to force, or <see langword="null"/> to let SharpCoreLedgerSheets detect it from the file.
/// </param>
public sealed record BankStatementImportCommand(
    string FileName,
    string Content,
    string BankAccountNumber,
    BankStatementFormat? Format = null);
