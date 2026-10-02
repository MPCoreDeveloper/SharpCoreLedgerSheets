using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Domain.Tests;

/// <summary>
/// Builds a small but complete ledger used across the domain tests.
/// </summary>
/// <remarks>
/// Scenario (all entries dated 2026):
/// <list type="number">
///   <item>start capital: cash 10.000 / equity 10.000</item>
///   <item>bank loan: bank 5.000 / loan 5.000</item>
///   <item>inventory on credit: inventory 2.000 / creditors 2.000</item>
///   <item>cash sale: cash 4.000 / revenue 4.000</item>
///   <item>cost of goods sold: expense 1.200 / inventory 1.200</item>
///   <item>machine bought from bank: machinery 3.000 / bank 3.000</item>
///   <item>expense paid from bank: expense 500 / bank 500</item>
///   <item>loan repayment from bank: loan 1.000 / bank 1.000</item>
/// </list>
/// Resulting totals: assets 18.300, liabilities 6.000, equity 10.000, result 2.300.
/// </remarks>
internal static class TestGrootboek
{
    public const string Kas = "1000";
    public const string Bank = "1100";
    public const string Voorraad = "1300";
    public const string Machines = "1500";
    public const string Crediteuren = "2000";
    public const string Banklening = "2100";
    public const string EigenVermogen = "3000";
    public const string Kosten = "4000";
    public const string Omzet = "8000";

    /// <summary>2026 is the financial year used by the scenario.</summary>
    public static readonly DateOnly Datum = new(2026, 6, 15);

    /// <summary>Full-year period for the scenario.</summary>
    public static Periode Jaar2026 => Periode.VoorJaar(2026);

    /// <summary>Creates the ledger with the complete scenario.</summary>
    public static Grootboek MetScenario()
    {
        var grootboek = new Grootboek();

        var kas = grootboek.VoegRekeningToe(Kas, "Kas", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var bank = grootboek.VoegRekeningToe(Bank, "Bank", RekeningSoort.Activa, isVlottend: true, isLiquideMiddel: true);
        var voorraad = grootboek.VoegRekeningToe(Voorraad, "Voorraad", RekeningSoort.Activa, isVlottend: true, isVoorraad: true);
        var machines = grootboek.VoegRekeningToe(Machines, "Machines", RekeningSoort.Activa);
        var crediteuren = grootboek.VoegRekeningToe(Crediteuren, "Crediteuren", RekeningSoort.Passiva, isVlottend: true);
        var banklening = grootboek.VoegRekeningToe(Banklening, "Banklening", RekeningSoort.Passiva);
        var eigenVermogen = grootboek.VoegRekeningToe(EigenVermogen, "Eigen vermogen", RekeningSoort.EigenVermogen);
        var kosten = grootboek.VoegRekeningToe(Kosten, "Bedrijfskosten", RekeningSoort.Kosten);
        var omzet = grootboek.VoegRekeningToe(Omzet, "Omzet", RekeningSoort.Opbrengsten);

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Startkapitaal")
            .Debet(kas.Id, 10_000m)
            .Credit(eigenVermogen.Id, 10_000m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Banklening ontvangen")
            .Debet(bank.Id, 5_000m)
            .Credit(banklening.Id, 5_000m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Inkoop voorraad op rekening")
            .Debet(voorraad.Id, 2_000m)
            .Credit(crediteuren.Id, 2_000m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Contante verkoop")
            .Debet(kas.Id, 4_000m)
            .Credit(omzet.Id, 4_000m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Inkoopwaarde verkopen")
            .Debet(kosten.Id, 1_200m)
            .Credit(voorraad.Id, 1_200m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Machine aangeschaft")
            .Debet(machines.Id, 3_000m)
            .Credit(bank.Id, 3_000m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Bedrijfskosten betaald")
            .Debet(kosten.Id, 500m)
            .Credit(bank.Id, 500m));

        grootboek.Boek(Journaalpost.Nieuw(Datum, "Aflossing banklening")
            .Debet(banklening.Id, 1_000m)
            .Credit(bank.Id, 1_000m));

        return grootboek;
    }

    /// <summary>Creates an empty ledger with only two accounts (debit/credit pair).</summary>
    public static (Grootboek Grootboek, Rekening Debet, Rekening Credit) MetTweeRekeningen(
        RekeningSoort debetSoort = RekeningSoort.Activa,
        RekeningSoort creditSoort = RekeningSoort.Passiva)
    {
        var grootboek = new Grootboek();
        var debet = grootboek.VoegRekeningToe("9999", "Debetrekening", debetSoort);
        var credit = grootboek.VoegRekeningToe("8888", "Creditrekening", creditSoort);
        return (grootboek, debet, credit);
    }
}
