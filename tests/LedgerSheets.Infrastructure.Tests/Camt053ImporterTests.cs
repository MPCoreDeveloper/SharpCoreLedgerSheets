using LedgerSheets.Domain.Banking;
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Common;
using LedgerSheets.Infrastructure.Banking;

namespace LedgerSheets.Infrastructure.Tests;

/// <summary>
/// Tests for <see cref="Camt053Importer"/>: the reader has to survive the differences between bank
/// exports and must refuse anything it cannot book honestly.
/// </summary>
public sealed class Camt053ImporterTests
{
    private const string BankAccount = "1100";

    private const string Header = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.02">
          <BkToCstmrStmt>
            <Stmt>
              <Id>AFSCHRIFT-2026-04</Id>
              <Acct>
                <Id><IBAN>NL91ABNA0417164300</IBAN></Id>
                <Ccy>EUR</Ccy>
              </Acct>
        """;

    private const string Tail = """
            </Stmt>
          </BkToCstmrStmt>
        </Document>
        """;

    private static readonly Camt053Importer Importer = new();

    [Fact]
    public void Format_IsCamt053() => Assert.Equal(BankStatementFormat.Camt053, Importer.Format);

    [Fact]
    public void CanProcess_RecognisesCamtDocument() =>
        Assert.True(Importer.CanProcess("afschrift.xml", Statement(Movement("12.50", "DBIT"))));

    [Fact]
    public void CanProcess_RejectsFilesWithoutACamtDocument()
    {
        Assert.False(Importer.CanProcess("afschrift.xml", "<Document><Iets/></Document>"));
        Assert.False(Importer.CanProcess("afschrift.csv", "Datum;Bedrag\r\n2026-04-02;12,50"));
    }

    [Fact]
    public void Read_ReadsAPayment()
    {
        var transactions = Importer.Read(Statement(Movement("12.50", "DBIT")), BankAccount);

        var transaction = Assert.Single(transactions);
        Assert.Equal(BankAccount, transaction.BankAccountNumber);
        Assert.Equal(new DateOnly(2026, 4, 2), transaction.Date);
        Assert.Equal(-12.50m, transaction.Movement.Value);
        Assert.Equal(Currency.Eur, transaction.Movement.Currency);
        Assert.Equal("REF-1", transaction.Reference);
        Assert.Equal("Albert Heijn", transaction.CounterpartyName);
        Assert.Equal("NL02ABNA0123456789", transaction.CounterpartyIban);
        Assert.Equal("Boodschappen week 14", transaction.Description);
        Assert.True(transaction.IsDepreciation);
        Assert.False(transaction.IsCredit);
        Assert.Equal(BankTransactionStatus.Open, transaction.Status);
    }

    [Fact]
    public void Read_ReadsReceiptAsPositiveAmount()
    {
        var transactions = Importer.Read(Statement(Movement("1250.00", "CRDT")), BankAccount);

        var transaction = Assert.Single(transactions);
        Assert.Equal(1250.00m, transaction.Movement.Value);
        Assert.True(transaction.IsCredit);
    }

    [Fact]
    public void Read_IgnoresNamespaceAndVersion()
    {
        var content = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <ns:Document xmlns:ns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
              <ns:BkToCstmrStmt>
                <ns:Stmt>
                  {Movement("12.50", "DBIT")}
                </ns:Stmt>
              </ns:BkToCstmrStmt>
            </ns:Document>
            """;

