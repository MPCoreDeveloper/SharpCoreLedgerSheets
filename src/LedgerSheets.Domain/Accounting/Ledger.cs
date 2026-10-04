using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Accounting;

/// <summary>
/// The general ledger ("ledger"): the collection of accounts and journal entries that is the
/// single source of truth for the balance sheet, the profit and loss statement and the cash flow
/// statement.
/// </summary>
public sealed class Ledger
{
    private readonly Dictionary<Guid, Account> _accounts = [];
    private readonly List<JournalEntry> _entries = [];

    /// <summary>Creates an empty ledger in the given currency (euro by default).</summary>
    public Ledger(Currency? currency = null) => Currency = currency ?? Currency.Eur;

    /// <summary>The reporting currency of this ledger.</summary>
    public Currency Currency { get; }

    /// <summary>All accounts in the ledger (insertion order).</summary>
    public IReadOnlyCollection<Account> Accounts => _accounts.Values;

    /// <summary>All posted journal entries (booking order).</summary>
    public IReadOnlyList<JournalEntry> Entries => _entries.AsReadOnly();

    /// <summary>Adds a new account; the number must be unique within the ledger.</summary>
    /// <exception cref="DomainException">Thrown when the number already exists.</exception>
    public Account AddAccount(
        string number,
        string name,
        AccountType type,
        bool isCurrent = false,
        bool isCashEquivalent = false,
        bool isInventory = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        if (_accounts.Values.Any(r => string.Equals(r.Number, number, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"Rekeningnummer '{number}' bestaat al in het grootboek.");
        }

        var account = new Account(Guid.NewGuid(), number, name, type, true, isCurrent, isCashEquivalent, isInventory);
        _accounts.Add(account.Id, account);
        return account;
    }

    /// <summary>Finds an account by its ledger number.</summary>
    public Account? FindAccount(string number) =>
        _accounts.Values.FirstOrDefault(r => string.Equals(r.Number, number, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Re-registers an existing account (used when rehydrating a ledger from storage).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the id or number is already present.</exception>
    public void RegisterAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (_accounts.ContainsKey(account.Id))
        {
            throw new DomainException("Deze rekening is al opgenomen in het grootboek.");
        }

        if (_accounts.Values.Any(r => string.Equals(r.Number, account.Number, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"Rekeningnummer '{account.Number}' bestaat al in het grootboek.");
        }

        _accounts.Add(account.Id, account);
    }

    /// <summary>
    /// Posts a journal entry after validating it (account existence, account active, in balance).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the entry is invalid or already posted.</exception>
    public void Post(JournalEntry post) => Place(post, checkActive: true);

    /// <summary>
    /// Rehydrates an already-posted journal entry (used when loading a ledger from storage).
    /// Skips the "account must be active" check because historical entries may reference
    /// accounts that have since been deactivated; balance and account existence are still enforced.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the entry is invalid or already present.</exception>
    public void Restore(JournalEntry post) => Place(post, checkActive: false);

    /// <summary>
    /// Returns the signed balance of an account (positive = debit balance) up to and including
    /// the given date. When <paramref name="through"/> is <see langword="null"/> all entries count.
    /// </summary>
    public decimal Balance(Guid accountId, DateOnly? through = null)
    {
        var threshold = through ?? DateOnly.MaxValue;
        return _entries
            .Where(p => p.Date <= threshold)
            .SelectMany(p => p.Lines)
            .Where(r => r.AccountId == accountId)
            .Sum(r => r.Movement);
    }

    /// <summary>Returns the signed movement of an account within a period (positive = debit).</summary>
    public decimal MovementInPeriod(Guid accountId, Period period)
    {
        return _entries
            .Where(p => period.Contains(p.Date))
            .SelectMany(p => p.Lines)
            .Where(r => r.AccountId == accountId)
            .Sum(r => r.Movement);
    }

    /// <summary>The total signed balance of all accounts; zero for a consistent ledger.</summary>
    public decimal CheckTotal() =>
        _entries.SelectMany(p => p.Lines).Sum(r => r.Movement);

    private void Place(JournalEntry post, bool checkActive)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (_entries.Any(p => p.Id == post.Id))
        {
            throw new DomainException("Deze journaalpost is al geboekt.");
        }

        foreach (var line in post.Lines)
        {
            if (!_accounts.TryGetValue(line.AccountId, out var account))
            {
                throw new DomainException("De journaalpost verwijst naar een onbekende rekening.");
            }

            if (checkActive && !account.IsActive)
            {
                throw new DomainException($"Rekening {account.Number} ({account.Name}) is niet actief.");
            }
        }

        post.Validate();
        _entries.Add(post);
    }
}
