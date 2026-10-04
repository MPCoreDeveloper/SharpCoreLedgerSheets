using System.Reflection;
using LedgerSheets.Domain.Accounting;
using NetArchTest.Rules;

namespace LedgerSheets.ArchitectureTests;

/// <summary>
/// Guards the layering: dependencies may only point inwards
/// (Web -> Reports/Infrastructure -> Application -> Domain).
/// </summary>
public sealed class LayerDependenciesTests
{
    private static readonly Assembly DomainAssembly = typeof(Ledger).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Services.IReportingService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Extensions.LedgerSheetsInfrastructureExtensions).Assembly;
    private static readonly Assembly ReportsAssembly = typeof(Reports.Abstractions.IReportRenderer).Assembly;
    private static readonly Assembly WebAssembly = typeof(Web.Pages.IndexModel).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLedgerSheetsLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "LedgerSheets.Application",
                "LedgerSheets.Infrastructure",
                "LedgerSheets.Reports",
                "LedgerSheets.Web")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void Domain_DependsOnNoOuterFramework()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("SharpCoreDB", "Microsoft.AspNetCore", "Microsoft.Extensions")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void Application_DependsOnDomainOnly()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "LedgerSheets.Infrastructure",
                "LedgerSheets.Reports",
                "LedgerSheets.Web",
                "SharpCoreDB")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void Infrastructure_DependsOnNoOuterLayer()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("LedgerSheets.Reports", "LedgerSheets.Web")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void Reports_DependsOnNoInfrastructureOrWeb()
    {
        var result = Types.InAssembly(ReportsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("LedgerSheets.Infrastructure", "LedgerSheets.Web")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void DomainServiceImplementations_AreSealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Services.IBalanceSheetBuilder))
            .Or()
            .ImplementInterface(typeof(Domain.Services.ICashFlowBuilder))
            .Should()
            .BeSealed()
            .GetResult();

        Report(result);
    }

    [Fact]
    public void RepositoryImplementations_AreSealed()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.IAccountRepository))
            .Or()
            .ImplementInterface(typeof(Application.Abstractions.IJournalEntryRepository))
            .Should()
            .BeSealed()
            .GetResult();

        Report(result);
    }

    [Fact]
    public void ApplicationServiceInterfaces_StartWithI()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Report(result);
    }

    [Fact]
    public void AllLayers_Load()
    {
        // Sanity check: the assemblies under test are the ones we expect.
        Assert.NotNull(DomainAssembly);
        Assert.Equal("LedgerSheets.Application", ApplicationAssembly.GetName().Name);
        Assert.Equal("LedgerSheets.Infrastructure", InfrastructureAssembly.GetName().Name);
        Assert.Equal("LedgerSheets.Reports", ReportsAssembly.GetName().Name);
        Assert.Equal("LedgerSheets.Web", WebAssembly.GetName().Name);
    }

    private static void Report(NetArchTest.Rules.TestResult result)
    {
        var violations = result.FailingTypeNames is null
            ? "(geen)"
            : string.Join(", ", result.FailingTypeNames);

        Assert.True(result.IsSuccessful, $"overtredingen: {violations}");
    }
}
