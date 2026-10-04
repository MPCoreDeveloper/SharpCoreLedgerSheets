namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// The five elementary account types of double-entry bookkeeping.
/// </summary>
/// <remarks>
/// Assets and Costs have a normal <em>debit</em> balance; Liabilities, Equity and
/// Revenue have a normal <em>credit</em> balance.
/// </remarks>
public enum AccountType
{
    /// <summary>Assets - what the business owns (normal debit balance).</summary>
    Assets = 0,

    /// <summary>Liabilities - what the business owes (normal credit balance).</summary>
    Liabilities = 1,

    /// <summary>Equity - the owners' stake (normal credit balance).</summary>
    Equity = 2,

    /// <summary>Revenue / income (normal credit balance).</summary>
    Revenue = 3,

    /// <summary>Expenses / costs (normal debit balance).</summary>
    Costs = 4,
}
