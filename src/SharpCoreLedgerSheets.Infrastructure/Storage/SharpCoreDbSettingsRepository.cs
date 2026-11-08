using System.Globalization;
using System.Text.Json;
using SharpCoreLedgerSheets.Application.Abstractions;
using SharpCoreLedgerSheets.Domain.Accounting;
using SharpCoreLedgerSheets.Domain.Administration;
using SharpCoreDB.Interfaces;

namespace SharpCoreLedgerSheets.Infrastructure.Storage;

/// <summary>
/// SharpCoreDB-backed <see cref="ISettingsRepository"/>.
/// </summary>
/// <remarks>
/// The settings are a handful of key/value rows; the VAT rates travel as JSON so adding a rate never needs a
/// schema change. Anything unreadable (a hand-edited row, a truncated value) falls back to the defaults
/// instead of making the administration unusable.
/// </remarks>
internal sealed class SharpCoreDbSettingsRepository(IDatabase database)
    : SharpCoreDbRepository(database), ISettingsRepository
{
    private const string CompanyNameKey = "company_name";
    private const string FiscalYearStartMonthKey = "fiscal_year_start_month";
    private const string CurrencyKey = "currency";
    private const string VatRatesKey = "vat_rates";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Task<AdministrationSettings> GetAsync(CancellationToken ct = default)
    {
        var rows = WithLock(() => Database.ExecuteQuery(
            $"SELECT name, content FROM {SettingsSchema.TableSettings}"));

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            values[ReadText(row, "name")] = ReadText(row, "content");
        }

        return Task.FromResult(ToSettings(values));
    }

    /// <inheritdoc />
    public Task SaveAsync(AdministrationSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CompanyNameKey] = settings.CompanyName,
            [FiscalYearStartMonthKey] = settings.FiscalYearStartMonth.ToString(CultureInfo.InvariantCulture),
            [CurrencyKey] = settings.Currency.Code,
            [VatRatesKey] = JsonSerializer.Serialize(settings.Vat.Rates.Select(ToDto).ToList(), JsonOptions),
        };

        WithLock(() =>
        {
            foreach (var (name, content) in values)
            {
                Database.ExecuteSQL(
                    $"DELETE FROM {SettingsSchema.TableSettings} WHERE name = @name",
                    Parameters(("@name", name)));

                Database.ExecuteSQL(
                    $"INSERT INTO {SettingsSchema.TableSettings} (name, content) VALUES (@name, @content)",
                    Parameters(("@name", name), ("@content", content)));
            }

            Database.Flush();
        });

        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> Parameters(params (string Name, object? Value)[] values)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in values)
        {
            parameters[name] = value;
        }

        return parameters;
    }

    private static AdministrationSettings ToSettings(IReadOnlyDictionary<string, string> values)
    {
        var companyName = values.TryGetValue(CompanyNameKey, out var storedName) && !string.IsNullOrWhiteSpace(storedName)
            ? storedName
            : AdministrationSettings.DefaultCompanyName;

        var month = values.TryGetValue(FiscalYearStartMonthKey, out var monthText)
            && int.TryParse(monthText, CultureInfo.InvariantCulture, out var parsedMonth)
                ? parsedMonth
                : AdministrationSettings.CalendarYearStart;

        if (month is < 1 or > 12)
        {
            month = AdministrationSettings.CalendarYearStart;
        }

        var currency = values.TryGetValue(CurrencyKey, out var code) ? FindCurrency(code) : null;
        var vat = values.TryGetValue(VatRatesKey, out var json) ? ReadVatScheme(json) : VatScheme.Dutch;

        return new AdministrationSettings(companyName, month, currency, vat);
    }

    private static Currency? FindCurrency(string code) =>
        Currency.All.FirstOrDefault(
            known => string.Equals(known.Code, code, StringComparison.OrdinalIgnoreCase));

    private static VatScheme ReadVatScheme(string json)
    {
        try
        {
            var rates = JsonSerializer.Deserialize<List<VatRateDto>>(json, JsonOptions);

            return rates is null
                ? VatScheme.Dutch
                : new VatScheme(rates.Select(FromDto).ToList());
        }
        catch (JsonException)
        {
            // Unreadable configuration must not brick the administration: fall back to the Dutch scheme.
            return VatScheme.Dutch;
        }
    }

    private static VatRateDto ToDto(VatRate rate) =>
        new(rate.Code, rate.Description, rate.Percentage, rate.OutputAccountNumber, rate.InputAccountNumber);

    private static VatRate FromDto(VatRateDto dto) =>
        new(dto.Code, dto.Description, dto.Percentage, dto.OutputAccountNumber, dto.InputAccountNumber);

    private sealed record VatRateDto(
        string Code,
        string Description,
        decimal Percentage,
        string? OutputAccountNumber,
        string? InputAccountNumber);
}
