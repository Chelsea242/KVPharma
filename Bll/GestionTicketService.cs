using Pharma.Dal;
using Pharma.Models;

namespace Pharma.Bll;

/// <summary>Accès aux tickets avec contrôle d'appartenance pour le profil vendeur.</summary>
public sealed class GestionTicketService
{
    private readonly VenteRepository _ventes;
    public GestionTicketService(string connectionString)=>_ventes=new VenteRepository(connectionString);
    public TicketVente ObtenirTicket(long id,Utilisateur demandeur)
    {
        ArgumentNullException.ThrowIfNull(demandeur);
        return _ventes.ObtenirTicket(id,demandeur.Role==RoleUtilisateur.Administrateur?null:demandeur.Id);
    }
}
