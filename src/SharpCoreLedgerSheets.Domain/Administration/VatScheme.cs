namespace SharpCoreLedgerSheets.Domain.Administration;

/// <summary>
/// One VAT rate of an administration, with the ledger accounts its output and input VAT are booked to.
/// </summary>
/// <param name="Code">The short code used in commands and on screen (for example <c>21</c>).</param>
/// <param name="Description">The description shown to the user (for example <c>21%</c>).</param>
/// <param name="Percentage">The rate as a percentage (21 for 21%).</param>
/// <param name="OutputAccountNumber">
/// The account that collects VAT owed on sales, or <see langword="null"/> for a rate without a VAT leg
/// (0% and VAT-exempt turnover).
/// </param>
/// <param name="InputAccountNumber">
/// The account that collects VAT reclaimable on purchases, or <see langword="null"/> for a rate without a
/// VAT leg.
/// </param>
public sealed record VatRate(
    string Code,
    string Description,
    decimal Percentage,
    string? OutputAccountNumber = null,
    string? InputAccountNumber = null)
{
    /// <summary>True when this rate books a VAT leg on both sides.</summary>
    public bool BooksVat =>
        !string.IsNullOrWhiteSpace(OutputAccountNumber) && !string.IsNullOrWhiteSpace(InputAccountNumber);
}

/// <summary>
/// The VAT rates an administration works with.
/// </summary>
/// <remarks>
/// A VAT transaction is an ordinary balanced journal entry: the net amount goes to the revenue or cost
/// account and the VAT goes to the rate's VAT account. The scheme only says which rates exist and which
/// accounts they use - it never changes the ledger itself, so every VAT booking stays auditable.
/// </remarks>
public sealed class VatScheme
{
    /// <summary>The Dutch scheme: 21% and 9% with their own accounts, plus the 0% / exempt rate.</summary>
    public static VatScheme Dutch { get; } = new(
    [
        new("21", "21%", 21m, OutputAccountNumber: "2100", InputAccountNumber: "1110"),
        new("9", "9%", 9m, OutputAccountNumber: "2101", InputAccountNumber: "1111"),
        new("0", "0% / vrijgesteld", 0m),
    ]);

    /// <summary>An administration that does not use VAT at all.</summary>
    public static VatScheme None { get; } = new([]);

    /// <summary>Creates a scheme from its rates; the codes must be unique.</summary>
    /// <exception cref="Common.DomainException">Thrown when the list is null or a code repeats.</exception>
    public VatScheme(IReadOnlyList<VatRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        var duplicates = rates
            .GroupBy(rate => rate.Code, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (duplicates)
        {
            throw new Common.DomainException(
                Common.MessageCode.VatRateDuplicate,
                "De btw-tarieven bevatten een dubbel tarief.");
        }

        Rates = rates;
    }

    /// <summary>Every rate of the scheme, in the order they are offered.</summary>
    public IReadOnlyList<VatRate> Rates { get; }

    /// <summary>True when the administration does not work with VAT.</summary>
    public bool IsEmpty => Rates.Count == 0;

    /// <summary>Finds a rate by its code, or <see langword="null"/> when it does not exist.</summary>
    public VatRate? Find(string code) =>
        Rates.FirstOrDefault(rate => string.Equals(rate.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds a rate by its code and returns it, or explains which codes do exist.</summary>
    /// <exception cref="Common.DomainException">Thrown when the code is unknown.</exception>
    public VatRate Require(string code) =>
        Find(code)
        ?? throw new Common.DomainException(
            Common.MessageCode.VatRateUnknown,
            "Het btw-tarief '{0}' bestaat niet. Beschikbaar: {1}.",
            code,
            string.Join(", ", Rates.Select(rate => rate.Code)));
}
