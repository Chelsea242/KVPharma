using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Ui;

/// Alias de compatibilité : la vue de caisse officielle est désormais UC_Vente.
public sealed class UC_Caisse : UserControl
{
    public UC_Caisse(GestionStockService stock, GestionCatalogueService catalogue, GestionTicketService tickets, GestionCaisseService caisse, Utilisateur utilisateur)
    {
        Dock = DockStyle.Fill;
        Controls.Add(new UC_Vente(stock, catalogue, tickets, caisse, utilisateur) { Dock = DockStyle.Fill });
    }
}
