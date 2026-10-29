using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class FournisseurRepository
{
    private readonly string _cs; private readonly ActionLogRepository _logs;
    public FournisseurRepository(string connectionString){_cs=connectionString;_logs=new ActionLogRepository(connectionString);}
    public long Ajouter(Fournisseur f,long utilisateurId){Validate(f);using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO Fournisseurs(Nom,Telephone,Email,Adresse,IsArchive) VALUES($nom,$tel,$mail,$adresse,0); SELECT last_insert_rowid();";Add(c,f);var id=Convert.ToInt64(c.ExecuteScalar());_logs.Enregistrer(utilisateurId,$"Création du fournisseur {f.Nom}",cn,tx);tx.Commit();return id;}
    public IReadOnlyList<Fournisseur> Lister(bool inclureArchives=false){using var cn=new SqliteConnection(_cs);cn.Open();using var c=cn.CreateCommand();c.CommandText="SELECT Id,Nom,Telephone,Email,Adresse,IsArchive FROM Fournisseurs WHERE $include=1 OR IsArchive=0 ORDER BY Nom";c.Parameters.AddWithValue("$include",inclureArchives?1:0);using var r=c.ExecuteReader();var list=new List<Fournisseur>();while(r.Read())list.Add(Map(r));return list;}
    public void Modifier(Fournisseur f,long utilisateurId){if(f.Id<=0)throw new ArgumentException("Fournisseur invalide.");Validate(f);using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="UPDATE Fournisseurs SET Nom=$nom,Telephone=$tel,Email=$mail,Adresse=$adresse,IsArchive=$archive WHERE Id=$id";Add(c,f);c.Parameters.AddWithValue("$archive",f.IsArchive?1:0);c.Parameters.AddWithValue("$id",f.Id);if(c.ExecuteNonQuery()!=1)throw new KeyNotFoundException("Fournisseur introuvable.");_logs.Enregistrer(utilisateurId,$"Modification du fournisseur {f.Id}",cn,tx);tx.Commit();}
    public void Archiver(long id,long utilisateurId){using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="UPDATE Fournisseurs SET IsArchive=1 WHERE Id=$id";c.Parameters.AddWithValue("$id",id);if(c.ExecuteNonQuery()!=1)throw new KeyNotFoundException("Fournisseur introuvable.");_logs.Enregistrer(utilisateurId,$"Archivage du fournisseur {id}",cn,tx);tx.Commit();}
    private static void Add(SqliteCommand c,Fournisseur f){c.Parameters.AddWithValue("$nom",f.Nom.Trim());c.Parameters.AddWithValue("$tel",(object?)f.Telephone??DBNull.Value);c.Parameters.AddWithValue("$mail",(object?)f.Email??DBNull.Value);c.Parameters.AddWithValue("$adresse",(object?)f.Adresse??DBNull.Value);}
    private static void Validate(Fournisseur f){if(string.IsNullOrWhiteSpace(f.Nom))throw new ArgumentException("Le nom du fournisseur est obligatoire.");}
    private static Fournisseur Map(SqliteDataReader r)=>new(){Id=r.GetInt64(0),Nom=r.GetString(1),Telephone=r.IsDBNull(2)?null:r.GetString(2),Email=r.IsDBNull(3)?null:r.GetString(3),Adresse=r.IsDBNull(4)?null:r.GetString(4),IsArchive=r.GetBoolean(5)};
}
