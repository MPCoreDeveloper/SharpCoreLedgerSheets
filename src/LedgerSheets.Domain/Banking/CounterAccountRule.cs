using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Banking;

/// <summary>
/// A rule the matching assistant learned from an earlier, confirmed booking: bank movements whose
/// counterparty or description contains <see cref="Keyword"/> belong on
/// <see cref="CounterAccountNumber"/>.
/// </summary>
/// <remarks>
/// A rule only ever comes from an explicit confirmation by the user, so the assistant can never
/// invent a booking rule on its own. <see cref="Confirmations"/> counts how often the user agreed
/// with the rule; when several rules match the same movement the most confirmed one wins.
/// </remarks>
public sealed record CounterAccountRule
{
    /// <summary>
    /// The smallest useful keyword. Shorter keywords would match almost every movement and turn a
    /// suggestion into noise.
    /// </summary>
    public const int MinLengthKeyword = 3;

    /// <summary>Creates a matching rule.</summary>
    /// <exception cref="DomainException">Thrown when the keyword or the account number is unusable.</exception>
    public CounterAccountRule(string keyword, string counterAccountNumber, int confirmations = 1)
    {
        if (string.IsNullOrWhiteSpace(counterAccountNumber))
        {
            throw new DomainException("Een matchingregel moet naar een grootboekrekening verwijzen.");
        }

        var normalized = TextNormalization.Normalize(keyword);

        if (normalized.Length < MinLengthKeyword)
        {
            throw new DomainException(
                $"Een matchingregel moet een trefwoord van minimaal {MinLengthKeyword} tekens hebben.");
        }

        if (confirmations < 1)
        {
            throw new DomainException("Een matchingregel moet minstens één keer zijn bevestigd.");
        }

        Keyword = normalized;
        CounterAccountNumber = counterAccountNumber.Trim();
        Confirmations = confirmations;
    }

    /// <summary>The keyword, normalised: an IBAN or a fragment of a name or description.</summary>
    public string Keyword { get; }

    /// <summary>The ledger number of the counterparty account.</summary>
    public string CounterAccountNumber { get; }

    /// <summary>How often the user confirmed this rule.</summary>
    public int Confirmations { get; private init; }

    /// <summary>Returns this rule with one more confirmation.</summary>
    public CounterAccountRule Confirmed() => this with { Confirmations = Confirmations + 1 };

    /// <summary>Returns this rule pointing at another account, keeping the keyword.</summary>
    public CounterAccountRule ForAccount(string counterAccountNumber) =>
        new(Keyword, counterAccountNumber, Confirmations);

    /// <summary>
    /// Normalises a keyword the same way <see cref="Keyword"/> is normalised, so callers can look
    /// up or compare rules without having to know the normalisation rules.
    /// </summary>
    public static string KeywordFrom(string? text) => TextNormalization.Normalize(text);
}
