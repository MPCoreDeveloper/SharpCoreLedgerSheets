using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Boekhouden;

/// <summary>
/// The general ledger ("grootboek"): the collection of accounts and journal entries that is the
/// single source of truth for the balance sheet, the profit and loss statement and the cash flow
/// statement.
/// </summary>
public sealed class Grootboek
{
    private readonly Dictionary<Guid, Rekening> _rekeningen = [];
    private readonly List<Journaalpost> _posten = [];

    /// <summary>Creates an empty ledger in the given currency (euro by default).</summary>
    public Grootboek(Valuta? valuta = null) => Valuta = valuta ?? Valuta.Eur;

    /// <summary>The reporting currency of this ledger.</summary>
    public Valuta Valuta { get; }

    /// <summary>All accounts in the ledger (insertion order).</summary>
    public IReadOnlyCollection<Rekening> Rekeningen => _rekeningen.Values;

    /// <summary>All posted journal entries (booking order).</summary>
    public IReadOnlyList<Journaalpost> Posten => _posten.AsReadOnly();

    /// <summary>Adds a new account; the number must be unique within the ledger.</summary>
    /// <exception cref="DomeinException">Thrown when the number already exists.</exception>
    public Rekening VoegRekeningToe(
        string nummer,
        string naam,
        RekeningSoort soort,
        bool isVlottend = false,
        bool isLiquideMiddel = false,
        bool isVoorraad = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nummer);

        if (_rekeningen.Values.Any(r => string.Equals(r.Nummer, nummer, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomeinException($"Rekeningnummer '{nummer}' bestaat al in het grootboek.");
        }

        var rekening = new Rekening(Guid.NewGuid(), nummer, naam, soort, true, isVlottend, isLiquideMiddel, isVoorraad);
        _rekeningen.Add(rekening.Id, rekening);
        return rekening;
    }

    /// <summary>Finds an account by its ledger number.</summary>
    public Rekening? ZoekRekening(string nummer) =>
        _rekeningen.Values.FirstOrDefault(r => string.Equals(r.Nummer, nummer, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Re-registers an existing account (used when rehydrating a ledger from storage).
    /// </summary>
    /// <exception cref="DomeinException">Thrown when the id or number is already present.</exception>
    public void RegistreerRekening(Rekening rekening)
    {
        ArgumentNullException.ThrowIfNull(rekening);

        if (_rekeningen.ContainsKey(rekening.Id))
        {
            throw new DomeinException("Deze rekening is al opgenomen in het grootboek.");
        }

        if (_rekeningen.Values.Any(r => string.Equals(r.Nummer, rekening.Nummer, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomeinException($"Rekeningnummer '{rekening.Nummer}' bestaat al in het grootboek.");
        }

        _rekeningen.Add(rekening.Id, rekening);
    }

    /// <summary>
    /// Posts a journal entry after validating it (account existence, account active, in balance).
    /// </summary>
    /// <exception cref="DomeinException">Thrown when the entry is invalid or already posted.</exception>
    public void Boek(Journaalpost post) => Plaats(post, controleerActief: true);

    /// <summary>
    /// Rehydrates an already-posted journal entry (used when loading a ledger from storage).
    /// Skips the "account must be active" check because historical entries may reference
    /// accounts that have since been deactivated; balance and account existence are still enforced.
    /// </summary>
    /// <exception cref="DomeinException">Thrown when the entry is invalid or already present.</exception>
    public void Herstel(Journaalpost post) => Plaats(post, controleerActief: false);

    /// <summary>
    /// Returns the signed balance of an account (positive = debit balance) up to and including
    /// the given date. When <paramref name="totEnMet"/> is <see langword="null"/> all entries count.
    /// </summary>
    public decimal Saldo(Guid rekeningId, DateOnly? totEnMet = null)
    {
        var grens = totEnMet ?? DateOnly.MaxValue;
        return _posten
            .Where(p => p.Datum <= grens)
            .SelectMany(p => p.Regels)
            .Where(r => r.RekeningId == rekeningId)
            .Sum(r => r.Mutatie);
    }

    /// <summary>Returns the signed movement of an account within a period (positive = debit).</summary>
    public decimal MutatieInPeriode(Guid rekeningId, Periode periode)
    {
        return _posten
            .Where(p => periode.Bevat(p.Datum))
            .SelectMany(p => p.Regels)
            .Where(r => r.RekeningId == rekeningId)
            .Sum(r => r.Mutatie);
    }

    /// <summary>The total signed balance of all accounts; zero for a consistent ledger.</summary>
    public decimal ControleTotaal() =>
        _posten.SelectMany(p => p.Regels).Sum(r => r.Mutatie);

    private void Plaats(Journaalpost post, bool controleerActief)
    {
        ArgumentNullException.ThrowIfNull(post);

        if (_posten.Any(p => p.Id == post.Id))
        {
            throw new DomeinException("Deze journaalpost is al geboekt.");
        }

        foreach (var regel in post.Regels)
        {
            if (!_rekeningen.TryGetValue(regel.RekeningId, out var rekening))
            {
                throw new DomeinException("De journaalpost verwijst naar een onbekende rekening.");
            }

            if (controleerActief && !rekening.IsActief)
            {
                throw new DomeinException($"Rekening {rekening.Nummer} ({rekening.Naam}) is niet actief.");
            }
        }

        post.Valideer();
        _posten.Add(post);
    }
}
