using System.Globalization;
using System.Text;
using SharpCoreLedgerSheets.Application.Extensions;
using SharpCoreLedgerSheets.Infrastructure.Extensions;
using SharpCoreLedgerSheets.Infrastructure.Options;
using SharpCoreLedgerSheets.Reports.Abstractions;
using SharpCoreLedgerSheets.Reports.Extensions;
using SharpCoreLedgerSheets.Web.Localization;
using SharpCoreLedgerSheets.Web.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Localization ------------------------------------------------------------------------------
// Dutch is the default, because the product targets the Dutch MKB first: a Dutch entrepreneur gets a
// Dutch application - and Dutch number and date formatting - without configuring anything. The
// neutral resource file (SharedResource.resx) holds the English text and is the fallback for every
// culture; nl, de, fr, es, it, pt and pl each have their own .resx next to it. The resource files
// live in Resources/ alongside the SharedResource marker type, which is exactly what the default
// ResourceManagerStringLocalizer naming convention expects.
// A host that serves another market can move the default without touching code, for example
// 'SharpCoreLedgerSheets:Localization:DefaultCulture' = 'en' in appsettings.json.
builder.Services.AddLocalization();
builder.Services.AddSingleton<IReportLabels, LocalizedReportLabels>();

var configuredDefaultCulture = builder.Configuration["SharpCoreLedgerSheets:Localization:DefaultCulture"];
var defaultCulture = Languages.IsSupported(configuredDefaultCulture ?? string.Empty)
    ? configuredDefaultCulture!
    : Languages.Default;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = Languages.Supported.Select(CultureInfo.GetCultureInfo).ToList();

    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;

    // An explicit ?culture=xx wins over the cookie, the cookie wins over the browser header.
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

// --- Application, storage and reports ----------------------------------------------------------
builder.Services.AddRazorPages();
builder.Services.AddSharpCoreLedgerSheetsApplication();

// The master password is a secret: it is never stored in a committed file. Precedence, highest
// first:
//   1. an explicit setting - appsettings.{Environment}.json, a user secret, a key vault, or
//      'SharpCoreLedgerSheets__Storage__MasterPassword' in the environment (the double underscore is the
//      environment-variable form of the ':' separator)
//   2. an interactive prompt, in Development only, and only when the process owns a real console
// The database cannot be opened without a password, so the app warns rather than failing at
// start-up - the UI then reports that the store is unavailable.
if (builder.Environment.IsDevelopment())
{
    // 'dotnet user-secrets set "SharpCoreLedgerSheets:Storage:MasterPassword" <password>' is the documented
    // development path (see README). Optional: a missing secrets file is not an error.
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

var configuredPassword = builder.Configuration[$"{SharpCoreLedgerSheetsStorageOptions.Section}:MasterPassword"];
var masterPassword = string.IsNullOrWhiteSpace(configuredPassword)
    ? ReadMasterPasswordFromConsole()
    : configuredPassword;

builder.Services.AddSharpCoreLedgerSheetsStorage(options =>
{
    options.DatabasePath =
        builder.Configuration[$"{SharpCoreLedgerSheetsStorageOptions.Section}:DatabasePath"] ?? "App_Data/sharpcoreledgersheets.scdb";
    options.MasterPassword = masterPassword;
});

builder.Services.AddSharpCoreLedgerSheetsReports();

var app = builder.Build();

var storage = app.Services.GetRequiredService<IOptions<SharpCoreLedgerSheetsStorageOptions>>().Value;

if (string.IsNullOrWhiteSpace(storage.MasterPassword))
{
    app.Logger.LogWarning(
        "No master password is configured for the ledger database. Set '{Section}:MasterPassword' " +
        "(for example 'dotnet user-secrets set \"SharpCoreLedgerSheets:Storage:MasterPassword\" <password>') or " +
        "export the environment variable 'SharpCoreLedgerSheets__Storage__MasterPassword'; without a password " +
        "the database cannot be opened.",
        SharpCoreLedgerSheetsStorageOptions.Section);
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

