using System.Globalization;
using FinOverzicht.Application.Extensions;
using FinOverzicht.Infrastructure.Extensions;
using FinOverzicht.Infrastructure.Options;
using FinOverzicht.Reports.Abstractions;
using FinOverzicht.Reports.Extensions;
using FinOverzicht.Web.Localization;
using FinOverzicht.Web.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Localization ------------------------------------------------------------------------------
// Dutch is the default; the neutral resource file holds the Dutch strings and every other culture
// has its own .resx next to it (en, de, fr, es, it, pt, pl). The resource files live in
// Resources/ alongside the SharedResource marker type, which is exactly what the default
// ResourceManagerStringLocalizer naming convention expects.
builder.Services.AddLocalization();
builder.Services.AddSingleton<IReportLabels, LocalizedReportLabels>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culturen = Talen.Ondersteund.Select(CultureInfo.GetCultureInfo).ToList();

    options.DefaultRequestCulture = new RequestCulture(Talen.Standaard);
    options.SupportedCultures = culturen;
    options.SupportedUICultures = culturen;

    // An explicit ?culture=xx wins over the cookie, the cookie wins over the browser header.
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
});

// --- Application, storage and reports ----------------------------------------------------------
builder.Services.AddRazorPages();
builder.Services.AddFinOverzichtApplication();

builder.Services.AddFinOverzichtOpslag(options =>
{
    options.DatabasePad =
        builder.Configuration[$"{FinOverzichtOpslagOptions.Sectie}:DatabasePad"] ?? "App_Data/finoverzicht.scdb";
    options.MasterWachtwoord =
        builder.Configuration[$"{FinOverzichtOpslagOptions.Sectie}:MasterWachtwoord"] ?? string.Empty;
});

builder.Services.AddFinOverzichtReports();

var app = builder.Build();

var opslag = app.Services.GetRequiredService<IOptions<FinOverzichtOpslagOptions>>().Value;

if (string.IsNullOrWhiteSpace(opslag.MasterWachtwoord))
{
    app.Logger.LogWarning(
        "Er is geen master-wachtwoord voor de database ingesteld. Stel '{Sectie}:MasterWachtwoord' in via " +
        "user secrets of de omgevingsvariabele 'FinOverzicht__Opslag__MasterWachtwoord'; zonder wachtwoord " +
        "kan de database niet worden geopend.",
        FinOverzichtOpslagOptions.Sectie);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Fout");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Remember an explicit language choice so it survives navigation.
app.Use(async (context, next) =>
{
    if (context.Request.Query.TryGetValue(Talen.CultuurQuerySleutel, out var waarde) &&
        Talen.IsOndersteund(waarde.ToString()))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(waarde.ToString())),
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

