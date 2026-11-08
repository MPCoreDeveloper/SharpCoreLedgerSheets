using SharpCoreLedgerSheets.Application.Exceptions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Tests.Services;

public sealed class BankStatementServiceTests
{
    private const string Bank = "1100";
    private const string Costs = "4000";
    private const string Omzet = "8000";

    private const string AlbertHeijn = "2026-04-02;-12.50;REF-0001;Albert Heijn;NL91ABNA0417164300;Boodschappen";
    private const string Pinbetaling = "2026-04-03;250.00;REF-0002;Betaalvereniging;;PINBETALING OMZET";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ImportAsync_ReadsAndSavesNewMovements()
    {
        var administration = await WithAdministrationAsync();

        var result = await ImportAsync(administration, $"{AlbertHeijn}\n{Pinbetaling}");

        Assert.Equal(BankStatementFormat.Csv, result.Format);
        Assert.Equal(2, result.Read);
        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Skipped);
        Assert.False(result.AllAlreadyKnown);
        Assert.Equal(2, result.New.Count);
        Assert.Equal(2, (await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct)).Count);
    }

    [Fact]
    public async Task ImportAsync_SkipsAlreadyKnownMovements()
    {
        var administration = await WithAdministrationAsync();
        await ImportAsync(administration, $"{AlbertHeijn}\n{Pinbetaling}");

        var result = await ImportAsync(administration, $"{AlbertHeijn}\n{Pinbetaling}");

        Assert.Equal(2, result.Read);
        Assert.Equal(0, result.Added);
        Assert.Equal(2, result.Skipped);
        Assert.True(result.AllAlreadyKnown);
        Assert.Empty(result.New);
    }

    [Fact]
    public async Task ImportAsync_SkipsADuplicateLineWithinAFile()
    {
        var administration = await WithAdministrationAsync();

        var result = await ImportAsync(administration, $"{AlbertHeijn}\n{AlbertHeijn}");

        Assert.Equal(2, result.Read);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task ImportAsync_ChoosesTheReaderOnFileName()
    {
        var administration = await WithAdministrationAsync();

        var result = await ImportAsync(administration, AlbertHeijn, "afschrift.fake");

        Assert.Equal(BankStatementFormat.Csv, result.Format);
    }

    [Fact]
    public async Task ImportAsync_ChoosesTheReaderOnTheSpecifiedFormat()
    {
        var administration = await WithAdministrationAsync();

        var result = await administration.BankStatementService.ImportAsync(
            new BankStatementImportCommand("afschrift.fake", AlbertHeijn, Bank, BankStatementFormat.Camt053),
            Ct);

        Assert.Equal(BankStatementFormat.Camt053, result.Format);
    }

    [Fact]
    public async Task ImportAsync_WithoutSuitableReader_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => ImportAsync(administration, AlbertHeijn, "afschrift.pdf"));

        Assert.Contains("geen ondersteund bankafschrift", exception.Message);
    }

    [Fact]
    public async Task ImportAsync_UnknownBankAccount_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => administration.BankStatementService.ImportAsync(
                new BankStatementImportCommand("afschrift.fake", AlbertHeijn, "9999"),
                Ct));

        Assert.Contains("9999", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ImportAsync_BlankFileName_IsRejected(string fileName)
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ImportAsync(administration, AlbertHeijn, fileName));
    }

    [Fact]
    public async Task ImportAsync_ConnectsTheMovementToTheBankAccount()
    {
        var administration = await WithAdministrationAsync();

        await ImportAsync(administration, AlbertHeijn);

        var movement = Assert.Single(await administration.BankStatementService.AllAsync(null, Ct));
        Assert.Equal(Bank, movement.BankAccountNumber);
        Assert.Equal(BankTransactionStatus.Open, movement.Status);
    }

    [Fact]
    public async Task PostAsync_ADepreciation_DebitsTheCounterAccount()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        var entry = await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        var bank = await administration.Accounts.FindOnNumberAsync(Bank, Ct);
        var costs = await administration.Accounts.FindOnNumberAsync(Costs, Ct);

        Assert.Equal(new DateOnly(2026, 4, 2), entry.Date);
        Assert.Equal("REF-0001", entry.Reference);
        Assert.True(entry.IsBalanced);
        Assert.Equal(12.50m, entry.Lines.Single(line => line.AccountId == costs!.Id).Debit);
        Assert.Equal(12.50m, entry.Lines.Single(line => line.AccountId == bank!.Id).Credit);
    }

    [Fact]
    public async Task PostAsync_ACredit_DebitsTheBankAccount()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration, Pinbetaling);

        var entry = await administration.BankStatementService.PostAsync(movement.Id, Omzet, ct: Ct);

        var bank = await administration.Accounts.FindOnNumberAsync(Bank, Ct);
        var revenue = await administration.Accounts.FindOnNumberAsync(Omzet, Ct);

        Assert.Equal(250.00m, entry.Lines.Single(line => line.AccountId == bank!.Id).Debit);
        Assert.Equal(250.00m, entry.Lines.Single(line => line.AccountId == revenue!.Id).Credit);
    }

    [Fact]
    public async Task PostAsync_MarksTheMovementAsPosted()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        var entry = await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        var posted = Assert.Single(
            await administration.BankStatementService.AllAsync(BankTransactionStatus.Posted, Ct));

        Assert.Equal(entry.Id, posted.JournalEntryId);
        Assert.Empty(await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct));
        Assert.Single(await administration.Entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task PostAsync_UsedTheDescriptionFromTheMovement()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        var entry = await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        Assert.Equal("Albert Heijn - Boodschappen", entry.Description);
    }

    [Fact]
    public async Task PostAsync_TwoTimesSameMovement_IsRejected()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct));

        Assert.Contains("al verwerkt", exception.Message);
    }

    [Fact]
    public async Task PostAsync_OnTheBankAccountItself_IsRejected()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.BankStatementService.PostAsync(movement.Id, Bank, ct: Ct));

        Assert.Contains("bankrekening zelf", exception.Message);
    }

    [Fact]
    public async Task PostAsync_UnknownCounterAccount_IsRejected()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        await Assert.ThrowsAsync<NotFoundException>(
            () => administration.BankStatementService.PostAsync(movement.Id, "9999", ct: Ct));
    }

    [Fact]
    public async Task PostAsync_InactiveCounterAccount_IsRejected()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);
        await administration.AccountService.DeactivateAsync(Costs, Ct);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct));

        Assert.Contains("niet actief", exception.Message);
    }

    [Fact]
    public async Task PostAsync_UnknownMovement_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => administration.BankStatementService.PostAsync(Guid.NewGuid(), Costs, ct: Ct));
    }

    [Fact]
    public async Task PostAsync_WithoutId_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administration.BankStatementService.PostAsync(Guid.Empty, Costs, ct: Ct));
    }

    [Fact]
    public async Task IgnoreAsync_SetTheMovementOnIgnored()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);

        await administration.BankStatementService.IgnoreAsync(movement.Id, Ct);

        Assert.Empty(await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct));
        Assert.Single(await administration.BankStatementService.AllAsync(BankTransactionStatus.Ignored, Ct));
        Assert.Empty(await administration.Entries.AllAsync(ct: Ct));
    }

    [Fact]
    public async Task IgnoreAsync_APostedMovement_IsRejected()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => administration.BankStatementService.IgnoreAsync(movement.Id, Ct));

        Assert.Contains("niet worden genegeerd", exception.Message);
    }

    [Fact]
    public async Task ProposalAsync_WithoutSignal_GivesNoProposal()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(
            administration,
            "2026-04-04;-31.00;REF-0009;Onbekende Partij;NL99RABO0123456789;Betaling");

        Assert.Null(await administration.BankStatementService.ProposalAsync(movement.Id, Ct));
    }

    [Fact]
    public async Task ProposalAsync_LearnsFromAPreviousPosting()
    {
        var administration = await WithAdministrationAsync();
        var first = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(first.Id, Costs, ct: Ct);
        var second = await OpenMovementAsync(
            administration,
            "2026-04-05;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen");

        var proposal = await administration.BankStatementService.ProposalAsync(second.Id, Ct);

        Assert.NotNull(proposal);
        Assert.Equal(Costs, proposal!.CounterAccountNumber);
        Assert.Equal("Bedrijfskosten", proposal.CounterAccountName);
        Assert.True(proposal.IsReliable);
    }

    [Fact]
    public async Task ProposalsAsync_DoetAProposalPerMovement()
    {
        var administration = await WithAdministrationAsync();
        var learned = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(learned.Id, Costs, ct: Ct);
        await ImportAsync(
            administration,
            "2026-04-04;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen\n" +
            "2026-04-05;-31.00;REF-0009;Onbekende Partij;NL99RABO0123456789;Betaling");

        var open = await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct);
        var proposals = await administration.BankStatementService.ProposalsAsync(open, Ct);

        var proposal = Assert.Single(proposals);
        Assert.Equal(open.Single(movement => movement.Reference == "REF-0004").Id, proposal.TransactionId);
        Assert.Equal(Costs, proposal.CounterAccountNumber);
        Assert.True(proposal.IsReliable);
    }

    [Fact]
    public async Task ProposalsAsync_ReadsTheAccountsAndTheLinesOnce()
    {
        var administration = await WithAdministrationAsync();
        var learned = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(learned.Id, Costs, ct: Ct);
        await ImportAsync(
            administration,
            "2026-04-04;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen\n" +
            "2026-04-05;-9.00;REF-0005;Albert Heijn;NL91ABNA0417164300;Boodschappen");
        var open = await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct);
        var accountsFor = administration.Accounts.CountTimesRead;
        var linesFor = administration.Lines.CountTimesRead;

        var proposals = await administration.BankStatementService.ProposalsAsync(open, Ct);

        Assert.Equal(2, open.Count);
        Assert.Equal(2, proposals.Count);
        Assert.All(proposals, proposal => Assert.True(proposal.IsReliable));
        Assert.Equal(1, administration.Accounts.CountTimesRead - accountsFor);
        Assert.Equal(1, administration.Lines.CountTimesRead - linesFor);
    }

    [Fact]
    public async Task ProposalsAsync_WithoutMovements_ReadsNothing()
    {
        var administration = await WithAdministrationAsync();
        var accountsFor = administration.Accounts.CountTimesRead;
        var linesFor = administration.Lines.CountTimesRead;

        var proposals = await administration.BankStatementService.ProposalsAsync([], Ct);

        Assert.Empty(proposals);
        Assert.Equal(accountsFor, administration.Accounts.CountTimesRead);
        Assert.Equal(linesFor, administration.Lines.CountTimesRead);
    }

    [Fact]
    public async Task ProposalsAsync_WithoutList_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => administration.BankStatementService.ProposalsAsync(null!, Ct));
    }

    [Fact]
    public async Task ProposalAsync_UnknownMovement_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => administration.BankStatementService.ProposalAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task ProposalAsync_WithoutId_IsRejected()
    {
        var administration = await WithAdministrationAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => administration.BankStatementService.ProposalAsync(Guid.Empty, Ct));
    }

    [Fact]
    public async Task PostAsync_RemembersTheLineAndConfirmsIt()
    {
        var administration = await WithAdministrationAsync();
        var first = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(first.Id, Costs, ct: Ct);
        var second = await OpenMovementAsync(
            administration,
            "2026-04-05;-8.00;REF-0004;Albert Heijn;NL91ABNA0417164300;Boodschappen");

        await administration.BankStatementService.PostAsync(second.Id, Costs, ct: Ct);

        var line = Assert.Single(await administration.Lines.AllAsync(Ct));
        Assert.Equal("NL91ABNA0417164300", line.Keyword);
        Assert.Equal(Costs, line.CounterAccountNumber);
        Assert.Equal(2, line.Confirmations);
    }

    [Fact]
    public async Task PostAsync_WithoutCounterparty_RemembersNoLine()
    {
        var administration = await WithAdministrationAsync();
        var movement = await OpenMovementAsync(administration, "2026-04-06;-3.50;REF-0005;;;;");

        await administration.BankStatementService.PostAsync(movement.Id, Costs, ct: Ct);

        Assert.Empty(await administration.Lines.AllAsync(Ct));
    }

    [Fact]
    public async Task AllAsync_WithoutStatus_ShowsEveryMovement()
    {
        var administration = await WithAdministrationAsync();
        var first = await OpenMovementAsync(administration);
        await administration.BankStatementService.PostAsync(first.Id, Costs, ct: Ct);
        await OpenMovementAsync(administration, Pinbetaling);

        Assert.Equal(2, (await administration.BankStatementService.AllAsync(ct: Ct)).Count);
    }

    private static Task<BankStatementImportResult> ImportAsync(
        TestAdministration administration,
        string content,
        string fileName = "afschrift.fake") =>
        administration.BankStatementService.ImportAsync(
            new BankStatementImportCommand(fileName, content, Bank),
            Ct);

    private static async Task<TestAdministration> WithAdministrationAsync()
    {
        var administration = new TestAdministration();

        await administration.AccountService.AddAsync(
            new AddAccountCommand(Bank, "Bank", AccountType.Assets, Current: true, CashEquivalent: true),
            Ct);
        await administration.AccountService.AddAsync(
            new AddAccountCommand(Costs, "Bedrijfskosten", AccountType.Costs),
            Ct);
        await administration.AccountService.AddAsync(
            new AddAccountCommand(Omzet, "Omzet", AccountType.Revenue),
            Ct);

        return administration;
    }

    private static async Task<BankTransaction> OpenMovementAsync(
        TestAdministration administration,
        string content = AlbertHeijn)
    {
        await ImportAsync(administration, content);

        return Assert.Single(
            await administration.BankStatementService.AllAsync(BankTransactionStatus.Open, Ct));
    }
}
