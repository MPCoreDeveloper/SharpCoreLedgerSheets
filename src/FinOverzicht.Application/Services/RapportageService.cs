using FinOverzicht.Application.Models;
using FinOverzicht.Domain.Boekhouden;
using FinOverzicht.Domain.Services;

namespace FinOverzicht.Application.Services;

/// <summary>
/// Default <see cref="IRapportageService"/>: composes the domain statement builders.
/// </summary>
/// <remarks>
/// The balance sheet is taken at the end of the period while the profit and loss statement and
/// the cash flow statement cover the period itself, which is the conventional presentation.
/// </remarks>
public sealed class RapportageService(
    IGrootboekLader grootboekLader,
    IBalansOpsteller balansOpsteller,
    IWinstVerliesOpsteller winstVerliesOpsteller,
    IKasstroomOpsteller kasstroomOpsteller,
    IRatioBerekening ratioBerekening) : IRapportageService
{
    /// <inheritdoc />
    public async Task<FinancieelOverzicht> StelOverzichtOpAsync(Periode periode, CancellationToken ct = default)
    {
        var grootboek = await grootboekLader.LaadAsync(ct).ConfigureAwait(false);

        var balans = balansOpsteller.StelOp(grootboek, periode.Eind);
        var winstVerlies = winstVerliesOpsteller.StelOp(grootboek, periode);
        var kasstroom = kasstroomOpsteller.StelOp(grootboek, periode);
        var ratio = ratioBerekening.Bereken(balans, winstVerlies);

        return new FinancieelOverzicht(balans, winstVerlies, kasstroom, ratio, periode);
    }
}