        var transaction = Assert.Single(Importer.Read(content, BankAccount));
        Assert.Equal(-12.50m, transaction.Movement.Value);
    }

    [Fact]
    public void Read_SkipsPendingMovements()
    {
        var pending = Movement("99.99", "DBIT", reference: "REF-WACHT").Replace(
            "<Sts>BOOK</Sts>",
            "<Sts>PDNG</Sts>",
            StringComparison.Ordinal);

        var transactions = Importer.Read(Statement(pending + Movement("12.50", "DBIT")), BankAccount);

        var transaction = Assert.Single(transactions);
        Assert.Equal("REF-1", transaction.Reference);
    }

    [Fact]
    public void Read_PrefersTheCreditorForAPayment()
    {
        var movement = """
              <Ntry>
                <Amt Ccy="EUR">12.50</Amt>
                <CdtDbtInd>DBIT</CdtDbtInd>
                <Sts>BOOK</Sts>
                <BookgDt><Dt>2026-04-02</Dt></BookgDt>
                <NtryDtls>
                  <TxDtls>
                    <RltdPties>
                      <Dbtr><Nm>Onze Onderneming</Nm></Dbtr>
                      <DbtrAcct><Id><IBAN>NL91ABNA0417164300</IBAN></Id></DbtrAcct>
                      <Cdtr><Nm>Albert Heijn</Nm></Cdtr>
                      <CdtrAcct><Id><IBAN>NL02ABNA0123456789</IBAN></Id></CdtrAcct>
                    </RltdPties>
                  </TxDtls>
                </NtryDtls>
              </Ntry>
            """;

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal("Albert Heijn", transaction.CounterpartyName);
        Assert.Equal("NL02ABNA0123456789", transaction.CounterpartyIban);
    }

    [Fact]
    public void Read_PrefersTheDebtorForAReceipt()
    {
        var movement = """
              <Ntry>
                <Amt Ccy="EUR">1250.00</Amt>
                <CdtDbtInd>CRDT</CdtDbtInd>
                <Sts>BOOK</Sts>
                <BookgDt><Dt>2026-04-02</Dt></BookgDt>
                <NtryDtls>
                  <TxDtls>
                    <RltdPties>
                      <Dbtr><Nm>Klant BV</Nm></Dbtr>
                      <Cdtr><Nm>Onze Onderneming</Nm></Cdtr>
                    </RltdPties>
                  </TxDtls>
                </NtryDtls>
              </Ntry>
            """;

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal("Klant BV", transaction.CounterpartyName);
    }

    [Fact]
    public void Read_KeepsTheFileOrder()
    {
        var transactions = Importer.Read(
            Statement(
                Movement("1.00", "DBIT", reference: "REF-A")
                + Movement("2.00", "DBIT", reference: "REF-B")
                + Movement("3.00", "DBIT", reference: "REF-C")),
            BankAccount);

        Assert.Collection(
            transactions,
            transaction => Assert.Equal("REF-A", transaction.Reference),
            transaction => Assert.Equal("REF-B", transaction.Reference),
            transaction => Assert.Equal("REF-C", transaction.Reference));
    }

    [Fact]
    public void Read_UsesTheEndToEndIdWhenTheBankSuppliesNoReference()
    {
        var movement = """
              <Ntry>
                <Amt Ccy="EUR">12.50</Amt>
                <CdtDbtInd>DBIT</CdtDbtInd>
                <Sts>BOOK</Sts>
                <BookgDt><Dt>2026-04-02</Dt></BookgDt>
                <AcctSvcrRef>NOTPROVIDED</AcctSvcrRef>
                <NtryDtls>
                  <TxDtls><Refs><EndToEndId>E2E-9</EndToEndId></Refs></TxDtls>
                </NtryDtls>
              </Ntry>
            """;

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal("E2E-9", transaction.Reference);
    }

    [Fact]
    public void Read_DerivesAStableReferenceWhenTheBankSuppliesNone()
    {
        var withoutReference = Movement("12.50", "DBIT").Replace(
            "<AcctSvcrRef>REF-1</AcctSvcrRef>",
            string.Empty,
            StringComparison.Ordinal);

        var transactions = Importer.Read(Statement(withoutReference + withoutReference), BankAccount);

        Assert.Equal(2, transactions.Count);
        Assert.StartsWith("SYN-", transactions[0].Reference, StringComparison.Ordinal);

        // Two movements that the bank cannot tell apart count as one: the same movement may never be
        // imported twice.
        Assert.Equal(transactions[0].Reference, transactions[1].Reference);
    }

    [Fact]
    public void Read_ReadsTheValueDateWhenTheBookingDateIsMissing()
    {
        var movement = Movement("12.50", "DBIT").Replace(
            "<BookgDt><Dt>2026-04-02</Dt></BookgDt>",
            "<ValDt><Dt>2026-04-03</Dt></ValDt>",
            StringComparison.Ordinal);

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal(new DateOnly(2026, 4, 3), transaction.Date);
    }

    [Fact]
    public void Read_ReadsTheSignOfTheAmountWhenTheDirectionIsMissing()
    {
        var movement = Movement("-12.50", "DBIT").Replace(
            "<CdtDbtInd>DBIT</CdtDbtInd>",
            string.Empty,
            StringComparison.Ordinal);

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal(-12.50m, transaction.Movement.Value);
    }

    [Fact]
    public void Read_ReadsTheCurrencyOfTheMovement()
    {
        var movement = Movement("12.50", "DBIT").Replace("Ccy=\"EUR\"", "Ccy=\"CHF\"", StringComparison.Ordinal);

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount));

        Assert.Equal(Currency.Chf, transaction.Movement.Currency);
    }

    [Fact]
    public void Read_UsesTheStandardCurrencyWhenTheStatementOmitsIt()
    {
        var movement = Movement("12.50", "DBIT").Replace(" Ccy=\"EUR\"", string.Empty, StringComparison.Ordinal);

        var transaction = Assert.Single(Importer.Read(Statement(movement), BankAccount, Currency.Usd));

        Assert.Equal(Currency.Usd, transaction.Movement.Currency);
    }

    [Fact]
    public void Read_ThrowsForAnUnsupportedCurrency()
    {
        var movement = Movement("12.50", "DBIT").Replace("Ccy=\"EUR\"", "Ccy=\"SEK\"", StringComparison.Ordinal);

        var error = Assert.Throws<DomainException>(() => Importer.Read(Statement(movement), BankAccount));

        Assert.Contains("SEK", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsWhenNoDateIsReadable()
    {
        var movement = Movement("12.50", "DBIT").Replace(
            "<BookgDt><Dt>2026-04-02</Dt></BookgDt>",
            string.Empty,
            StringComparison.Ordinal);

        var error = Assert.Throws<DomainException>(() => Importer.Read(Statement(movement), BankAccount));

        Assert.Contains("boekdatum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsForAnUnreadableAmount()
    {
        var movement = Movement("12.50", "DBIT").Replace(">12.50<", ">twaalf<", StringComparison.Ordinal);

        var error = Assert.Throws<DomainException>(() => Importer.Read(Statement(movement), BankAccount));

        Assert.Contains("bedrag", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsWhenNothingIsBooked()
    {
        var pending = Movement("12.50", "DBIT").Replace("<Sts>BOOK</Sts>", "<Sts>PDNG</Sts>", StringComparison.Ordinal);

        var error = Assert.Throws<DomainException>(() => Importer.Read(Statement(pending), BankAccount));

        Assert.Contains("geen geboekte bankmutaties", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsForXmlThatIsNotACamtStatement()
    {
        var error = Assert.Throws<DomainException>(
            () => Importer.Read("<Document><Iets/></Document>", BankAccount));

        Assert.Contains("BkToCstmrStmt", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsForUnreadableXml()
    {
        var error = Assert.Throws<DomainException>(
            () => Importer.Read("<Document><BkToCstmrStmt>", BankAccount));

        Assert.Contains("XML", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Read_ThrowsForBlankContent(string content) =>
        Assert.Throws<ArgumentException>(() => Importer.Read(content, BankAccount));

    [Fact]
    public void Read_ThrowsForBlankBankAccount() =>
        Assert.Throws<ArgumentException>(() => Importer.Read(Statement(Movement("12.50", "DBIT")), "  "));

    private static string Statement(string movements) => $"{Header}{movements}{Tail}";

    private static string Movement(
        string amount,
        string direction,
        string reference = "REF-1",
        string counterparty = "Albert Heijn",
        string iban = "NL02ABNA0123456789",
        string description = "Boodschappen week 14")
    {
        // A payment has the creditor as counterparty, a receipt the debtor.
        var party = direction == "DBIT" ? "Cdtr" : "Dbtr";

        return $"""
              <Ntry>
                <Amt Ccy="EUR">{amount}</Amt>
                <CdtDbtInd>{direction}</CdtDbtInd>
                <Sts>BOOK</Sts>
                <BookgDt><Dt>2026-04-02</Dt></BookgDt>
                <AcctSvcrRef>{reference}</AcctSvcrRef>
                <NtryDtls>
                  <TxDtls>
                    <RltdPties>
                      <{party}><Nm>{counterparty}</Nm></{party}>
                      <{party}Acct><Id><IBAN>{iban}</IBAN></Id></{party}Acct>
                    </RltdPties>
                    <RmtInf><Ustrd>{description}</Ustrd></RmtInf>
                  </TxDtls>
                </NtryDtls>
              </Ntry>
            """;
    }
}
