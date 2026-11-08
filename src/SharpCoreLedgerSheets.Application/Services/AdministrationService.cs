using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IAdministrationService"/>.
/// </summary>
/// <remarks>
/// Opening balances are stored as one reserved journal entry, dated the day before the book year starts and
/// tagged with the reference <c>OPENING-&lt;year&gt;</c>. That keeps a single source of truth: the balances
/// are part of the ledger, so the statements pick them up without special cases, they are visible in the
/// journal, and they obey the same rules as every other booking.
/// </remarks>
public sealed class AdministrationService(
    ISettingsRepository settingsRepository,
    IAccountRepository accountRepository,
    IJournalEntryRepository journalEntryRepository,
    IJournalEntryService journalEntryService) : IAdministrationService
{
    /// <summary>Prefix of the reserved reference that marks an opening-balance entry.</summary>
    public const string OpeningReferencePrefix = "OPENING-";

    /// <summary>Returns the reserved reference of the opening balance of a book year.</summary>
    public static string OpeningReferenceFor(int year) => $"{OpeningReferencePrefix}{year}";

    /// <inheritdoc />
    public Task<AdministrationSettings> GetSettingsAsync(CancellationToken ct = default) =>
        settingsRepository.GetAsync(ct);

    /// <inheritdoc />
    public Task SaveSettingsAsync(AdministrationSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settingsRepository.SaveAsync(settings, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OpeningBalanceLine>> OpeningBalancesAsync(int year, CancellationToken ct = default)
    {
        var entry = await FindOpeningEntryAsync(year, ct).ConfigureAwait(false);

        if (entry is null)
        {
            return [];
        }

        var numbers = (await accountRepository.AllAsync(ct).ConfigureAwait(false))
            .ToDictionary(account => account.Id, account => account.Number);

        return entry.Lines
            .Where(line => numbers.ContainsKey(line.AccountId))
            .Select(line => new OpeningBalanceLine(numbers[line.AccountId], line.Debit, line.Credit))
            .ToList();
    }

    /// <inheritdoc />
    public async Task ReplaceOpeningBalancesAsync(
        int year,
        IReadOnlyList<OpeningBalanceLine> lines,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var settings = await settingsRepository.GetAsync(ct).ConfigureAwait(false);
        var totalDebit = lines.Sum(line => line.Debit);
        var totalCredit = lines.Sum(line => line.Credit);

        if (totalDebit != totalCredit)
        {
            throw new DomainException(
                MessageCode.OpeningBalanceNotBalanced,
                "De beginsaldi zijn niet in balans: debet {0} versus credit {1}.",
                totalDebit,
                totalCredit);
        }

        foreach (var line in lines)
        {
            if (line.Debit < 0m || line.Credit < 0m)
            {
                throw new DomainException(MessageCode.AmountMustBePositive, "Een bedrag mag niet negatief zijn.");
            }

            if (line.Debit > 0m && line.Credit > 0m)
            {
                throw new DomainException(
                    MessageCode.LineDebitAndCredit,
                    "Een boekingsregel mag niet zowel debet als credit bevatten.");
            }
        }

        var existing = await FindOpeningEntryAsync(year, ct).ConfigureAwait(false);

        if (existing is not null)
        {
            await journalEntryRepository.RemoveAsync(existing.Id, ct).ConfigureAwait(false);
        }

        var filled = lines.Where(line => line.Debit > 0m || line.Credit > 0m).ToList();

        if (filled.Count == 0)
        {
            // An empty set clears the opening balances of the year; the entry above has been removed.
            return;
        }

        var start = settings.PeriodFor(year).Start;

        await journalEntryService.PostAsync(
            new PostJournalEntryCommand(
                start.AddDays(-1),
                $"Beginsaldo {year}",
                filled.Select(line => new JournalLineCommand(line.AccountNumber, line.Debit, line.Credit)).ToList(),
                OpeningReferenceFor(year)),
            ct).ConfigureAwait(false);
    }

    private async Task<JournalEntry?> FindOpeningEntryAsync(int year, CancellationToken ct)
    {
        var reference = OpeningReferenceFor(year);
        var entries = await journalEntryRepository.AllAsync(ct: ct).ConfigureAwait(false);

        return entries.FirstOrDefault(
            entry => string.Equals(entry.Reference, reference, StringComparison.Ordinal));
    }
}
