namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// The five elementary account types of double-entry bookkeeping.
/// </summary>
/// <remarks>
/// Activa and Kosten have a normal <em>debit</em> balance; Passiva, EigenVermogen and
/// Opbrengsten have a normal <em>credit</em> balance.
/// </remarks>
public enum RekeningSoort
{
    /// <summary>Assets - what the business owns (normal debit balance).</summary>
    Activa = 0,

    /// <summary>Liabilities - what the business owes (normal credit balance).</summary>
    Passiva = 1,

    /// <summary>Equity - the owners' stake (normal credit balance).</summary>
    EigenVermogen = 2,

    /// <summary>Revenue / income (normal credit balance).</summary>
    Opbrengsten = 3,

    /// <summary>Expenses / costs (normal debit balance).</summary>
    Kosten = 4,
}
