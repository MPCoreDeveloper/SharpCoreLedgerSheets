using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Produces the complete financial overview (three statements plus ratios) for a period.
/// </summary>
public interface IRapportageService
{
    /// <summary>Builds the overview for <paramref name="periode"/>.</summary>
    Task<FinancieelOverzicht> StelOverzichtOpAsync(Periode periode, CancellationToken ct = default);
}
