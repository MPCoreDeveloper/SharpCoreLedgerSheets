using FinOverzicht.Domain.Bankzaken;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Common;
using FinOverzicht.Infrastructure.Bankzaken;

namespace FinOverzicht.Infrastructure.Tests;

/// <summary>
/// Tests for <see cref="Camt053Importeur"/>: the reader has to survive the differences between bank
/// exports and must refuse anything it cannot book honestly.
/// </summary>
public sealed class Camt053ImporteurTests
{
    private const string Bankrekening = "1100";

    private const string Kop = """
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

    private const string Staart = """
            </Stmt>
          </BkToCstmrStmt>
        </Document>
        """;

    private static readonly Camt053Importeur Importeur = new();

    [Fact]
    public void Formaat_IsCamt053() => Assert.Equal(BankstaatFormaat.Camt053, Importeur.Formaat);

    [Fact]
    public void KanVerwerken_RecognisesCamtDocument() =>
        Assert.True(Importeur.KanVerwerken("afschrift.xml", Afschrift(Mutatie("12.50", "DBIT"))));

    [Fact]
    public void KanVerwerken_RejectsFilesWithoutACamtDocument()
    {
        Assert.False(Importeur.KanVerwerken("afschrift.xml", "<Document><Iets/></Document>"));
        Assert.False(Importeur.KanVerwerken("afschrift.csv", "Datum;Bedrag\r\n2026-04-02;12,50"));
    }

    [Fact]
    public void Lees_ReadsAPayment()
    {
        var transacties = Importeur.Lees(Afschrift(Mutatie("12.50", "DBIT")), Bankrekening);

        var transactie = Assert.Single(transacties);
        Assert.Equal(Bankrekening, transactie.BankRekeningNummer);
        Assert.Equal(new DateOnly(2026, 4, 2), transactie.Datum);
        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
        Assert.Equal(Valuta.Eur, transactie.Mutatie.Valuta);
        Assert.Equal("REF-1", transactie.Referentie);
        Assert.Equal("Albert Heijn", transactie.TegenpartijNaam);
        Assert.Equal("NL02ABNA0123456789", transactie.TegenpartijIban);
        Assert.Equal("Boodschappen week 14", transactie.Omschrijving);
        Assert.True(transactie.IsAfschrijving);
        Assert.False(transactie.IsBijschrijving);
        Assert.Equal(BankTransactieStatus.Open, transactie.Status);
    }

    [Fact]
    public void Lees_ReadsReceiptAsPositiveAmount()
    {
        var transacties = Importeur.Lees(Afschrift(Mutatie("1250.00", "CRDT")), Bankrekening);

        var transactie = Assert.Single(transacties);
        Assert.Equal(1250.00m, transactie.Mutatie.Waarde);
        Assert.True(transactie.IsBijschrijving);
    }

    [Fact]
    public void Lees_IgnoresNamespaceAndVersion()
    {
        var inhoud = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <ns:Document xmlns:ns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
              <ns:BkToCstmrStmt>
                <ns:Stmt>
                  {Mutatie("12.50", "DBIT")}
                </ns:Stmt>
              </ns:BkToCstmrStmt>
            </ns:Document>
            """;

