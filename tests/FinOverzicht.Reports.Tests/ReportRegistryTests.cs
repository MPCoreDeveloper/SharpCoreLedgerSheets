using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Models;
using FinOverzicht.Reports.Services;
using FluentAssertions;

namespace FinOverzicht.Reports.Tests;

public sealed class ReportRegistryTests
{
    private readonly ReportRegistry _registry = new();

    [Fact]
    public void Registreer_AddsReport()
    {
        _registry.Registreer("Test", "Balans", "Activa", "BalansReport");

        var rapport = _registry.Zoek("Test");

        rapport.Should().NotBeNull();
        rapport!.TemplateNaam.Should().Be("BalansReport");
        rapport.Formaten.Should().Contain(ReportFormat.Html);
    }

    [Fact]
    public void Registreer_IsCaseInsensitiveForDuplicates()
    {
        _registry.Registreer("Test", "Balans", "Activa", "BalansReport");

        var act = () => _registry.Registreer("test", "Balans", "Activa", "BalansReport");

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registreer_ThrowsForBlankKey(string sleutel)
    {
        var act = () => _registry.Registreer(sleutel, "Balans", "Activa", "BalansReport");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Zoek_ReturnsNullForUnknownKey()
    {
        _registry.Zoek("Onbekend").Should().BeNull();
    }

    [Fact]
    public void MetStandaardRapporten_RegistersFourReports()
    {
        var registry = ReportRegistry.MetStandaardRapporten();

        registry.Alle().Should().HaveCount(4);
        registry.Alle().Select(r => r.Sleutel).Should().BeEquivalentTo(
            ReportSleutels.Overzicht,
            ReportSleutels.Balans,
            ReportSleutels.WinstVerlies,
            ReportSleutels.Kasstroom);
    }

    [Fact]
    public void MetStandaardRapporten_EveryTemplateNameIsRegistered()
    {
        var registry = ReportRegistry.MetStandaardRapporten();

        registry.Alle().Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.TemplateNaam));
    }
}
