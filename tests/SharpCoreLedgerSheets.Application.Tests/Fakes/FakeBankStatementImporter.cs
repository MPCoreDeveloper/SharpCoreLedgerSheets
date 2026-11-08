using System.Globalization;
using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Fakes;

/// <summary>
/// Stub <see cref="IBankStatementImporter"/> that reads a one-line-per-movement test format:
/// <c>date;amount;reference;counterparty;iban;description</c>.
/// </summary>
/// <remarks>
/// The application tests must not depend on the infrastructure layer, so the real CAMT.053 and CSV
/// readers are replaced by this stub. Only the extension decides whether the reader applies, which is
/// enough to test the selection of a reader.
/// </remarks>
internal sealed class FakeBankStatementImporter(string extension, BankStatementFormat format) : IBankStatementImporter
{
    /// <summary>The extension of the test format for which this stub applies.</summary>
    public string Extension { get; } = extension;

    public BankStatementFormat Format { get; } = format;

    public bool CanProcess(string fileName, string content) =>
        fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<BankTransaction> Read(
        string content,
        string bankAccountNumber,
        Currency? defaultCurrency = null)
    {
        var currency = defaultCurrency ?? Currency.Eur;
        var transactions = new List<BankTransaction>();

        foreach (var line in content.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(';');

            if (parts.Length < 3)
            {
                throw new DomainException($"Onleesbare regel: '{line}'.");
            }

            transactions.Add(new BankTransaction(
                Guid.NewGuid(),
                bankAccountNumber,
                DateOnly.ParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                new Amount(decimal.Parse(parts[1], CultureInfo.InvariantCulture), currency),
                parts[2],
                Divide(parts, 3),
                Divide(parts, 4),
                Divide(parts, 5)));
        }

        return transactions;
    }

    private static string? Divide(string[] parts, int index) =>
        parts.Length > index && parts[index].Length > 0 ? parts[index] : null;
}