        var transactie = Assert.Single(Importeur.Lees(inhoud, Bankrekening));
        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
    }

    [Fact]
    public void Lees_SkipsPendingMovements()
    {
        var wachtend = Mutatie("99.99", "DBIT", referentie: "REF-WACHT").Replace(
            "<Sts>BOOK</Sts>",
            "<Sts>PDNG</Sts>",
            StringComparison.Ordinal);

        var transacties = Importeur.Lees(Afschrift(wachtend + Mutatie("12.50", "DBIT")), Bankrekening);

        var transactie = Assert.Single(transacties);
        Assert.Equal("REF-1", transactie.Referentie);
    }

    [Fact]
    public void Lees_PrefersTheCreditorForAPayment()
    {
        var mutatie = """
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

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal("Albert Heijn", transactie.TegenpartijNaam);
        Assert.Equal("NL02ABNA0123456789", transactie.TegenpartijIban);
    }

    [Fact]
    public void Lees_PrefersTheDebtorForAReceipt()
    {
        var mutatie = """
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

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal("Klant BV", transactie.TegenpartijNaam);
    }

    [Fact]
    public void Lees_KeepsTheFileOrder()
    {
        var transacties = Importeur.Lees(
            Afschrift(
                Mutatie("1.00", "DBIT", referentie: "REF-A")
                + Mutatie("2.00", "DBIT", referentie: "REF-B")
                + Mutatie("3.00", "DBIT", referentie: "REF-C")),
            Bankrekening);

        Assert.Collection(
            transacties,
            transactie => Assert.Equal("REF-A", transactie.Referentie),
            transactie => Assert.Equal("REF-B", transactie.Referentie),
            transactie => Assert.Equal("REF-C", transactie.Referentie));
    }

    [Fact]
    public void Lees_UsesTheEndToEndIdWhenTheBankSuppliesNoReference()
    {
        var mutatie = """
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

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal("E2E-9", transactie.Referentie);
    }

    [Fact]
    public void Lees_DerivesAStableReferenceWhenTheBankSuppliesNone()
    {
        var zonderReferentie = Mutatie("12.50", "DBIT").Replace(
            "<AcctSvcrRef>REF-1</AcctSvcrRef>",
            string.Empty,
            StringComparison.Ordinal);

        var transacties = Importeur.Lees(Afschrift(zonderReferentie + zonderReferentie), Bankrekening);

        Assert.Equal(2, transacties.Count);
        Assert.StartsWith("SYN-", transacties[0].Referentie, StringComparison.Ordinal);

        // Two movements that the bank cannot tell apart count as one: the same movement may never be
        // imported twice.
        Assert.Equal(transacties[0].Referentie, transacties[1].Referentie);
    }

    [Fact]
    public void Lees_ReadsTheValueDateWhenTheBookingDateIsMissing()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace(
            "<BookgDt><Dt>2026-04-02</Dt></BookgDt>",
            "<ValDt><Dt>2026-04-03</Dt></ValDt>",
            StringComparison.Ordinal);

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal(new DateOnly(2026, 4, 3), transactie.Datum);
    }

    [Fact]
    public void Lees_ReadsTheSignOfTheAmountWhenTheDirectionIsMissing()
    {
        var mutatie = Mutatie("-12.50", "DBIT").Replace(
            "<CdtDbtInd>DBIT</CdtDbtInd>",
            string.Empty,
            StringComparison.Ordinal);

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal(-12.50m, transactie.Mutatie.Waarde);
    }

    [Fact]
    public void Lees_ReadsTheCurrencyOfTheMovement()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace("Ccy=\"EUR\"", "Ccy=\"CHF\"", StringComparison.Ordinal);

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Equal(Valuta.Chf, transactie.Mutatie.Valuta);
    }

    [Fact]
    public void Lees_UsesTheStandardCurrencyWhenTheStatementOmitsIt()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace(" Ccy=\"EUR\"", string.Empty, StringComparison.Ordinal);

        var transactie = Assert.Single(Importeur.Lees(Afschrift(mutatie), Bankrekening, Valuta.Usd));

        Assert.Equal(Valuta.Usd, transactie.Mutatie.Valuta);
    }

    [Fact]
    public void Lees_ThrowsForAnUnsupportedCurrency()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace("Ccy=\"EUR\"", "Ccy=\"SEK\"", StringComparison.Ordinal);

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Contains("SEK", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsWhenNoDateIsReadable()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace(
            "<BookgDt><Dt>2026-04-02</Dt></BookgDt>",
            string.Empty,
            StringComparison.Ordinal);

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Contains("boekdatum", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsForAnUnreadableAmount()
    {
        var mutatie = Mutatie("12.50", "DBIT").Replace(">12.50<", ">twaalf<", StringComparison.Ordinal);

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(Afschrift(mutatie), Bankrekening));

        Assert.Contains("bedrag", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsWhenNothingIsBooked()
    {
        var wachtend = Mutatie("12.50", "DBIT").Replace("<Sts>BOOK</Sts>", "<Sts>PDNG</Sts>", StringComparison.Ordinal);

        var fout = Assert.Throws<DomeinException>(() => Importeur.Lees(Afschrift(wachtend), Bankrekening));

        Assert.Contains("geen geboekte bankmutaties", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsForXmlThatIsNotACamtStatement()
    {
        var fout = Assert.Throws<DomeinException>(
            () => Importeur.Lees("<Document><Iets/></Document>", Bankrekening));

        Assert.Contains("BkToCstmrStmt", fout.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Lees_ThrowsForUnreadableXml()
    {
        var fout = Assert.Throws<DomeinException>(
            () => Importeur.Lees("<Document><BkToCstmrStmt>", Bankrekening));

        Assert.Contains("XML", fout.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Lees_ThrowsForBlankContent(string inhoud) =>
        Assert.Throws<ArgumentException>(() => Importeur.Lees(inhoud, Bankrekening));

    [Fact]
    public void Lees_ThrowsForBlankBankAccount() =>
        Assert.Throws<ArgumentException>(() => Importeur.Lees(Afschrift(Mutatie("12.50", "DBIT")), "  "));

    private static string Afschrift(string mutaties) => $"{Kop}{mutaties}{Staart}";

    private static string Mutatie(
        string bedrag,
        string richting,
        string referentie = "REF-1",
        string tegenpartij = "Albert Heijn",
        string iban = "NL02ABNA0123456789",
        string omschrijving = "Boodschappen week 14")
    {
        // A payment has the creditor as counterparty, a receipt the debtor.
        var partij = richting == "DBIT" ? "Cdtr" : "Dbtr";

        return $"""
              <Ntry>
                <Amt Ccy="EUR">{bedrag}</Amt>
                <CdtDbtInd>{richting}</CdtDbtInd>
                <Sts>BOOK</Sts>
                <BookgDt><Dt>2026-04-02</Dt></BookgDt>
                <AcctSvcrRef>{referentie}</AcctSvcrRef>
                <NtryDtls>
                  <TxDtls>
                    <RltdPties>
                      <{partij}><Nm>{tegenpartij}</Nm></{partij}>
                      <{partij}Acct><Id><IBAN>{iban}</IBAN></Id></{partij}Acct>
                    </RltdPties>
                    <RmtInf><Ustrd>{omschrijving}</Ustrd></RmtInf>
                  </TxDtls>
                </NtryDtls>
              </Ntry>
            """;
    }
}
