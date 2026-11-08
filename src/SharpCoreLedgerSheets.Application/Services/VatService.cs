using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Application.Models;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreLedgerSheets.Domain.Common;

namespace SharpCoreLedgerSheets.Application.Services;

/// <summary>
/// Default <see cref="IVatService"/>.
/// </summary>
public sealed class VatService(
    ISettingsRepository settingsRepository,
    IJournalEntryService journalEntryService) : IVatService
{
    /// <inheritdoc />
    public Task<JournalEntry> PostSaleAsync(VatInvoiceCommand command, CancellationToken ct = default) =>
        PostAsync(command, isSale: true, ct);

    /// <inheritdoc />
    public Task<JournalEntry> PostPurchaseAsync(VatInvoiceCommand command, CancellationToken ct = default) =>
        PostAsync(command, isSale: false, ct);

    /// <summary>Rounds the VAT of a net amount to cents, half away from zero.</summary>
    public static decimal VatOn(decimal netAmount, decimal percentage) =>
        Math.Round(netAmount * percentage / 100m, 2, MidpointRounding.AwayFromZero);

    private async Task<JournalEntry> PostAsync(VatInvoiceCommand command, bool isSale, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.NetAmount <= 0m)
        {
            throw new DomainException(
                MessageCode.AmountMustBePositive,
                "Het nettobedrag moet groter zijn dan nul.");
        }

        var settings = await settingsRepository.GetAsync(ct).ConfigureAwait(false);

        if (settings.Vat.IsEmpty)
        {
            throw new DomainException(
                MessageCode.VatSchemeEmpty,
                "Er zijn geen btw-tarieven ingesteld voor deze administratie.");
        }

        var rate = settings.Vat.Require(command.VatRateCode);
        var vat = VatOn(command.NetAmount, rate.Percentage);
        var gross = command.NetAmount + vat;

        var lines = new List<JournalLineCommand>(3);

        if (isSale)
        {
            lines.Add(new JournalLineCommand(command.CounterAccountNumber, gross, 0m, command.Description));
            lines.Add(new JournalLineCommand(command.NetAccountNumber, 0m, command.NetAmount, command.Description));

            if (vat > 0m)
            {
                lines.Add(new JournalLineCommand(OutputAccount(rate), 0m, vat, VatDescription(rate)));
            }
        }
        else
        {
            lines.Add(new JournalLineCommand(command.NetAccountNumber, command.NetAmount, 0m, command.Description));

            if (vat > 0m)
            {
                lines.Add(new JournalLineCommand(InputAccount(rate), vat, 0m, VatDescription(rate)));
            }

            lines.Add(new JournalLineCommand(command.CounterAccountNumber, 0m, gross, command.Description));
        }

        return await journalEntryService.PostAsync(
            new PostJournalEntryCommand(command.Date, command.Description, lines, command.Reference),
            ct).ConfigureAwait(false);
    }

    private static string OutputAccount(VatRate rate) =>
        rate.OutputAccountNumber
        ?? throw new DomainException(
            MessageCode.VatAccountsMissing,
            "Het btw-tarief '{0}' heeft geen grootboekrekening voor de af te dragen btw.",
            rate.Code);

    private static string InputAccount(VatRate rate) =>
        rate.InputAccountNumber
        ?? throw new DomainException(
            MessageCode.VatAccountsMissing,
            "Het btw-tarief '{0}' heeft geen grootboekrekening voor de voorbelasting.",
            rate.Code);

    private static string VatDescription(VatRate rate) => $"Btw {rate.Description}";
}
