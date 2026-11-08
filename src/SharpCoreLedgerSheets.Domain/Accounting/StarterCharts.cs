namespace SharpCoreLedgerSheets.Domain.Accounting;

/// <summary>
/// The starter charts of accounts the engine can offer for a new administration.
/// </summary>
public enum StarterChart
{
    /// <summary>
    /// A simplified chart for the Dutch MKB: the customary Dutch numbering (1xxx liquid assets, 2xxx
    /// current liabilities, 3xxx long-term debt, 4xxx equity, 7xxx costs, 8xxx revenue), aligned with the
    /// RGS grouping without copying its full depth. Dutch account names, because this is the chart a Dutch
    /// entrepreneur recognises.
    /// </summary>
    DutchMkb,

    /// <summary>
    /// The same numbering and structure with English account names, so a non-Dutch administration reports
    /// the same figures under names it understands.
    /// </summary>
    International,
}

/// <summary>One account of a starter chart, before it is written to the ledger.</summary>
/// <param name="Number">The unique ledger number.</param>
/// <param name="Name">The account name, in the language of the chart.</param>
/// <param name="Type">The account type.</param>
/// <param name="IsCurrent">Whether the account is a current asset or liability.</param>
/// <param name="IsCashEquivalent">Whether the account is cash or a cash equivalent.</param>
/// <param name="IsInventory">Whether the account holds inventory.</param>
public sealed record StarterAccount(
    string Number,
    string Name,
    AccountType Type,
    bool IsCurrent = false,
    bool IsCashEquivalent = false,
    bool IsInventory = false);

/// <summary>
/// The starter charts themselves: enough accounts to post a real administration and to produce all three
/// statements (there is cash, inventory, debt, equity, revenue and costs), and not one account more.
/// </summary>
public static class StarterCharts
{
    /// <summary>Returns the accounts of the requested chart, in ledger-number order.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown chart value.</exception>
    public static IReadOnlyList<StarterAccount> For(StarterChart chart) => chart switch
    {
        StarterChart.DutchMkb => DutchMkb,
        StarterChart.International => International,
        _ => throw new ArgumentOutOfRangeException(nameof(chart), chart, "Unknown starter chart."),
    };

    /// <summary>The Dutch MKB chart, with Dutch account names.</summary>
    public static IReadOnlyList<StarterAccount> DutchMkb { get; } =
    [
        new("0100", "Inventaris", AccountType.Assets),
        new("0200", "Vervoermiddelen", AccountType.Assets),
        new("1000", "Kas", AccountType.Assets, IsCurrent: true, IsCashEquivalent: true),
        new("1100", "Bank", AccountType.Assets, IsCurrent: true, IsCashEquivalent: true),
        new("1300", "Debiteuren", AccountType.Assets, IsCurrent: true),
        new("1400", "Voorraad", AccountType.Assets, IsCurrent: true, IsInventory: true),
        new("1500", "Vooruitbetaalde kosten", AccountType.Assets, IsCurrent: true),
        new("2000", "Crediteuren", AccountType.Liabilities, IsCurrent: true),
        new("2100", "Te betalen btw", AccountType.Liabilities, IsCurrent: true),
        new("2200", "Te betalen loonheffing", AccountType.Liabilities, IsCurrent: true),
        new("2300", "Overige kortlopende schulden", AccountType.Liabilities, IsCurrent: true),
        new("3000", "Langlopende leningen", AccountType.Liabilities),
        new("4000", "Eigen vermogen", AccountType.Equity),
        new("7000", "Inkoopwaarde van de omzet", AccountType.Costs),
        new("7100", "Lonen en salarissen", AccountType.Costs),
        new("7200", "Sociale lasten", AccountType.Costs),
        new("7300", "Huisvestingskosten", AccountType.Costs),
        new("7400", "Verkoopkosten", AccountType.Costs),
        new("7500", "Autokosten", AccountType.Costs),
        new("7600", "Kantoorkosten", AccountType.Costs),
        new("7700", "Algemene kosten", AccountType.Costs),
        new("7800", "Afschrijvingen", AccountType.Costs),
        new("7900", "Rente- en bankkosten", AccountType.Costs),
        new("8000", "Omzet", AccountType.Revenue),
        new("8100", "Overige opbrengsten", AccountType.Revenue),
        new("8200", "Rentebaten", AccountType.Revenue),
    ];

    /// <summary>The same chart with English account names.</summary>
    public static IReadOnlyList<StarterAccount> International { get; } =
    [
        new("0100", "Fixtures and equipment", AccountType.Assets),
        new("0200", "Vehicles", AccountType.Assets),
        new("1000", "Cash", AccountType.Assets, IsCurrent: true, IsCashEquivalent: true),
        new("1100", "Bank", AccountType.Assets, IsCurrent: true, IsCashEquivalent: true),
        new("1300", "Trade receivables", AccountType.Assets, IsCurrent: true),
        new("1400", "Inventory", AccountType.Assets, IsCurrent: true, IsInventory: true),
        new("1500", "Prepaid expenses", AccountType.Assets, IsCurrent: true),
        new("2000", "Trade payables", AccountType.Liabilities, IsCurrent: true),
        new("2100", "VAT payable", AccountType.Liabilities, IsCurrent: true),
        new("2200", "Payroll tax payable", AccountType.Liabilities, IsCurrent: true),
        new("2300", "Other current liabilities", AccountType.Liabilities, IsCurrent: true),
        new("3000", "Long-term loans", AccountType.Liabilities),
        new("4000", "Equity", AccountType.Equity),
        new("7000", "Cost of goods sold", AccountType.Costs),
        new("7100", "Salaries", AccountType.Costs),
        new("7200", "Social security contributions", AccountType.Costs),
        new("7300", "Housing costs", AccountType.Costs),
        new("7400", "Selling costs", AccountType.Costs),
        new("7500", "Vehicle costs", AccountType.Costs),
        new("7600", "Office costs", AccountType.Costs),
        new("7700", "General expenses", AccountType.Costs),
        new("7800", "Depreciation", AccountType.Costs),
        new("7900", "Interest and bank charges", AccountType.Costs),
        new("8000", "Revenue", AccountType.Revenue),
        new("8100", "Other income", AccountType.Revenue),
        new("8200", "Interest income", AccountType.Revenue),
    ];
}
