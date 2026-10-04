using LedgerSheets.Domain.Banking;

namespace LedgerSheets.Web.Pages;

/// <summary>
/// One open bank movement on the import page, together with the account the matching assistant
/// proposes for it.
/// </summary>
/// <param name="Transaction">The movement that still has to be booked or ignored.</param>
/// <param name="Proposal">The proposal, or <see langword="null"/> when the assistant has no idea.</param>
public sealed record BankImportLine(BankTransaction Transaction, PostingProposal? Proposal);
