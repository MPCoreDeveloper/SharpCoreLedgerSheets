using SharpCoreLedgerSheets.Domain.Accounting;

namespace SharpCoreLedgerSheets.Domain.Tests.Accounting;

/// <summary>
/// The starter charts are content, but bad content breaks the first click of a new administration, so
/// the invariants are checked here instead of being discovered by a user.
/// </summary>
public sealed class StarterChartTests
{
    [Theory]
    [InlineData(StarterChart.DutchMkb)]
    [InlineData(StarterChart.International)]
    public void For_ReturnsAccountsWithUniqueNumbers(StarterChart chart)
    {
        var accounts = StarterCharts.For(chart);

        Assert.NotEmpty(accounts);
        Assert.Equal(
            accounts.Count,
            accounts.Select(account => account.Number).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData(StarterChart.DutchMkb)]
    [InlineData(StarterChart.International)]
    public void For_CoversEveryAccountType(StarterChart chart) =>
        Assert.Equal(
            Enum.GetValues<AccountType>().Length,
            StarterCharts.For(chart).Select(account => account.Type).Distinct().Count());

    [Theory]
    [InlineData(StarterChart.DutchMkb)]
    [InlineData(StarterChart.International)]
    public void For_ContainsCashSoTheCashFlowStatementWorks(StarterChart chart) =>
        Assert.Contains(StarterCharts.For(chart), account => account.IsCashEquivalent);

    [Theory]
    [InlineData(StarterChart.DutchMkb)]
    [InlineData(StarterChart.International)]
    public void For_ProducesAccountsTheDomainAccepts(StarterChart chart)
    {
        // Account itself validates the flag rules (cash and inventory only on assets), so a template
        // that would be rejected fails here rather than on the user's first click.
        foreach (var template in StarterCharts.For(chart))
        {
            _ = new Account(
                Guid.NewGuid(),
                template.Number,
                template.Name,
                template.Type,
                isActive: true,
                isCurrent: template.IsCurrent,
                isCashEquivalent: template.IsCashEquivalent,
                isInventory: template.IsInventory);
        }
    }

    [Fact]
    public void BothCharts_ShareTheSameNumbering()
    {
        Assert.Equal(
            StarterCharts.DutchMkb.Select(account => account.Number),
            StarterCharts.International.Select(account => account.Number));
    }

    [Fact]
    public void For_ThrowsForAnUnknownChart() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => StarterCharts.For((StarterChart)99));
}
