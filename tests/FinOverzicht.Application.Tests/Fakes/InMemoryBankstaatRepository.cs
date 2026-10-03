using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IBankstaatRepository"/> used by the application tests.</summary>
internal sealed class InMemoryBankstaatRepository : IBankstaatRepository
{
    private readonly Dictionary<Guid, BankTransactie> _transacties = [];

    public Task<IReadOnlyList<BankTransactie>> AlleAsync(
        BankTransactieStatus? status = null,
        CancellationToken ct = default)
    {
        IEnumerable<BankTransactie> query = _transacties.Values;

        if (status.HasValue)
        {
            query = query.Where(transactie => transactie.Status == status.Value);
        }

        return Task.FromResult<IReadOnlyList<BankTransactie>>(
            query.OrderBy(transactie => transactie.Datum)
                .ThenBy(transactie => transactie.Referentie, StringComparer.Ordinal)
                .ToList());
    }

    public Task<BankTransactie?> ZoekAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_transacties.TryGetValue(id, out var transactie) ? transactie : null);

    public Task<IReadOnlyList<string>> BestaandeSleutelsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(_transacties.Values.Select(t => t.Sleutel).ToList());

    public Task ToevoegenAsync(IEnumerable<BankTransactie> transacties, CancellationToken ct = default)
    {
        foreach (var transactie in transacties)
        {
            _transacties[transactie.Id] = transactie;
        }

        return Task.CompletedTask;
    }

    public Task BijwerkenAsync(BankTransactie transactie, CancellationToken ct = default)
    {
        _transacties[transactie.Id] = transactie;
        return Task.CompletedTask;
    }
}
