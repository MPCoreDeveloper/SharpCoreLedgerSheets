using SharpCoreLedgerSheets.Domain.Banking;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Common;
using SharpCoreLedgerSheets.Infrastructure.Banking;

namespace SharpCoreLedgerSheets.Infrastructure.Tests;

/// <summary>
/// Tests for <see cref="CsvImporter"/>: the reader recognises columns by their header names, so it
/// has to cope with the many header spellings, delimiters and number notations banks use.
/// </summary>
public sealed class CsvImporterTests
{
    private const string BankAccount = "1100";

    private static readonly CsvImporter Importer = new();

    [Fact]
    public void Format_IsCsv() => Assert.Equal(BankStatementFormat.Csv, Importer.Format);

    [Theory]
    [InlineData("afschrift.csv")]
    [InlineData("AFSCHRIFT.CSV")]
    [InlineData("afschrift.txt")]
    public void CanProcess_RecognisesDelimitedFiles(string fileName) =>
        Assert.True(Importer.CanProcess(fileName, "Datum;Bedrag\r\n02-04-2026;12,50"));

    [Fact]
    public void CanProcess_RejectsFilesWithoutContent()
    {
        Assert.False(Importer.CanProcess("afschrift.csv", string.Empty));
        Assert.False(Importer.CanProcess("afschrift.xlsx", "Datum;Bedrag\r\n02-04-2026;12,50"));
        Assert.False(Importer.CanProcess("afschrift.xml", "<Document><BkToCstmrStmt/></Document>"));
    }

    [Fact]
    public void Read_ReadsADutchSemicolonExport()
    {
        var content = Csv(
            "Datum;Bedrag;Tegenpartij;IBAN;Omschrijving;Referentie",
            "02-04-2026;-12,50;Albert Heijn;NL02ABNA0123456789;Boodschappen week 14;REF-1",
            "03-04-2026;1.234,56;Klant BV;NL91ABNA0417164300;Factuur 2026-014;REF-2");

        var transactions = Importer.Read(content, BankAccount);

        Assert.Equal(2, transactions.Count);

        var payment = transactions[0];
        Assert.Equal(new DateOnly(2026, 4, 2), payment.Date);
        Assert.Equal(-12.50m, payment.Movement.Value);
        Assert.Equal(Currency.Eur, payment.Movement.Currency);
        Assert.Equal("Albert Heijn", payment.CounterpartyName);
        Assert.Equal("NL02ABNA0123456789", payment.CounterpartyIban);
        Assert.Equal("Boodschappen week 14", payment.Description);
        Assert.Equal("REF-1", payment.Reference);
        Assert.Equal(BankAccount, payment.BankAccountNumber);
        Assert.Equal(BankTransactionStatus.Open, payment.Status);

        var receipt = transactions[1];
        Assert.Equal(new DateOnly(2026, 4, 3), receipt.Date);
        Assert.Equal(1234.56m, receipt.Movement.Value);
        Assert.True(receipt.IsCredit);
        Assert.Equal("REF-2", receipt.Reference);
    }

    [Fact]
    public void Read_ReadsAnEnglishCommaExportWithQuotedFields()
    {
        var content = Csv(
            "Date,Amount,Counterparty,Description,Reference",
            "2026-04-02,\"1,234.56\",\"Klant, BV\",\"Zeg \"\"hallo\"\"\",REF-2");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(new DateOnly(2026, 4, 2), transaction.Date);
        Assert.Equal(1234.56m, transaction.Movement.Value);
        Assert.Equal("Klant, BV", transaction.CounterpartyName);
        Assert.Equal("Zeg \"hallo\"", transaction.Description);
        Assert.Equal("REF-2", transaction.Reference);
    }

    [Fact]
    public void Read_ReadsATabSeparatedExport()
    {
        var content = Csv(
            "Datum\tBedrag\tTegenpartij",
            "02-04-2026\t-12,50\tAlbert Heijn");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(-12.50m, transaction.Movement.Value);
        Assert.Equal("Albert Heijn", transaction.CounterpartyName);
    }

    [Fact]
    public void Read_RecognisesHeaderNamesInAnyForm()
    {
        var content = Csv(
            "BEDRAG (EUR);Boekdatum;Naam tegenpartij;Mededelingen",
            "-12,50;02-04-2026;Albert Heijn;Boodschappen");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(new DateOnly(2026, 4, 2), transaction.Date);
        Assert.Equal(-12.50m, transaction.Movement.Value);
        Assert.Equal("Albert Heijn", transaction.CounterpartyName);
        Assert.Equal("Boodschappen", transaction.Description);
    }

