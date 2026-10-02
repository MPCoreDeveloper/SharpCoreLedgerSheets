using System.Reflection;
using FinOverzicht.Domain.Boekhouden;
using FluentAssertions;
using NetArchTest.Rules;

namespace FinOverzicht.ArchitectureTests;

/// <summary>
/// Guards the layering: dependencies may only point inwards
/// (Web -> Reports/Infrastructure -> Application -> Domain).
/// </summary>
public sealed class LaagAfhankelijkhedenTests
{
    private static readonly Assembly DomainAssembly = typeof(Grootboek).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Services.IRapportageService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Extensions.FinOverzichtInfrastructureExtensions).Assembly;
    private static readonly Assembly ReportsAssembly = typeof(Reports.Abstractions.IReportRenderer).Assembly;
    private static readonly Assembly WebAssembly = typeof(Web.Pages.IndexModel).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherFinOverzichtLayer()
    {
        var resultaat = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FinOverzicht.Application",
                "FinOverzicht.Infrastructure",
                "FinOverzicht.Reports",
                "FinOverzicht.Web")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void Domain_DependsOnNoOuterFramework()
    {
        var resultaat = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("SharpCoreDB", "Microsoft.AspNetCore", "Microsoft.Extensions")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void Application_DependsOnDomainOnly()
    {
        var resultaat = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "FinOverzicht.Infrastructure",
                "FinOverzicht.Reports",
                "FinOverzicht.Web",
                "SharpCoreDB")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void Infrastructure_DependsOnNoOuterLayer()
    {
        var resultaat = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("FinOverzicht.Reports", "FinOverzicht.Web")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void Reports_DependsOnNoInfrastructureOrWeb()
    {
        var resultaat = Types.InAssembly(ReportsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("FinOverzicht.Infrastructure", "FinOverzicht.Web")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void DomainServiceImplementations_AreSealed()
    {
        var resultaat = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Services.IBalansOpsteller))
            .Or()
            .ImplementInterface(typeof(Domain.Services.IKasstroomOpsteller))
            .Should()
            .BeSealed()
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void RepositoryImplementations_AreSealed()
    {
        var resultaat = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.IRekeningRepository))
            .Or()
            .ImplementInterface(typeof(Application.Abstractions.IJournaalpostRepository))
            .Should()
            .BeSealed()
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void ApplicationServiceInterfaces_StartWithI()
    {
        var resultaat = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Meld(resultaat);
    }

    [Fact]
    public void AllLayers_Load()
    {
        // Sanity check: the assemblies under test are the ones we expect.
        DomainAssembly.Should().NotBeNull();
        ApplicationAssembly.GetName().Name.Should().Be("FinOverzicht.Application");
        InfrastructureAssembly.GetName().Name.Should().Be("FinOverzicht.Infrastructure");
        ReportsAssembly.GetName().Name.Should().Be("FinOverzicht.Reports");
        WebAssembly.GetName().Name.Should().Be("FinOverzicht.Web");
    }

    private static void Meld(NetArchTest.Rules.TestResult resultaat) =>
        resultaat.IsSuccessful.Should().BeTrue(
            "overtredingen: {0}",
            resultaat.FailingTypeNames is null ? "(geen)" : string.Join(", ", resultaat.FailingTypeNames));
}
