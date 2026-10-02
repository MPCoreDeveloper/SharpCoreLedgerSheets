using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Services;

namespace FinOverzicht.Reports.Tests;

public sealed class ReportRegistryTests
{
    private static readonly string[] StandaardRapportSleutels =
    [
        ReportSleutels.Overzicht,
        ReportSleutels.Balans,
        ReportSleutels.WinstVerlies,
        ReportSleutels.Kasstroom,
    ];

    private readonly ReportRegistry _registry = new();

    [Fact]
    public void Registreer_AddsReport()
    {
        _registry.Registreer("Test", "Balans", "Activa", "BalansReport");

        var rapport = _registry.Zoek("Test");

        Assert.NotNull(rapport);
        Assert.Equal("BalansReport", rapport.TemplateNaam);
        Assert.Contains(ReportFormat.Html, rapport.Formaten);
    }

    [Fact]
    public void Registreer_IsCaseInsensitiveForDuplicates()
    {
        _registry.Registreer("Test", "Balans", "Activa", "BalansReport");

        Assert.Throws<InvalidOperationException>(
            () => _registry.Registreer("test", "Balans", "Activa", "BalansReport"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registreer_ThrowsForBlankKey(string sleutel)
    {
        Assert.Throws<ArgumentException>(
            () => _registry.Registreer(sleutel, "Balans", "Activa", "BalansReport"));
    }

    [Fact]
    public void Zoek_ReturnsNullForUnknownKey()
    {
        Assert.Null(_registry.Zoek("Onbekend"));
    }

    [Fact]
    public void MetStandaardRapporten_RegistersFourReports()
    {
        var registry = ReportRegistry.MetStandaardRapporten();

        Assert.Equal(4, registry.Alle().Count);
        Assert.Equal(StandaardRapportSleutels, registry.Alle().Select(r => r.Sleutel));
    }

    [Fact]
    public void MetStandaardRapporten_EveryTemplateNameIsRegistered()
    {
        var registry = ReportRegistry.MetStandaardRapporten();

        Assert.All(registry.Alle(), r => Assert.False(string.IsNullOrWhiteSpace(r.TemplateNaam)));
    }
}
