using FinOverzicht.Domain.Common;

namespace FinOverzicht.Domain.Bankzaken;

/// <summary>
/// A rule the matching assistant learned from an earlier, confirmed booking: bank movements whose
/// counterparty or description contains <see cref="Trefwoord"/> belong on
/// <see cref="TegenrekeningNummer"/>.
/// </summary>
/// <remarks>
/// A rule only ever comes from an explicit confirmation by the user, so the assistant can never
/// invent a booking rule on its own. <see cref="Bevestigingen"/> counts how often the user agreed
/// with the rule; when several rules match the same movement the most confirmed one wins.
/// </remarks>
public sealed record TegenrekeningRegel
{
    /// <summary>
    /// The smallest useful keyword. Shorter keywords would match almost every movement and turn a
    /// suggestion into noise.
    /// </summary>
    public const int MinLengteTrefwoord = 3;

    /// <summary>Creates a matching rule.</summary>
    /// <exception cref="DomeinException">Thrown when the keyword or the account number is unusable.</exception>
    public TegenrekeningRegel(string trefwoord, string tegenrekeningNummer, int bevestigingen = 1)
    {
        if (string.IsNullOrWhiteSpace(tegenrekeningNummer))
        {
            throw new DomeinException("Een matchingregel moet naar een grootboekrekening verwijzen.");
        }

        var genormaliseerd = TekstNormalisatie.Normaliseer(trefwoord);

        if (genormaliseerd.Length < MinLengteTrefwoord)
        {
            throw new DomeinException(
                $"Een matchingregel moet een trefwoord van minimaal {MinLengteTrefwoord} tekens hebben.");
        }

        if (bevestigingen < 1)
        {
            throw new DomeinException("Een matchingregel moet minstens één keer zijn bevestigd.");
        }

        Trefwoord = genormaliseerd;
        TegenrekeningNummer = tegenrekeningNummer.Trim();
        Bevestigingen = bevestigingen;
    }

    /// <summary>The keyword, normalised: an IBAN or a fragment of a name or description.</summary>
    public string Trefwoord { get; }

    /// <summary>The ledger number of the counterparty account.</summary>
    public string TegenrekeningNummer { get; }

    /// <summary>How often the user confirmed this rule.</summary>
    public int Bevestigingen { get; private init; }

    /// <summary>Returns this rule with one more confirmation.</summary>
    public TegenrekeningRegel Bevestigd() => this with { Bevestigingen = Bevestigingen + 1 };

    /// <summary>Returns this rule pointing at another account, keeping the keyword.</summary>
    public TegenrekeningRegel VoorRekening(string tegenrekeningNummer) =>
        new(Trefwoord, tegenrekeningNummer, Bevestigingen);

    /// <summary>
    /// Normalises a keyword the same way <see cref="Trefwoord"/> is normalised, so callers can look
    /// up or compare rules without having to know the normalisation rules.
    /// </summary>
    public static string TrefwoordVan(string? tekst) => TekstNormalisatie.Normaliseer(tekst);
}
