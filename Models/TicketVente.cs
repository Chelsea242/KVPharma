namespace Pharma.Models;

public sealed record LigneTicket(string Designation, int Quantite, decimal PrixUnitaire, decimal Remise, decimal SousTotal);
public sealed record TicketVente(long Id, DateTime Date, string Vendeur, string Paiement, decimal Total, IReadOnlyList<LigneTicket> Lignes);
