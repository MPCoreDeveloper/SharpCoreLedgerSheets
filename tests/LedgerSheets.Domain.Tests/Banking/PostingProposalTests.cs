using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Common;

namespace LedgerSheets.Domain.Tests.Banking;

public sealed class PostingProposalTests
{
    [Fact]
    public void Proposal_ContainsAllOnderdelen()
    {
        var transactionId = Guid.NewGuid();

        var proposal = new PostingProposal(transactionId, "4000", "Bedrijfskosten", 70, "De omschrijving noemt 'X'.");

        Assert.Equal(transactionId, proposal.TransactionId);
        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal("Bedrijfskosten", proposal.CounterAccountName);
        Assert.Equal(70, proposal.Confidence);
        Assert.False(proposal.IsReliable);
    }

    [Theory]
    [InlineData(90, true)]
    [InlineData(100, true)]
    [InlineData(89, false)]
    public void IsReliable_FromNinety(int confidence, bool expected)
    {
        var proposal = new PostingProposal(Guid.NewGuid(), "4000", "Bedrijfskosten", confidence, "Omdat.");

        Assert.Equal(expected, proposal.IsReliable);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ConfidenceOutsideBereik_IsRejected(int confidence)
    {
        var exception = Assert.Throws<DomainException>(
            () => new PostingProposal(Guid.NewGuid(), "4000", "Bedrijfskosten", confidence, "Omdat."));

        Assert.Contains("tussen 0 en 100", exception.Message);
    }

    [Fact]
    public void WithoutTransaction_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => new PostingProposal(Guid.Empty, "4000", "Bedrijfskosten", 70, "Omdat."));

        Assert.Contains("banktransactie", exception.Message);
    }

    [Fact]
    public void WithoutCounterAccount_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => new PostingProposal(Guid.NewGuid(), "  ", "Bedrijfskosten", 70, "Omdat."));

        Assert.Contains("tegenrekening", exception.Message);
    }

    [Fact]
    public void WithoutReason_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => new PostingProposal(Guid.NewGuid(), "4000", "Bedrijfskosten", 70, "   "));

        Assert.Contains("waarom", exception.Message);
    }
}
