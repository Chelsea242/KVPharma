using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Bll;

public sealed class RapportService
{
    private readonly string _cs;
    public RapportService(string connectionString) => _cs = connectionString;

    public IReadOnlyList<(DateTime Date, long Id, string Paiement, string Vendeur, decimal Total)> Ventes(DateTime debut, DateTime fin, Utilisateur demandeur)
    {
        ExigerResponsable(demandeur);
        if (fin.Date < debut.Date) throw new ArgumentException("La date de fin doit être postérieure ou égale à la date de début.");
        using var cn=new SqliteConnection(_cs);cn.Open();using var cmd=cn.CreateCommand();
        cmd.CommandText="SELECT v.DateVente,v.Id,v.ModePaiement,u.Identifiant,v.Total FROM Ventes v JOIN Utilisateurs u ON u.Id=v.UtilisateurId WHERE date(v.DateVente) BETWEEN $debut AND $fin AND v.Statut<>'Annulee' ORDER BY v.DateVente DESC";
        cmd.Parameters.AddWithValue("$debut",debut.ToString("yyyy-MM-dd"));cmd.Parameters.AddWithValue("$fin",fin.ToString("yyyy-MM-dd"));
        using var r=cmd.ExecuteReader();var rows=new List<(DateTime,long,string,string,decimal)>();while(r.Read())rows.Add((DateTime.Parse(r.GetString(0)),r.GetInt64(1),r.GetString(2),r.GetString(3),r.GetDecimal(4)));return rows;
    }

    public IReadOnlyList<(string Reference,string Designation,int Physique,int Vendable,string Etat)> EtatStock(Utilisateur demandeur)
    { ExigerResponsable(demandeur);using var c=new SqliteConnection(_cs);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT p.Reference,p.Designation,p.SeuilStockMinimum,COALESCE(SUM(l.QuantitePhysique),0),COALESCE(SUM(CASE WHEN l.IsBloque=0 AND l.DatePeremption>=date('now','localtime') THEN l.QuantiteVendable ELSE 0 END),0) FROM Produits p LEFT JOIN Lots l ON l.ProduitId=p.Id WHERE p.IsArchive=0 GROUP BY p.Id ORDER BY p.Designation";using var r=q.ExecuteReader();var rows=new List<(string,string,int,int,string)>();while(r.Read()){var seuil=r.GetInt32(2);var phy=r.GetInt32(3);var ven=r.GetInt32(4);rows.Add((r.GetString(0),r.GetString(1),phy,ven,ven==0?"Rupture":ven<=seuil?"Stock faible":"Disponible"));}return rows;}

    public IReadOnlyList<(string Designation,int Quantite,decimal ChiffreAffaires,decimal Marge)> ProduitsLesPlusVendus(DateTime debut,DateTime fin,Utilisateur demandeur)
    { ExigerResponsable(demandeur);using var c=new SqliteConnection(_cs);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT p.Designation,SUM(l.Quantite),SUM(l.Quantite*l.PrixVente),SUM(l.Quantite*(l.PrixVente-lt.CoutAchat)) FROM LignesVente l JOIN Ventes v ON v.Id=l.VenteId JOIN Produits p ON p.Id=l.ProduitId JOIN Lots lt ON lt.Id=l.LotId WHERE date(v.DateVente) BETWEEN $debut AND $fin AND v.Statut='Terminee' GROUP BY p.Id,p.Designation ORDER BY SUM(l.Quantite) DESC LIMIT 100";q.Parameters.AddWithValue("$debut",debut.ToString("yyyy-MM-dd"));q.Parameters.AddWithValue("$fin",fin.ToString("yyyy-MM-dd"));using var r=q.ExecuteReader();var rows=new List<(string,int,decimal,decimal)>();while(r.Read())rows.Add((r.GetString(0),r.GetInt32(1),r.GetDecimal(2),r.GetDecimal(3)));return rows;}

    public IReadOnlyList<(string Designation,string NumeroLot,DateTime Peremption,int Vendable)> Peremptions(int jours,Utilisateur demandeur)
    { ExigerResponsable(demandeur);using var c=new SqliteConnection(_cs);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT p.Designation,l.NumeroLot,l.DatePeremption,l.QuantiteVendable FROM Lots l JOIN Produits p ON p.Id=l.ProduitId WHERE p.IsArchive=0 AND l.IsBloque=0 AND l.QuantiteVendable>0 AND l.DatePeremption BETWEEN date('now','localtime') AND date('now','localtime','+' || $jours || ' day') ORDER BY l.DatePeremption";q.Parameters.AddWithValue("$jours",jours);using var r=q.ExecuteReader();var rows=new List<(string,string,DateTime,int)>();while(r.Read())rows.Add((r.GetString(0),r.GetString(1),DateTime.Parse(r.GetString(2)),r.GetInt32(3)));return rows;}

    private static void ExigerResponsable(Utilisateur u){if(u.Role!=RoleUtilisateur.Administrateur)throw new UnauthorizedAccessException("Rapport réservé au responsable.");}
}
