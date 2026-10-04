using LedgerSheets.Domain.Banking;

namespace LedgerSheets.Infrastructure.Tests;

/// <summary>
/// Tests for the SharpCoreDB matching rule store: a keyword identifies a rule, so storing a rule for
/// a keyword that already exists replaces it instead of leaving two conflicting rules behind.
/// </summary>
public sealed class SharpCoreDbCounterAccountRuleRepositoryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveAsync_And_AllAsync_RoundTripAllProperties()
    {
        await using var context = new SharpCoreDbTestContext();

        await context.MatchRules.SaveAsync(new CounterAccountRule("albert heijn", "4700", 3), Ct);

        var line = Assert.Single(await context.MatchRules.AllAsync(Ct));
        Assert.Equal("ALBERT HEIJN", line.Keyword);
        Assert.Equal("4700", line.CounterAccountNumber);
        Assert.Equal(3, line.Confirmations);
    }

    [Fact]
    public async Task AllAsync_OrdersByConfirmations()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.MatchRules.SaveAsync(new CounterAccountRule("energie", "4300", 1), Ct);
        await context.MatchRules.SaveAsync(new CounterAccountRule("albert heijn", "4700", 7), Ct);

        var lines = await context.MatchRules.AllAsync(Ct);

        Assert.Collection(
            lines,
            line => Assert.Equal("ALBERT HEIJN", line.Keyword),
            line => Assert.Equal("ENERGIE", line.Keyword));
    }

    [Fact]
    public async Task SaveAsync_ReplacesTheRuleForTheSameKeyword()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.MatchRules.SaveAsync(new CounterAccountRule("albert heijn", "4700", 1), Ct);

        await context.MatchRules.SaveAsync(new CounterAccountRule("albert heijn", "4710", 2), Ct);

        var line = Assert.Single(await context.MatchRules.AllAsync(Ct));
        Assert.Equal("4710", line.CounterAccountNumber);
        Assert.Equal(2, line.Confirmations);
    }

    [Fact]
    public async Task Rules_SurviveReopen()
    {
        await using var context = new SharpCoreDbTestContext();
        await context.MatchRules.SaveAsync(new CounterAccountRule("albert heijn", "4700", 4), Ct);

        await context.HeropenAsync();

        var line = Assert.Single(await context.MatchRules.AllAsync(Ct));
        Assert.Equal("4700", line.CounterAccountNumber);
        Assert.Equal(4, line.Confirmations);
    }

    [Fact]
    public async Task SaveAsync_ThrowsForNull()
    {
        await using var context = new SharpCoreDbTestContext();

        await Assert.ThrowsAsync<ArgumentNullException>(() => context.MatchRules.SaveAsync(null!, Ct));
    }
}
