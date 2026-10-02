namespace FinOverzicht.Reports.Reporting;

/// <summary>
/// Shared, dependency-free styling for every report template.
/// </summary>
/// <remarks>
/// The CSS is injected into the template through ViewData so the produced HTML document is fully
/// self-contained: it renders identically in a browser preview and when printed to PDF, with no
/// network access and no JavaScript required.
/// </remarks>
internal static class ReportAssets
{
    internal const string Css = """
        :root {
            --ink: #16212e;
            --muted: #63748b;
            --line: #d8e0ea;
            --accent: #0b6e4f;
            --accent-soft: #e6f3ee;
            --debit: #16212e;
            --credit: #0b6e4f;
            --danger: #a4262c;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            font-family: "Segoe UI", system-ui, -apple-system, "Helvetica Neue", Arial, sans-serif;
            color: var(--ink);
            background: #f4f6f9;
            font-size: 14px;
            line-height: 1.5;
        }
        .report-shell { max-width: 1080px; margin: 0 auto; padding: 32px 28px 48px; background: #fff; }
        .report-header {
            display: flex; justify-content: space-between; align-items: flex-start;
            gap: 24px; padding-bottom: 18px; border-bottom: 3px solid var(--accent);
        }
        .report-header h1 { margin: 0 0 6px; font-size: 24px; letter-spacing: -0.01em; }
        .report-header p { margin: 0; color: var(--muted); }
        .generated { text-align: right; color: var(--muted); font-size: 12px; white-space: nowrap; }
        h2 { margin: 32px 0 12px; font-size: 18px; }
        h3 { margin: 22px 0 8px; font-size: 15px; color: var(--muted); text-transform: uppercase; letter-spacing: 0.06em; }
        table { width: 100%; border-collapse: collapse; margin-bottom: 8px; }
        th, td { padding: 7px 10px; text-align: left; border-bottom: 1px solid var(--line); }
        th { font-size: 12px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--muted); }
        td.amount, th.amount { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
        tr.total td { font-weight: 700; border-top: 2px solid var(--ink); border-bottom: none; }
        tr.subtotal td { font-weight: 600; background: var(--accent-soft); border-bottom: none; }
        .negative { color: var(--danger); }
        .columns { display: flex; gap: 28px; flex-wrap: wrap; }
        .columns > section { flex: 1 1 320px; }
        .badge { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; }
        .badge.ok { background: var(--accent-soft); color: var(--accent); }
        .badge.warn { background: #fdecec; color: var(--danger); }
        .empty { color: var(--muted); font-style: italic; padding: 6px 10px; }
        .ratio-list { list-style: none; margin: 0; padding: 0; }
        .ratio-list li { display: flex; justify-content: space-between; gap: 16px; padding: 8px 0; border-bottom: 1px solid var(--line); }
        .ratio-value { font-variant-numeric: tabular-nums; font-weight: 600; }
        .report-footer { margin-top: 40px; padding-top: 14px; border-top: 1px solid var(--line); color: var(--muted); font-size: 12px; }
        @media print {
            body { background: #fff; }
            .report-shell { max-width: none; padding: 0; }
            h2 { page-break-after: avoid; }
            table { page-break-inside: auto; }
            tr { page-break-inside: avoid; }
        }
        """;
}
