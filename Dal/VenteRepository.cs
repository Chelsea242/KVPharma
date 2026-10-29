using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class VenteRepository
{
    private readonly string _cs;
    public VenteRepository(string connectionString) => _cs=connectionString;
    public TicketVente ObtenirTicket(long venteId, long? utilisateurAutorise = null)
    {
        using var cn=new SqliteConnection(_cs);cn.Open();using var head=cn.CreateCommand();head.CommandText="SELECT v.DateVente,u.Identifiant,v.ModePaiement,v.Total FROM Ventes v JOIN Utilisateurs u ON u.Id=v.UtilisateurId WHERE v.Id=$id AND ($user IS NULL OR v.UtilisateurId=$user)";head.Parameters.AddWithValue("$id",venteId);head.Parameters.AddWithValue("$user",(object?)utilisateurAutorise??DBNull.Value);using var h=head.ExecuteReader();if(!h.Read())throw new KeyNotFoundException("Vente introuvable ou accès non autorisé.");var date=DateTime.Parse(h.GetString(0));var vendeur=h.GetString(1);var paiement=h.GetString(2);var total=h.GetDecimal(3);h.Close();
        using var lines=cn.CreateCommand();lines.CommandText="SELECT p.Designation,l.Quantite,l.PrixVente,0,l.Quantite*l.PrixVente FROM LignesVente l JOIN Produits p ON p.Id=l.ProduitId WHERE l.VenteId=$id ORDER BY l.Id";lines.Parameters.AddWithValue("$id",venteId);using var r=lines.ExecuteReader();var items=new List<LigneTicket>();while(r.Read())items.Add(new LigneTicket(r.GetString(0),r.GetInt32(1),r.GetDecimal(2),r.GetDecimal(3),r.GetDecimal(4)));return new TicketVente(venteId,date,vendeur,paiement,total,items);
    }
}
