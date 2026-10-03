using FinOverzicht.Domain.Bankzaken;

namespace FinOverzicht.Web.Pages;

/// <summary>
/// One open bank movement on the import page, together with the account the matching assistant
/// proposes for it.
/// </summary>
/// <param name="Transactie">The movement that still has to be booked or ignored.</param>
/// <param name="Voorstel">The proposal, or <see langword="null"/> when the assistant has no idea.</param>
public sealed record BankimportRegel(BankTransactie Transactie, Boekingsvoorstel? Voorstel);
