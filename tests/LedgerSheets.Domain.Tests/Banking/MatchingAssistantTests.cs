using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Accounting;

namespace LedgerSheets.Domain.Tests.Banking;

public sealed class MatchingAssistantTests
{
    private const string BankNumber = "1100";
    private static readonly DateOnly Date = new(2026, 4, 2);

    private readonly MatchingAssistant _assistant = new();

    [Fact]
    public void WithoutSignal_GivesNoProposal()
    {
        var proposal = _assistant.Propose(
            Transaction(name: "Onbekende Partij", description: "Betaling zonder trefwoord"),
            [Bank(), Costs(), Omzet()],
            []);

        Assert.Null(proposal);
    }

    [Fact]
    public void IbanLine_GivesCompleteConfidence()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", iban: "NL91ABNA0417164300", description: "Boodschappen"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("NL91ABNA0417164300", "4000", 4)]);

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal("Bedrijfskosten", proposal.CounterAccountName);
        Assert.Equal(100, proposal.Confidence);
        Assert.True(proposal.IsReliable);
        Assert.Contains("NL91ABNA0417164300", proposal.Reason);
    }

    [Fact]
    public void IbanLine_IgnoresOtherGroeperingFromTheIban()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", iban: "NL91 ABNA 0417 1643 00"),
            [Bank(), Costs()],
            [new CounterAccountRule("nl91abna0417164300", "4000")]);

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal(100, proposal.Confidence);
    }

    [Fact]
    public void KeywordRule_InTheDescription_ProposesWithNinety()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", description: "Boodschappen winkel 42"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("albert heijn", "4000")]);

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal(90, proposal.Confidence);
        Assert.Contains("ALBERT HEIJN", proposal.Reason);
    }

    [Fact]
    public void KeywordRule_InTheCounterpartyName_Proposes()
    {
        var proposal = Proposal(
            Transaction(name: "Gemeente Amsterdam", description: "Leges"),
            [Bank(), Costs()],
            [new CounterAccountRule("GEMEENTE", "4000")]);

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal(90, proposal.Confidence);
    }

    [Fact]
    public void IbanLine_WinsFromKeywordRule()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", iban: "NL91ABNA0417164300", description: "Payment"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("PAYMENT", "8000"), new CounterAccountRule("NL91ABNA0417164300", "4000")]);

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal(100, proposal.Confidence);
    }

    [Fact]
    public void MeesteConfirmations_Wins()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", description: "Boodschappen"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("ALBERT HEIJN", "4000", 1), new CounterAccountRule("ALBERT HEIJN", "8000", 5)]);

        Assert.Equal("8000", proposal.CounterAccountNumber);
    }

    [Fact]
    public void LongestKeyword_WinsOnEqualConfirmations()
    {
        var proposal = Proposal(
            Transaction(name: "Albert Heijn", description: "Boodschappen"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("ALBERT", "4000"), new CounterAccountRule("ALBERT HEIJN", "8000")]);

        Assert.Equal("Omzet", proposal.CounterAccountName);
    }

    [Fact]
    public void AccountNameInTheDescription_ProposesWithSeventy()
    {
        var proposal = Proposal(
            Transaction(name: "Betaalvereniging", description: "PINBETALING OMZET augustus"),
            [Bank(), Costs(), Omzet()],
            []);

        Assert.Equal("8000", proposal.CounterAccountNumber);
        Assert.Equal(70, proposal.Confidence);
        Assert.False(proposal.IsReliable);
        Assert.Contains("Omzet", proposal.Reason);
    }

    [Fact]
    public void AccountNumberAsSeparateWord_Proposes()
    {
        var proposal = Proposal(
            Transaction(name: "Onbekende Partij", description: "Betaling 8000 ten behoeve van derden"),
            [Bank(), Costs(), Omzet()],
            []);

        Assert.Equal("8000", proposal.CounterAccountNumber);
        Assert.Equal(70, proposal.Confidence);
    }

    [Fact]
    public void AccountNumberWithinALargerNumber_DoesNotPropose()
    {
        var proposal = _assistant.Propose(
            Transaction(name: "Onbekende Partij", description: "Factuur 128000"),
            [Bank(), Costs(), Omzet()],
            []);

        Assert.Null(proposal);
    }

    [Fact]
    public void InactiveAccount_IsSkippedAfterWhichAWeakerSignalFollows()
    {
        var deprecated = new Account(Guid.NewGuid(), "4100", "Oude kostenrekening", AccountType.Costs, isActive: false);

        var proposal = Proposal(
            Transaction(name: "Albert Heijn", description: "PINBETALING OMZET"),
            [Bank(), Costs(), Omzet(), deprecated],
            [new CounterAccountRule("ALBERT HEIJN", "4100", 9)]);

        Assert.Equal("8000", proposal.CounterAccountNumber);
        Assert.Equal(70, proposal.Confidence);
    }

    [Fact]
    public void LineToAnUnknownAccount_GivesNoProposal()
    {
        var proposal = _assistant.Propose(
            Transaction(name: "Albert Heijn", description: "Boodschappen"),
            [Bank(), Costs(), Omzet()],
            [new CounterAccountRule("ALBERT HEIJN", "9999")]);

        Assert.Null(proposal);
    }

    [Fact]
    public void TheBankAccountItself_IsNeverProposed()
    {
        var cash = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);

        var proposal = _assistant.Propose(
            Transaction(name: null, description: "Geldopname automaat"),
            [Bank(), Costs(), cash],
            [new CounterAccountRule("GELDOPNAME", BankNumber)]);

        Assert.Null(proposal);
    }

    [Fact]
    public void OwnAccountParameter_DeterminesWhichAccountIsExcluded()
    {
        var cash = new Account(Guid.NewGuid(), "1000", "Kas", AccountType.Assets, isCurrent: true, isCashEquivalent: true);

        var proposal = Proposal(
            Transaction(name: null, description: "Geldopname automaat", bankAccount: "1200"),
            [Bank(), Costs(), cash],
            [new CounterAccountRule("GELDOPNAME", "4000")],
            bankAccountNumber: "1200");

        Assert.Equal("4000", proposal.CounterAccountNumber);
        Assert.Equal(90, proposal.Confidence);
    }

    [Fact]
    public void WithoutActiveAccounts_GivesNoProposal()
    {
        var proposal = _assistant.Propose(
            Transaction(name: "Albert Heijn", description: "Boodschappen"),
            [Bank(active: false), Costs(active: false)],
            [new CounterAccountRule("ALBERT HEIJN", "4000")]);

        Assert.Null(proposal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        var transaction = Transaction();

        Assert.Throws<ArgumentNullException>(() => _assistant.Propose(null!, [Bank()], []));
        Assert.Throws<ArgumentNullException>(() => _assistant.Propose(transaction, null!, []));
        Assert.Throws<ArgumentNullException>(() => _assistant.Propose(transaction, [Bank()], null!));
    }

    private static BankTransaction Transaction(
        string? name = null,
        string? iban = null,
        string? description = null,
        string bankAccount = BankNumber) =>
        new(
            Guid.NewGuid(),
            bankAccount,
            Date,
            new Amount(-25m, Currency.Eur),
            Guid.NewGuid().ToString("N"),
            name,
            iban,
            description);

    private static Account Bank(bool active = true) =>
        new(
            Guid.NewGuid(),
            BankNumber,
            "Bank",
            AccountType.Assets,
            isActive: active,
            isCurrent: true,
            isCashEquivalent: true);

    private static Account Costs(bool active = true) =>
        new(Guid.NewGuid(), "4000", "Bedrijfskosten", AccountType.Costs, isActive: active);

    private static Account Omzet(bool active = true) =>
        new(Guid.NewGuid(), "8000", "Omzet", AccountType.Revenue, isActive: active);

    private PostingProposal Proposal(
        BankTransaction transaction,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<CounterAccountRule> lines,
        string? bankAccountNumber = null) =>
        _assistant.Propose(transaction, accounts, lines, bankAccountNumber)
        ?? throw new InvalidOperationException("De matching-assistent had een voorstel moeten doen.");
}
