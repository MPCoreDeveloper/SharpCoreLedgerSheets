namespace FinOverzicht.Domain.Rapporteren;

/// <summary>
/// A single line in a financial statement: an optional account number, a description and an amount.
/// </summary>
/// <remarks>
/// The optional classification flags are copied from the underlying <c>Rekening</c> so that
/// derived calculations (for example the liquidity ratios) can run on report data alone.
/// </remarks>
public sealed record SaldoRegel(string? Nummer, string Omschrijving, decimal Bedrag)
{
    /// <summary>Whether the underlying account is a current (short-term) asset or liability.</summary>
    public bool Vlottend { get; init; }

    /// <summary>Whether the underlying account represents inventory.</summary>
    public bool Voorraad { get; init; }

    /// <summary>Whether the underlying account represents cash or a cash equivalent.</summary>
    public bool LiquideMiddel { get; init; }
}
