namespace SharpCoreLedgerSheets.Domain.Common;

/// <summary>
/// The stable codes that identify the user-facing messages the engine can raise.
/// </summary>
/// <remarks>
/// A code is the seam between the layer that raises a message and the layer that shows it: the exception
/// carries the code plus the values to insert, and the host resolves <c>Msg_&lt;code&gt;</c> from its
/// resources. That way one rule speaks Dutch, English or German without the domain knowing about cultures.
/// The message the exception carries stays Dutch - the primary market - and is the fallback for a host
/// that has no translation for the code.
/// </remarks>
public static class MessageCode
{
    // Ledger accounts.
    public const string AccountIdRequired = nameof(AccountIdRequired);
    public const string AccountNumberRequired = nameof(AccountNumberRequired);
    public const string AccountNameRequired = nameof(AccountNameRequired);
    public const string AccountCashOnlyForAssets = nameof(AccountCashOnlyForAssets);
    public const string AccountInventoryOnlyForAssets = nameof(AccountInventoryOnlyForAssets);
    public const string AccountNumberDuplicate = nameof(AccountNumberDuplicate);
    public const string AccountNumberExists = nameof(AccountNumberExists);
    public const string AccountAlreadyRegistered = nameof(AccountAlreadyRegistered);
    public const string AccountNotFound = nameof(AccountNotFound);
    public const string AccountInactive = nameof(AccountInactive);

    // Money and periods.
    public const string AmountCurrencyRequired = nameof(AmountCurrencyRequired);
    public const string AmountCurrencyMismatch = nameof(AmountCurrencyMismatch);
    public const string PeriodEndsBeforeStart = nameof(PeriodEndsBeforeStart);

    // Journal entries and their lines.
    public const string EntryIdRequired = nameof(EntryIdRequired);
    public const string EntryDescriptionRequired = nameof(EntryDescriptionRequired);
    public const string EntryNeedsTwoLines = nameof(EntryNeedsTwoLines);
    public const string EntryNotBalanced = nameof(EntryNotBalanced);
    public const string EntryAlreadyPosted = nameof(EntryAlreadyPosted);
    public const string EntryUnknownAccount = nameof(EntryUnknownAccount);
    public const string LineAccountRequired = nameof(LineAccountRequired);
    public const string LineNegativeAmount = nameof(LineNegativeAmount);
    public const string LineDebitAndCredit = nameof(LineDebitAndCredit);
    public const string LineAmountRequired = nameof(LineAmountRequired);

    // Bank movements, matching rules and proposals.
    public const string TransactionIdRequired = nameof(TransactionIdRequired);
    public const string TransactionBankAccountRequired = nameof(TransactionBankAccountRequired);
    public const string TransactionReferenceRequired = nameof(TransactionReferenceRequired);
    public const string TransactionAmountRequired = nameof(TransactionAmountRequired);
    public const string TransactionEntryRequired = nameof(TransactionEntryRequired);
    public const string MovementAlreadyBooked = nameof(MovementAlreadyBooked);
    public const string MovementNotFound = nameof(MovementNotFound);
    public const string BookedMovementCannotBeIgnored = nameof(BookedMovementCannotBeIgnored);
    public const string CounterAccountIsBankAccount = nameof(CounterAccountIsBankAccount);
    public const string RuleAccountRequired = nameof(RuleAccountRequired);
    public const string RuleKeywordTooShort = nameof(RuleKeywordTooShort);
    public const string RuleConfirmationsRequired = nameof(RuleConfirmationsRequired);
    public const string ProposalTransactionRequired = nameof(ProposalTransactionRequired);
    public const string ProposalAccountRequired = nameof(ProposalAccountRequired);
    public const string ProposalAccountNameRequired = nameof(ProposalAccountNameRequired);
    public const string ProposalCertaintyRange = nameof(ProposalCertaintyRange);
    public const string ProposalReasonRequired = nameof(ProposalReasonRequired);

    // Importing statements.
    public const string StatementFileRequired = nameof(StatementFileRequired);
    public const string NoImporterForFormat = nameof(NoImporterForFormat);
    public const string UnsupportedStatementFile = nameof(UnsupportedStatementFile);
    public const string StatementHasNoBookedMovements = nameof(StatementHasNoBookedMovements);
    public const string StatementNotReadableXml = nameof(StatementNotReadableXml);
    public const string StatementNotACamtFile = nameof(StatementNotACamtFile);
    public const string StatementUnreadableAmount = nameof(StatementUnreadableAmount);
    public const string StatementUnsupportedCurrency = nameof(StatementUnsupportedCurrency);
    public const string StatementUnreadableBookingDate = nameof(StatementUnreadableBookingDate);
    public const string CsvHasNoHeader = nameof(CsvHasNoHeader);
    public const string CsvMissingColumn = nameof(CsvMissingColumn);
    public const string CsvHasNoMovements = nameof(CsvHasNoMovements);
    public const string CsvUnreadableAmount = nameof(CsvUnreadableAmount);
}