    [Fact]
    public void Read_UsesTheDirectionColumnForTheSign()
    {
        var content = Csv(
            "Datum;Bedrag;Af/Bij;Tegenpartij",
            "02-04-2026;12,50;Af;Albert Heijn",
            "03-04-2026;1.234,56;Bij;Klant BV");

        var transactions = Importer.Read(content, BankAccount);

        Assert.Collection(
            transactions,
            payment => Assert.Equal(-12.50m, payment.Movement.Value),
            receipt => Assert.Equal(1234.56m, receipt.Movement.Value));
    }

    [Fact]
    public void Read_ReadsThousandsSeparatorWithoutDecimals()
    {
        var content = Csv(
            "Datum;Bedrag",
            "02-04-2026;1.234");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(1234m, transaction.Movement.Value);
    }

    [Theory]
    [InlineData("2026-04-02")]
    [InlineData("02-04-2026")]
    [InlineData("2-4-2026")]
    [InlineData("02/04/2026")]
    [InlineData("20260402")]
    public void Read_ReadsTheDateNotationsBanksUse(string date)
    {
        var content = Csv("Datum;Bedrag", $"{date};-12,50");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(new DateOnly(2026, 4, 2), transaction.Date);
    }

    [Fact]
    public void Read_SkipsRowsWithoutADate()
    {
        var content = Csv(
            "Datum;Bedrag;Tegenpartij",
            "02-04-2026;-12,50;Albert Heijn",
            ";1.234,56;Totaal");

        var transaction = Assert.Single(Importer.Read(content, BankAccount));

        Assert.Equal(-12.50m, transaction.Movement.Value);
    }

    [Fact]
    public void Read_DerivesAStableReferenceWhenTheColumnIsMissing()
    {
        var content = Csv(
            "Datum;Bedrag;Tegenpartij;Omschrijving",
            "02-04-2026;-12,50;Albert Heijn;Boodschappen week 14",
            "02-04-2026;-12,50;Albert Heijn;Boodschappen week 14");

        var transactions = Importer.Read(content, BankAccount);

        Assert.Equal(2, transactions.Count);
        Assert.StartsWith("SYN-", transactions[0].Reference, StringComparison.Ordinal);
        Assert.Equal(transactions[0].Reference, transactions[1].Reference);
    }

    [Fact]
    public void Read_UsesTheStandardCurrencyWhenTheFileOmitsIt()
    {
        var content = Csv("Datum;Bedrag", "02-04-2026;-12,50");

        var transaction = Assert.Single(Importer.Read(content, BankAccount, Currency.Usd));

        Assert.Equal(Currency.Usd, transaction.Movement.Currency);
    }

    [Fact]
    public void Read_ThrowsWhenTheAmountColumnIsMissing()
    {
        var content = Csv("Datum;Omschrijving", "02-04-2026;Boodschappen");

        var error = Assert.Throws<DomainException>(() => Importer.Read(content, BankAccount));

        Assert.Contains("Amount", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsWhenTheDateColumnIsMissing()
    {
        var content = Csv("Bedrag;Omschrijving", "-12,50;Boodschappen");

        var error = Assert.Throws<DomainException>(() => Importer.Read(content, BankAccount));

        Assert.Contains("Date", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsForAnUnreadableAmountAndNamesTheLine()
    {
        var content = Csv(
            string.Empty,
            "Datum;Bedrag",
            "02-04-2026;12,50",
            string.Empty,
            "03-04-2026;twaalf");

        var error = Assert.Throws<DomainException>(() => Importer.Read(content, BankAccount));

        Assert.Contains("Regel 5", error.Message, StringComparison.Ordinal);
        Assert.Contains("twaalf", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsWhenNoRowIsAMovement()
    {
        var content = Csv("Datum;Bedrag", ";1.234,56");

        var error = Assert.Throws<DomainException>(() => Importer.Read(content, BankAccount));

        Assert.Contains("geen bankmutaties", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_ThrowsForBlankArguments()
    {
        Assert.Throws<ArgumentException>(() => Importer.Read(string.Empty, BankAccount));
        Assert.Throws<ArgumentException>(() => Importer.Read("Datum;Bedrag", "  "));
    }

    private static string Csv(params string[] lines) => string.Join('\n', lines);
}
