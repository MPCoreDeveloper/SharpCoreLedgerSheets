using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Reporting;

namespace SharpCoreLedgerSheets.Domain.Services;

/// <summary>
/// Derives the VAT return of a period from the ledger.
/// </summary>
public interface IVatReturnBuilder
{
    /// <summary>Builds the VAT return for the period from the movements on the VAT accounts.</summary>
    VatReturn BuildOn(Ledger ledger, VatScheme scheme, Period period);
}

/// <summary>
/// Default <see cref="IVatReturnBuilder"/>.
/// </summary>
/// <remarks>
/// The return is read straight from the VAT accounts: the output account of a rate is a liability (a sale
/// credits it, so the movement is negative and the amount owed is its inverse), the input account is an asset
/// (a purchase debits it). Nothing is recomputed from the revenue or cost lines, so the return always agrees
/// with the ledger - which is the property an accountant checks first.
/// </remarks>
public sealed class VatReturnBuilder : IVatReturnBuilder
{
    /// <inheritdoc />
    public VatReturn BuildOn(Ledger ledger, VatScheme scheme, Period period)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(scheme);

        var lines = new List<VatReturnLine>(scheme.Rates.Count);

        foreach (var rate in scheme.Rates)
        {
            var output = Movement(ledger, rate.OutputAccountNumber, period);
            var input = Movement(ledger, rate.InputAccountNumber, period);

            lines.Add(new VatReturnLine(
                rate.Code,
                rate.Description,
                Math.Round(output is null ? 0m : -output.Value, 2),
                Math.Round(input ?? 0m, 2)));
        }

        return new VatReturn(period, lines);
    }

    // Returns null when the rate has no account or the account is not in the chart, so a partially
    // configured administration still produces a readable return instead of an exception.
    private static decimal? Movement(Ledger ledger, string? accountNumber, Period period)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return null;
        }

        return ledger.FindAccount(accountNumber) is { } account
            ? ledger.MovementInPeriod(account.Id, period)
            : null;
    }
}
