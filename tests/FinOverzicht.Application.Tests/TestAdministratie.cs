using FinOverzicht.Application.Models;
using FinOverzicht.Application.Services;
using FinOverzicht.Application.Tests.Fakes;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;

namespace FinOverzicht.Application.Tests;

/// <summary>
/// Test harness that wires the application services on top of in-memory repositories.
/// </summary>
internal sealed class TestAdministratie
{
    private static readonly DateOnly ScenarioDatum = new(2026, 6, 15);

    public TestAdministratie()
    {
        RekeningService = new RekeningService(Rekeningen);
        JournaalpostService = new JournaalpostService(Rekeningen, Posten);
        Lader = new GrootboekLader(Rekeningen, Posten);
        Rapportage = new RapportageService(
            Lader,
            new BalansOpsteller(),
            new WinstVerliesOpsteller(),
            new KasstroomOpsteller(),
            new RatioBerekening());
    }

    public InMemoryRekeningRepository Rekeningen { get; } = new();

    public InMemoryJournaalpostRepository Posten { get; } = new();

    public RekeningService RekeningService { get; }

    public JournaalpostService JournaalpostService { get; }

    public GrootboekLader Lader { get; }

    public RapportageService Rapportage { get; }

    /// <summary>Financial year used by the seed data.</summary>
    public static Periode Jaar2026 => Periode.VoorJaar(2026);

    /// <summary>Seeds the same scenario as the domain tests.</summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("1000", "Kas", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("1100", "Bank", RekeningSoort.Activa, Vlottend: true, LiquideMiddel: true), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("1300", "Voorraad", RekeningSoort.Activa, Vlottend: true, Voorraad: true), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("1500", "Machines", RekeningSoort.Activa), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("2000", "Crediteuren", RekeningSoort.Passiva, Vlottend: true), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("2100", "Banklening", RekeningSoort.Passiva), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("3000", "Eigen vermogen", RekeningSoort.EigenVermogen), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("4000", "Bedrijfskosten", RekeningSoort.Kosten), ct);
        await RekeningService.VoegToeAsync(new RekeningToevoegenCommand("8000", "Omzet", RekeningSoort.Opbrengsten), ct);

        await BoekAsync("Startkapitaal", ("1000", 10_000m, 0m), ("3000", 0m, 10_000m), ct);
        await BoekAsync("Banklening ontvangen", ("1100", 5_000m, 0m), ("2100", 0m, 5_000m), ct);
        await BoekAsync("Inkoop voorraad op rekening", ("1300", 2_000m, 0m), ("2000", 0m, 2_000m), ct);
        await BoekAsync("Contante verkoop", ("1000", 4_000m, 0m), ("8000", 0m, 4_000m), ct);
        await BoekAsync("Inkoopwaarde verkopen", ("4000", 1_200m, 0m), ("1300", 0m, 1_200m), ct);
        await BoekAsync("Machine aangeschaft", ("1500", 3_000m, 0m), ("1100", 0m, 3_000m), ct);
        await BoekAsync("Bedrijfskosten betaald", ("4000", 500m, 0m), ("1100", 0m, 500m), ct);
        await BoekAsync("Aflossing banklening", ("2100", 1_000m, 0m), ("1100", 0m, 1_000m), ct);
    }

    private Task BoekAsync(
        string omschrijving,
        (string Nummer, decimal Debet, decimal Credit) debet,
        (string Nummer, decimal Debet, decimal Credit) credit,
        CancellationToken ct)
    {
        var regels = new List<BoekingsregelCommand>
        {
            new(debet.Nummer, debet.Debet, debet.Credit),
            new(credit.Nummer, credit.Debet, credit.Credit),
        };

        return JournaalpostService.BoekAsync(
            new JournaalpostBoekenCommand(ScenarioDatum, omschrijving, regels), ct);
    }
}
