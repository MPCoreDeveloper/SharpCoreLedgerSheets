using System.Globalization;
using System.Text;
using LedgerSheets.Application.Extensions;
using LedgerSheets.Infrastructure.Extensions;
using LedgerSheets.Infrastructure.Options;
using LedgerSheets.Reports.Abstractions;
using LedgerSheets.Reports.Extensions;
using LedgerSheets.Web.Localization;
using LedgerSheets.Web.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Localization ------------------------------------------------------------------------------
// English is the default: the neutral resource file holds the English strings, and every other
// culture has its own .resx next to it (nl, de, fr, es, it, pt, pl). The resource files live in
// Resources/ alongside the SharedResource marker type, which is exactly what the default
// ResourceManagerStringLocalizer naming convention expects.
builder.Services.AddLocalization();
builder.Services.AddSingleton<IReportLabels, LocalizedReportLabels>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = Languages.Supported.Select(CultureInfo.GetCultureInfo).ToList();

    options.DefaultRequestCulture = new RequestCulture(Languages.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    // An explicit ?culture=xx wins over the cookie, the cookie wins over the browser header.
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

// --- Application, storage and reports ----------------------------------------------------------
builder.Services.AddRazorPages();
builder.Services.AddLedgerSheetsApplication();

// The master password is a secret: it is never stored in a committed file. Precedence, highest
// first:
//   1. an explicit setting - appsettings.{Environment}.json, a user secret, a key vault, or
//      'LedgerSheets__Storage__MasterPassword' in the environment (the double underscore is the
//      environment-variable form of the ':' separator)
//   2. an interactive prompt, in Development only, and only when the process owns a real console
// The database cannot be opened without a password, so the app warns rather than failing at
// start-up - the UI then reports that the store is unavailable.
if (builder.Environment.IsDevelopment())
{
    // 'dotnet user-secrets set "LedgerSheets:Storage:MasterPassword" <password>' is the documented
    // development path (see README). Optional: a missing secrets file is not an error.
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

var configuredPassword = builder.Configuration[$"{LedgerSheetsStorageOptions.Section}:MasterPassword"];
var masterPassword = string.IsNullOrWhiteSpace(configuredPassword)
    ? ReadMasterPasswordFromConsole()
    : configuredPassword;

builder.Services.AddLedgerSheetsStorage(options =>
{
    options.DatabasePath =
        builder.Configuration[$"{LedgerSheetsStorageOptions.Section}:DatabasePath"] ?? "App_Data/ledgersheets.scdb";
    options.MasterPassword = masterPassword;
});

builder.Services.AddLedgerSheetsReports();

var app = builder.Build();

var storage = app.Services.GetRequiredService<IOptions<LedgerSheetsStorageOptions>>().Value;

if (string.IsNullOrWhiteSpace(storage.MasterPassword))
{
    app.Logger.LogWarning(
        "No master password is configured for the ledger database. Set '{Section}:MasterPassword' " +
        "(for example 'dotnet user-secrets set \"LedgerSheets:Storage:MasterPassword\" <password>') or " +
        "export the environment variable 'LedgerSheets__Storage__MasterPassword'; without a password " +
        "the database cannot be opened.",
        LedgerSheetsStorageOptions.Section);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Remember an explicit language choice so it survives navigation.
app.Use(async (context, next) =>
{
    if (context.Request.Query.TryGetValue(Languages.CultureQueryKey, out var value) &&
        Languages.IsSupported(value.ToString()))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(value.ToString())),
            new CookieOptions
            {
                Path = "/",
                IsEssential = true,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(365),
            });
    }

    await next().ConfigureAwait(false);
});

app.MapRazorPages();

await app.RunAsync().ConfigureAwait(false);

// Prompts for the master password without echoing it. Returns an empty string when there is no
// console to prompt on (tests, CI, a redirected pipe), so the caller logs the warning instead of
// blocking a headless run.
static string ReadMasterPasswordFromConsole()
{
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
    {
        return string.Empty;
    }

    Console.Write("Master password for the ledger database (Enter to skip): ");
    var password = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter) { break; }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0) { password.Length--; }
            continue;
        }

        if (!char.IsControl(key.KeyChar)) { password.Append(key.KeyChar); }
    }

    Console.WriteLine();
    return password.ToString();
}

