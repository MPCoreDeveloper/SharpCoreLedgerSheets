using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// A general-ledger account ("ledgerAccount").
/// </summary>
/// <remarks>
/// The optional flags drive the derived statements: <see cref="IsCashEquivalent"/> feeds the cash
/// flow statement, <see cref="IsCurrent"/> and <see cref="IsInventory"/> feed the liquidity ratios.
/// </remarks>
public sealed class Account
{
    /// <summary>Creates a new ledger account.</summary>
    /// <exception cref="DomainException">Thrown when the number or name is blank.</exception>
    public Account(
        Guid id,
        string number,
        string name,
        AccountType type,
        bool isActive = true,
        bool isCurrent = false,
        bool isCashEquivalent = false,
        bool isInventory = false)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Een rekening moet een uniek id hebben.");
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("Een rekening moet een nummer hebben.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Een rekening moet een naam hebben.");
        }

        if (isCashEquivalent && type != AccountType.Assets)
        {
            throw new DomainException("Alleen activa kunnen als liquide middel worden gemarkeerd.");
        }

        if (isInventory && type != AccountType.Assets)
        {
            throw new DomainException("Alleen activa kunnen als voorraad worden gemarkeerd.");
        }

        Id = id;
        Number = number.Trim();
        Name = name.Trim();
        Type = type;
        IsActive = isActive;
        IsCurrent = isCurrent;
        IsCashEquivalent = isCashEquivalent;
        IsInventory = isInventory;
    }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>The ledger number (for example <c>1000</c>).</summary>
    public string Number { get; }

    /// <summary>The human-readable name.</summary>
    public string Name { get; private set; }

    /// <summary>The account type.</summary>
    public AccountType Type { get; }

    /// <summary>Whether the account may still be used in new entries.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Whether the account is a current (short-term) asset or liability.</summary>
    public bool IsCurrent { get; private set; }

    /// <summary>Whether the account represents cash or a cash equivalent.</summary>
    public bool IsCashEquivalent { get; private set; }

    /// <summary>Whether the account represents inventory.</summary>
    public bool IsInventory { get; private set; }

    /// <summary>True when the account has a normal debit balance (Assets, Costs).</summary>
    public bool HasDebitBalance => Type is AccountType.Assets or AccountType.Costs;

    /// <summary>Renames the account.</summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Een rekening moet een naam hebben.");
        }

        Name = name.Trim();
    }

    /// <summary>Marks the account as inactive; inactive accounts must not receive new entries.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated account.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Marks or unmarks the account as a current asset/liability.</summary>
    public void SetCurrent(bool current) => IsCurrent = current;

    /// <summary>Marks or unmarks the account as cash / cash equivalent.</summary>
    public void SetCashEquivalent(bool cashEquivalent)
    {
        if (cashEquivalent && Type != AccountType.Assets)
        {
            throw new DomainException("Alleen activa kunnen als liquide middel worden gemarkeerd.");
        }

        IsCashEquivalent = cashEquivalent;
    }
}
