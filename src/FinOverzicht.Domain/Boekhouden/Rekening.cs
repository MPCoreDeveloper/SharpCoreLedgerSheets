using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// A general-ledger account ("grootboekrekening").
/// </summary>
/// <remarks>
/// The optional flags drive the derived statements: <see cref="IsLiquideMiddel"/> feeds the cash
/// flow statement, <see cref="IsVlottend"/> and <see cref="IsVoorraad"/> feed the liquidity ratios.
/// </remarks>
public sealed class Rekening
{
    /// <summary>Creates a new ledger account.</summary>
    /// <exception cref="DomeinException">Thrown when the number or name is blank.</exception>
    public Rekening(
        Guid id,
        string nummer,
        string naam,
        RekeningSoort soort,
        bool isActief = true,
        bool isVlottend = false,
        bool isLiquideMiddel = false,
        bool isVoorraad = false)
    {
        if (id == Guid.Empty)
        {
            throw new DomeinException("Een rekening moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(nummer))
        {
            throw new DomeinException("Een rekening moet een nummer hebben.");
        }

        if (string.IsNullOrWhiteSpace(naam))
        {
            throw new DomeinException("Een rekening moet een naam hebben.");
        }

        if (isLiquideMiddel && soort != RekeningSoort.Activa)
        {
            throw new DomeinException("Alleen activa kunnen als liquide middel worden gemarkeerd.");
        }

        if (isVoorraad && soort != RekeningSoort.Activa)
        {
            throw new DomeinException("Alleen activa kunnen als voorraad worden gemarkeerd.");
        }

        Id = id;
        Nummer = nummer.Trim();
        Naam = naam.Trim();
        Soort = soort;
        IsActief = isActief;
        IsVlottend = isVlottend;
        IsLiquideMiddel = isLiquideMiddel;
        IsVoorraad = isVoorraad;
    }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The ledger number (for example <c>1000</c>).</summary>
    public string Nummer { get; }

    /// <summary>The human-readable name.</summary>
    public string Naam { get; private set; }

    /// <summary>The account type.</summary>
    public RekeningSoort Soort { get; }

    /// <summary>Whether the account may still be used in new entries.</summary>
    public bool IsActief { get; private set; }

    /// <summary>Whether the account is a current (short-term) asset or liability.</summary>
    public bool IsVlottend { get; private set; }

    /// <summary>Whether the account represents cash or a cash equivalent.</summary>
    public bool IsLiquideMiddel { get; private set; }

    /// <summary>Whether the account represents inventory.</summary>
    public bool IsVoorraad { get; private set; }

    /// <summary>True when the account has a normal debit balance (Activa, Kosten).</summary>
    public bool HeeftDebetSaldo => Soort is RekeningSoort.Activa or RekeningSoort.Kosten;

    /// <summary>Renames the account.</summary>
    public void Hernoem(string naam)
    {
        if (string.IsNullOrWhiteSpace(naam))
        {
            throw new DomeinException("Een rekening moet een naam hebben.");
        }

        Naam = naam.Trim();
    }

    /// <summary>Marks the account as inactive; inactive accounts must not receive new entries.</summary>
    public void Deactiveer() => IsActief = false;

    /// <summary>Reactivates a previously deactivated account.</summary>
    public void Activeer() => IsActief = true;

    /// <summary>Marks or unmarks the account as a current asset/liability.</summary>
    public void ZetVlottend(bool vlottend) => IsVlottend = vlottend;

    /// <summary>Marks or unmarks the account as cash / cash equivalent.</summary>
    public void ZetLiquideMiddel(bool liquideMiddel)
    {
        if (liquideMiddel && Soort != RekeningSoort.Activa)
        {
            throw new DomeinException("Alleen activa kunnen als liquide middel worden gemarkeerd.");
        }

        IsLiquideMiddel = liquideMiddel;
    }
}
