using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IBankStatementService"/>.
/// </summary>
/// <remarks>
/// The service orchestrates the bank import: it picks a reader for the file, filters out movements
/// that are already known, asks the <see cref="IMatchingAssistant"/> for a suggestion, and posts the
/// journal entry through <see cref="IJournalEntryService"/> so that every booking rule stays in one
/// place. A confirmed booking is remembered as a <see cref="CounterAccountRule"/>, which is how the
/// assistant improves while the administration grows.
/// </remarks>
public sealed class BankStatementService(
    IBankStatementRepository bankStatementRepository,
    ICounterAccountRuleRepository lineRepository,
    IAccountRepository accountRepository,
    IJournalEntryService journalEntryService,
    IMatchingAssistant matchingAssistant,
    IEnumerable<IBankStatementImporter> importers) : IBankStatementService
{
    /// <inheritdoc />
    public async Task<BankStatementImportResult> ImportAsync(
        BankStatementImportCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Content);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.BankAccountNumber);

        var bankAccount = await FindActiveAccountAsync(command.BankAccountNumber, ct).ConfigureAwait(false);
        var importer = ChooseImporter(command);
        var read = importer.Read(command.Content, bankAccount.Number);

        // The keys that are already stored make the import idempotent; adding to the set also filters
        // out a movement that appears twice inside one file.
        var known = (await bankStatementRepository.ExistingKeysAsync(ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        var added = read.Where(transaction => known.Add(transaction.Key)).ToList();

        if (added.Count > 0)
        {
            await bankStatementRepository.AddAsync(added, ct).ConfigureAwait(false);
        }

        return new BankStatementImportResult(
            importer.Format,
            read.Count,
            added.Count,
            read.Count - added.Count,
            added);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BankTransaction>> AllAsync(
        BankTransactionStatus? status = null,
        CancellationToken ct = default) =>
        bankStatementRepository.AllAsync(status, ct);

    /// <inheritdoc />
    public async Task<PostingProposal?> ProposalAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await FindTransactionAsync(transactionId, ct).ConfigureAwait(false);
        var proposals = await ProposalsAsync([transaction], ct).ConfigureAwait(false);

        return proposals.Count > 0 ? proposals[0] : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PostingProposal>> ProposalsAsync(
        IReadOnlyList<BankTransaction> transactions,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        if (transactions.Count == 0)
        {
            return [];
        }

        // The ledger and the learned rules are read once for the whole list: a statement of a few
        // hundred movements must not turn into a few hundred round trips for the same two tables.
        var accounts = await accountRepository.AllAsync(ct).ConfigureAwait(false);
        var lines = await lineRepository.AllAsync(ct).ConfigureAwait(false);

        var proposals = new List<PostingProposal>(transactions.Count);

        foreach (var transaction in transactions)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            if (matchingAssistant.Propose(transaction, accounts, lines) is { } proposal)
            {
                proposals.Add(proposal);
            }
        }

        return proposals;
    }

    /// <inheritdoc />
    public async Task<JournalEntry> PostAsync(
        Guid transactionId,
        string counterAccountNumber,
        string? description = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(counterAccountNumber);

        var transaction = await FindTransactionAsync(transactionId, ct).ConfigureAwait(false);

        if (transaction.Status != BankTransactionStatus.Open)
        {
            throw new DomainException(MessageCode.MovementAlreadyBooked, "De bankmutatie van {0:dd-MM-yyyy} is al verwerkt.", transaction.Date);
        }

        var bankAccount = await FindActiveAccountAsync(transaction.BankAccountNumber, ct).ConfigureAwait(false);
        var counterAccount = await FindActiveAccountAsync(counterAccountNumber, ct).ConfigureAwait(false);

        if (string.Equals(bankAccount.Number, counterAccount.Number, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(MessageCode.CounterAccountIsBankAccount, "De tegenrekening van een bankmutatie mag niet de bankrekening zelf zijn.");
        }

        var amount = transaction.AmountWithoutSign.Value;
        var text = description ?? DetermineDescription(transaction);

        // Received money debits the bank account, paid money credits it; the counterparty takes the
        // other side. That is exactly the entry a bookkeeper would make by hand.
        IReadOnlyList<JournalLineCommand> lines = transaction.IsCredit
            ?
            [
                new JournalLineCommand(bankAccount.Number, amount, 0m, text),
                new JournalLineCommand(counterAccount.Number, 0m, amount, text),
            ]
            :
            [
                new JournalLineCommand(counterAccount.Number, amount, 0m, text),
                new JournalLineCommand(bankAccount.Number, 0m, amount, text),
            ];

        var entry = await journalEntryService.PostAsync(
            new PostJournalEntryCommand(transaction.Date, text, lines, transaction.Reference),
            ct).ConfigureAwait(false);

        await bankStatementRepository.UpdateAsync(transaction.MarkPosted(entry.Id), ct).ConfigureAwait(false);
        await RememberAsync(transaction, counterAccount.Number, ct).ConfigureAwait(false);

        return entry;
    }

    /// <inheritdoc />
    public async Task IgnoreAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await FindTransactionAsync(transactionId, ct).ConfigureAwait(false);

        if (transaction.Status == BankTransactionStatus.Posted)
        {
            throw new DomainException(MessageCode.BookedMovementCannotBeIgnored, "Een geboekte bankmutatie kan niet worden genegeerd.");
        }

        await bankStatementRepository.UpdateAsync(transaction.Ignore(), ct).ConfigureAwait(false);
    }

    /// <summary>Remembers a confirmed booking as a matching rule for the next import.</summary>
    private async Task RememberAsync(BankTransaction transaction, string counterAccountNumber, CancellationToken ct)
    {
        // The IBAN is the most specific keyword; without one the counterparty's name is the best we
        // have. A very short keyword is skipped: it would match almost every movement.
        var keyword = CounterAccountRule.KeywordFrom(transaction.CounterpartyIban ?? transaction.CounterpartyName);

        if (keyword.Length < CounterAccountRule.MinLengthKeyword)
        {
            return;
        }

        var existing = (await lineRepository.AllAsync(ct).ConfigureAwait(false))
            .FirstOrDefault(line => string.Equals(line.Keyword, keyword, StringComparison.Ordinal));

        // Booking the same keyword on another account is a deliberate correction: the rule moves with
        // it and counts as one more confirmation.
        var line = existing is null
            ? new CounterAccountRule(keyword, counterAccountNumber)
            : existing.ForAccount(counterAccountNumber).Confirmed();

        await lineRepository.SaveAsync(line, ct).ConfigureAwait(false);
    }

    /// <summary>The description of the journal entry: who it was and what it was for.</summary>
    private static string DetermineDescription(BankTransaction transaction)
    {
        var parts = new[] { transaction.CounterpartyName, transaction.Description }
            .Where(divide => !string.IsNullOrWhiteSpace(divide));

        var description = string.Join(" - ", parts);

        return description.Length > 0 ? description : $"Bankmutatie {transaction.Reference}";
    }

    /// <summary>Picks the reader: the forced format, or the first reader that recognises the file.</summary>
    private IBankStatementImporter ChooseImporter(BankStatementImportCommand command)
    {
        if (command.Format is { } format)
        {
            return importers.FirstOrDefault(importer => importer.Format == format)
                ?? throw new DomainException(MessageCode.NoImporterForFormat, "Er is geen lezer voor het format '{0}'.", format);
        }

        return importers.FirstOrDefault(
                importer => importer.CanProcess(command.FileName, command.Content))
            ?? throw new DomainException(
                MessageCode.UnsupportedStatementFile,
                "Het bestand '{0}' is geen ondersteund bankafschrift (CAMT.053 of CSV).",
                command.FileName);
    }

    private async Task<BankTransaction> FindTransactionAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Geef een geldig id op.", nameof(id));
        }

        return await bankStatementRepository.FindAsync(id, ct).ConfigureAwait(false)
            ?? throw new NotFoundException(MessageCode.MovementNotFound, "Bankmutatie '{0}' is niet gevonden.", id);
    }

    private async Task<Account> FindActiveAccountAsync(string number, CancellationToken ct)
    {
        var account = await accountRepository.FindOnNumberAsync(number, ct).ConfigureAwait(false)
            ?? throw new NotFoundException(MessageCode.AccountNotFound, "Rekening '{0}' is niet gevonden.", number);

        if (!account.IsActive)
        {
            throw new DomainException(MessageCode.AccountInactive, "Rekening '{0}' ({1}) is niet actief.", account.Number, account.Name);
        }

        return account;
    }
}
