using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Application.Services;
using SharpCoreLedgerSheets.Application.Tests.Fakes;
using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Services;

namespace SharpCoreLedgerSheets.Application.Tests;

/// <summary>
/// Test harness that wires the application services on top of in-memory repositories.
/// </summary>
internal sealed class TestAdministration
{
    private static readonly DateOnly ScenarioDate = new(2026, 6, 15);

    public TestAdministration()
    {
        AccountService = new AccountService(Accounts);
        JournalEntryService = new JournalEntryService(Accounts, Entries);
        Loader = new LedgerLoader(Accounts, Entries);
        Reporting = new ReportingService(
            Loader,
            new BalanceSheetBuilder(),
            new ProfitAndLossBuilder(),
            new CashFlowBuilder(),
            new RatioCalculator());
        BankStatementService = new BankStatementService(
            BankStatement,
            Lines,
            Accounts,
            JournalEntryService,
            new MatchingAssistant(),
            [FakeImporter, CamtImporter]);
    }

    public InMemoryAccountRepository Accounts { get; } = new();

    public InMemoryJournalEntryRepository Entries { get; } = new();

    public InMemoryBankStatementRepository BankStatement { get; } = new();

    public InMemoryCounterAccountRuleRepository Lines { get; } = new();

    /// <summary>Statement reader for the <c>.fake</c> test format, detected by its extension.</summary>
    public FakeBankStatementImporter FakeImporter { get; } = new(".fake", BankStatementFormat.Csv);

    /// <summary>Statement reader that can only be selected by forcing <see cref="BankStatementFormat.Camt053"/>.</summary>
    public FakeBankStatementImporter CamtImporter { get; } = new(".camt", BankStatementFormat.Camt053);

    public AccountService AccountService { get; }

    public JournalEntryService JournalEntryService { get; }

    public LedgerLoader Loader { get; }

    public ReportingService Reporting { get; }

    public BankStatementService BankStatementService { get; }

    /// <summary>Financial year used by the seed data.</summary>
    public static Period Year2026 => Period.ForYear(2026);

    /// <summary>Seeds the same scenario as the domain tests.</summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await AccountService.AddAsync(new AddAccountCommand("1000", "Kas", AccountType.Assets, Current: true, CashEquivalent: true), ct);
        await AccountService.AddAsync(new AddAccountCommand("1100", "Bank", AccountType.Assets, Current: true, CashEquivalent: true), ct);
        await AccountService.AddAsync(new AddAccountCommand("1300", "Inventory", AccountType.Assets, Current: true, Inventory: true), ct);
        await AccountService.AddAsync(new AddAccountCommand("1500", "Machines", AccountType.Assets), ct);
        await AccountService.AddAsync(new AddAccountCommand("2000", "Creditors", AccountType.Liabilities, Current: true), ct);
        await AccountService.AddAsync(new AddAccountCommand("2100", "BankLoan", AccountType.Liabilities), ct);
        await AccountService.AddAsync(new AddAccountCommand("3000", "Eigen vermogen", AccountType.Equity), ct);
        await AccountService.AddAsync(new AddAccountCommand("4000", "Bedrijfskosten", AccountType.Costs), ct);
        await AccountService.AddAsync(new AddAccountCommand("8000", "Omzet", AccountType.Revenue), ct);

        await PostAsync("Startkapitaal", ("1000", 10_000m, 0m), ("3000", 0m, 10_000m), ct);
        await PostAsync("Banklening ontvangen", ("1100", 5_000m, 0m), ("2100", 0m, 5_000m), ct);
        await PostAsync("Inkoop voorraad op rekening", ("1300", 2_000m, 0m), ("2000", 0m, 2_000m), ct);
        await PostAsync("Contante verkoop", ("1000", 4_000m, 0m), ("8000", 0m, 4_000m), ct);
        await PostAsync("Inkoopwaarde verkopen", ("4000", 1_200m, 0m), ("1300", 0m, 1_200m), ct);
        await PostAsync("Machine aangeschaft", ("1500", 3_000m, 0m), ("1100", 0m, 3_000m), ct);
        await PostAsync("Bedrijfskosten betaald", ("4000", 500m, 0m), ("1100", 0m, 500m), ct);
        await PostAsync("Aflossing banklening", ("2100", 1_000m, 0m), ("1100", 0m, 1_000m), ct);
    }

    private Task PostAsync(
        string description,
        (string Number, decimal Debit, decimal Credit) debit,
        (string Number, decimal Debit, decimal Credit) credit,
        CancellationToken ct)
    {
        var lines = new List<JournalLineCommand>
        {
            new(debit.Number, debit.Debit, debit.Credit),
            new(credit.Number, credit.Debit, credit.Credit),
        };

        return JournalEntryService.PostAsync(
            new PostJournalEntryCommand(ScenarioDate, description, lines), ct);
    }
}
