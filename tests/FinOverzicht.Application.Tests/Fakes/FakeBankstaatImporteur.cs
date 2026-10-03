using System.Globalization;
using FinOverzicht.Application.Abstractions;
using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;

namespace FinOverzicht.Application.Tests.Fakes;

/// <summary>
/// Stub <see cref="IBankstaatImporteur"/> that reads a one-line-per-movement test format:
/// <c>datum;bedrag;referentie;tegenpartij;iban;omschrijving</c>.
/// </summary>
/// <remarks>
/// The application tests must not depend on the infrastructure layer, so the real CAMT.053 and CSV
/// readers are replaced by this stub. Only the extension decides whether the reader applies, which is
/// enough to test the selection of a reader.
/// </remarks>
internal sealed class FakeBankstaatImporteur(string extensie, BankstaatFormaat formaat) : IBankstaatImporteur
{
    /// <summary>The extension of the test format for which this stub applies.</summary>
    public string Extensie { get; } = extensie;

    public BankstaatFormaat Formaat { get; } = formaat;

    public bool KanVerwerken(string bestandsnaam, string inhoud) =>
        bestandsnaam.EndsWith(Extensie, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<BankTransactie> Lees(
        string inhoud,
        string bankRekeningNummer,
        Valuta? standaardValuta = null)
    {
        var valuta = standaardValuta ?? Valuta.Eur;
        var transacties = new List<BankTransactie>();

        foreach (var regel in inhoud.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var delen = regel.Split(';');

            if (delen.Length < 3)
            {
                throw new DomeinException($"Onleesbare regel: '{regel}'.");
            }

            transacties.Add(new BankTransactie(
                Guid.NewGuid(),
                bankRekeningNummer,
                DateOnly.ParseExact(delen[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                new Bedrag(decimal.Parse(delen[1], CultureInfo.InvariantCulture), valuta),
                delen[2],
                Deel(delen, 3),
                Deel(delen, 4),
                Deel(delen, 5)));
        }

        return transacties;
    }

    private static string? Deel(string[] delen, int index) =>
        delen.Length > index && delen[index].Length > 0 ? delen[index] : null;
}
