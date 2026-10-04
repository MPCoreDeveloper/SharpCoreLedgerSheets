using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Domain.Banking;

/// <summary>
/// Default <see cref="IMatchingAssistant"/>.
/// </summary>
/// <remarks>
/// It uses three signals, in order of decreasing specificity:
/// <list type="number">
///   <item>an earlier confirmed booking for the same counterparty IBAN;</item>
///   <item>an earlier confirmed booking for a keyword in the counterparty or the description;</item>
///   <item>the name, or the standalone ledger number, of an account that occurs in the description.</item>
/// </list>
/// The assistant never guesses: when none of these signals yields an active account it returns
/// <see langword="null"/> and the user chooses the account themselves.
/// </remarks>
public sealed class MatchingAssistant : IMatchingAssistant
{
    /// <summary>Certainty of a rule learned on the counterparty's IBAN.</summary>
    private const int ConfidenceIban = 100;

    /// <summary>Certainty of a rule learned on a keyword.</summary>
    private const int ConfidenceKeyword = 90;

    /// <summary>Certainty of an account name or number that occurs in the description.</summary>
    private const int ConfidenceAccount = 70;

    /// <inheritdoc />
    public PostingProposal? Propose(
        BankTransaction transaction,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<CounterAccountRule> lines,
        string? bankAccountNumber = null)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(lines);

        var ownAccount = bankAccountNumber ?? transaction.BankAccountNumber;

        var kandidaten = accounts
            .Where(account => account.IsActive && !IsSameAccount(account, ownAccount))
            .ToList();

        if (kandidaten.Count == 0)
        {
            return null;
        }

        var iban = TextNormalization.Iban(transaction.CounterpartyIban);

        if (iban.Length > 0)
        {
            var onIban = BestLine(
                lines.Where(line => string.Equals(
                    TextNormalization.Iban(line.Keyword), iban, StringComparison.Ordinal)),
                kandidaten);

            if (onIban is not null)
            {
                return Proposal(
                    transaction,
                    onIban.Value.Account,
                    ConfidenceIban,
                    $"Eerder geboekt voor tegenpartij {iban}.");
            }
        }

        var onKeyword = BestLine(
            lines.Where(line => TextNormalization.Contains(transaction.SearchText, line.Keyword)),
            kandidaten);

        if (onKeyword is not null)
        {
            return Proposal(
                transaction,
                onKeyword.Value.Account,
                ConfidenceKeyword,
                $"Trefwoord '{onKeyword.Value.Line.Keyword}' is eerder op deze rekening geboekt.");
        }

        var onName = OnName(transaction.SearchText, kandidaten);

        return onName is null
            ? null
            : Proposal(
                transaction,
                onName.Value.Account,
                ConfidenceAccount,
                $"De omschrijving noemt '{onName.Value.SearchTerm}'.");
    }

    /// <summary>
    /// Picks the rule that should win and resolves it to an account. Rules pointing at an account
    /// that no longer exists or is inactive are skipped, so a stale rule cannot block a proposal.
    /// </summary>
    private static (CounterAccountRule Line, Account Account)? BestLine(
        IEnumerable<CounterAccountRule> lines,
        IReadOnlyList<Account> kandidaten)
    {
        foreach (var line in lines
            .OrderByDescending(line => line.Confirmations)
            .ThenByDescending(line => line.Keyword.Length))
        {
            var account = kandidaten.FirstOrDefault(
                candidate => IsSameAccount(candidate, line.CounterAccountNumber));

            if (account is not null)
            {
                return (line, account);
            }
        }

        return null;
    }

    /// <summary>Finds an account whose name, or whose ledger number as a separate word, is mentioned.</summary>
    private static (Account Account, string SearchTerm)? OnName(
        string searchText,
        IReadOnlyList<Account> kandidaten)
    {
        var words = TextNormalization.Words(searchText);

        foreach (var account in kandidaten
            .OrderByDescending(account => account.Name.Length)
            .ThenBy(account => account.Number, StringComparer.Ordinal))
        {
            if (TextNormalization.Contains(searchText, TextNormalization.Normalize(account.Name)))
            {
                return (account, account.Name);
            }

            if (words.Contains(TextNormalization.Normalize(account.Number), StringComparer.Ordinal))
            {
                return (account, account.Number);
            }
        }

        return null;
    }

    private static bool IsSameAccount(Account account, string number) =>
        string.Equals(account.Number, number, StringComparison.OrdinalIgnoreCase);

    private static PostingProposal Proposal(
        BankTransaction transaction,
        Account account,
        int confidence,
        string reason) =>
        new(transaction.Id, account.Number, account.Name, confidence, reason);
}
