using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class ProduitCrudRepository
{
    private readonly string _cs; private readonly ActionLogRepository _logs;
    public ProduitCrudRepository(string connectionString) { _cs=connectionString; _logs=new ActionLogRepository(connectionString); }

    public long Ajouter(Produit produit, long utilisateurId)
    {
        Validate(produit); using var cn=new SqliteConnection(_cs); cn.Open(); using var tx=cn.BeginTransaction();
        using var cmd=cn.CreateCommand(); cmd.Transaction=tx; cmd.CommandText="INSERT INTO Produits(Reference,CodeBarres,Designation,Categorie,SeuilStockMinimum,PrixVente,IsArchive) VALUES($ref,$barcode,$designation,$categorie,$seuil,$prix,0); SELECT last_insert_rowid();";
        AddParameters(cmd, produit); var id=Convert.ToInt64(cmd.ExecuteScalar()); _logs.Enregistrer(utilisateurId,$"Création du produit {produit.Reference}",cn,tx); tx.Commit(); return id;
    }

    public Produit? Obtenir(long id, bool inclureArchives=false)
    {
        using var cn=new SqliteConnection(_cs); cn.Open(); using var cmd=cn.CreateCommand(); cmd.CommandText="SELECT Id,Reference,CodeBarres,Designation,Categorie,SeuilStockMinimum,PrixVente,IsArchive FROM Produits WHERE Id=$id AND ($include=1 OR IsArchive=0)"; cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$include",inclureArchives?1:0); using var r=cmd.ExecuteReader(); return r.Read()?Map(r):null;
    }

    public IReadOnlyList<Produit> Lister(bool inclureArchives=false)
    {
        using var cn=new SqliteConnection(_cs); cn.Open(); using var cmd=cn.CreateCommand(); cmd.CommandText="SELECT Id,Reference,CodeBarres,Designation,Categorie,SeuilStockMinimum,PrixVente,IsArchive FROM Produits WHERE $include=1 OR IsArchive=0 ORDER BY Designation"; cmd.Parameters.AddWithValue("$include",inclureArchives?1:0); using var r=cmd.ExecuteReader(); var result=new List<Produit>(); while(r.Read()) result.Add(Map(r)); return result;
    }

    public IReadOnlyList<string> ListerCategories()
    {
        using var cn = new SqliteConnection(_cs); cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT Categorie FROM Produits WHERE IsArchive=0 AND trim(Categorie)<>'' ORDER BY Categorie COLLATE NOCASE";
        using var r = cmd.ExecuteReader(); var values = new List<string>();
        while (r.Read()) values.Add(r.GetString(0));
        return values;
    }

    public bool SupprimerSiSansHistorique(long produitId, long utilisateurId)
    {
        using var cn = new SqliteConnection(_cs); cn.Open(); using var tx = cn.BeginTransaction();
        using var check = cn.CreateCommand(); check.Transaction = tx;
        check.CommandText = "SELECT EXISTS(SELECT 1 FROM Lots WHERE ProduitId=$id) OR EXISTS(SELECT 1 FROM LignesVente WHERE ProduitId=$id) OR EXISTS(SELECT 1 FROM MouvementsStock m JOIN Lots l ON l.Id=m.LotId WHERE l.ProduitId=$id) FROM Produits WHERE Id=$id";
        check.Parameters.AddWithValue("$id", produitId);
        var historique = Convert.ToInt64(check.ExecuteScalar() ?? 1L) != 0;
        if (historique) { tx.Rollback(); return false; }
        using var delete = cn.CreateCommand(); delete.Transaction = tx; delete.CommandText = "DELETE FROM Produits WHERE Id=$id"; delete.Parameters.AddWithValue("$id", produitId);
        if (delete.ExecuteNonQuery() != 1) { tx.Rollback(); return false; }
        _logs.Enregistrer(utilisateurId, $"Suppression physique du produit {produitId} sans historique", cn, tx); tx.Commit(); return true;
    }

    public void Modifier(Produit produit, long utilisateurId)
    {
        if(produit.Id<=0) throw new ArgumentException("Produit invalide."); Validate(produit); using var cn=new SqliteConnection(_cs); cn.Open(); using var tx=cn.BeginTransaction(); using var cmd=cn.CreateCommand(); cmd.Transaction=tx; cmd.CommandText="UPDATE Produits SET Reference=$ref,CodeBarres=$barcode,Designation=$designation,Categorie=$categorie,SeuilStockMinimum=$seuil,PrixVente=$prix,IsArchive=$archive WHERE Id=$id"; AddParameters(cmd,produit); cmd.Parameters.AddWithValue("$id",produit.Id); cmd.Parameters.AddWithValue("$archive",produit.IsArchive?1:0); if(cmd.ExecuteNonQuery()!=1) throw new KeyNotFoundException("Produit introuvable."); _logs.Enregistrer(utilisateurId,$"Modification du produit {produit.Id}",cn,tx); tx.Commit();
    }

    public void ModifierPrix(long produitId, decimal nouveauPrix, long utilisateurId)
    {
        if(nouveauPrix<0) throw new ArgumentOutOfRangeException(nameof(nouveauPrix)); using var cn=new SqliteConnection(_cs); cn.Open(); using var tx=cn.BeginTransaction(); using var cmd=cn.CreateCommand(); cmd.Transaction=tx; cmd.CommandText="UPDATE Produits SET PrixVente=$prix WHERE Id=$id AND IsArchive=0"; cmd.Parameters.AddWithValue("$prix",nouveauPrix); cmd.Parameters.AddWithValue("$id",produitId); if(cmd.ExecuteNonQuery()!=1) throw new KeyNotFoundException("Produit introuvable."); _logs.Enregistrer(utilisateurId,$"Changement de prix du produit {produitId} vers {nouveauPrix}",cn,tx); tx.Commit();
    }

    public void Archiver(long produitId, long utilisateurId)
    {
        using var cn=new SqliteConnection(_cs); cn.Open(); using var tx=cn.BeginTransaction(); using var cmd=cn.CreateCommand(); cmd.Transaction=tx; cmd.CommandText="UPDATE Produits SET IsArchive=1 WHERE Id=$id"; cmd.Parameters.AddWithValue("$id",produitId); if(cmd.ExecuteNonQuery()!=1) throw new KeyNotFoundException("Produit introuvable."); _logs.Enregistrer(utilisateurId,$"Archivage du produit {produitId}",cn,tx); tx.Commit();
    }

    private static void AddParameters(SqliteCommand c, Produit p){c.Parameters.AddWithValue("$ref",p.Reference.Trim());c.Parameters.AddWithValue("$barcode",string.IsNullOrWhiteSpace(p.CodeBarres)?DBNull.Value:p.CodeBarres.Trim());c.Parameters.AddWithValue("$designation",p.Designation.Trim());c.Parameters.AddWithValue("$categorie",p.Categorie.Trim());c.Parameters.AddWithValue("$seuil",p.SeuilStockMinimum);c.Parameters.AddWithValue("$prix",p.PrixVente);}
    private static void Validate(Produit p){if(string.IsNullOrWhiteSpace(p.Reference)||string.IsNullOrWhiteSpace(p.Designation))throw new ArgumentException("Référence et désignation obligatoires.");if(p.SeuilStockMinimum<0||p.PrixVente<0)throw new ArgumentException("Les valeurs numériques ne peuvent pas être négatives.");}
    private static Produit Map(SqliteDataReader r)=>new(){Id=r.GetInt64(0),Reference=r.GetString(1),CodeBarres=r.IsDBNull(2)?"":r.GetString(2),Designation=r.GetString(3),Categorie=r.GetString(4),SeuilStockMinimum=r.GetInt32(5),PrixVente=r.GetDecimal(6),IsArchive=r.GetBoolean(7)};
}
