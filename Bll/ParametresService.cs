using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Bll;

public sealed class ParametresService
{
    private readonly string _cs; public ParametresService(string connectionString)=>_cs=connectionString;
    public string Obtenir(string cle,string valeurParDefaut="")
    { using var c=new SqliteConnection(_cs);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT Valeur FROM Parametres WHERE Cle=$cle";q.Parameters.AddWithValue("$cle",cle);return q.ExecuteScalar()?.ToString()??valeurParDefaut; }
    public IReadOnlyDictionary<string,string> LireTout(){using var c=new SqliteConnection(_cs);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT Cle,Valeur FROM Parametres ORDER BY Cle";using var r=q.ExecuteReader();var d=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);while(r.Read())d[r.GetString(0)]=r.GetString(1);return d;}
    public void Enregistrer(Utilisateur auteur,IReadOnlyDictionary<string,string> valeurs)
    { ExigerAdmin(auteur);using var c=new SqliteConnection(_cs);c.Open();using var tx=c.BeginTransaction();foreach(var pair in valeurs){using var q=c.CreateCommand();q.Transaction=tx;q.CommandText="INSERT INTO Parametres(Cle,Valeur) VALUES($cle,$val) ON CONFLICT(Cle) DO UPDATE SET Valeur=excluded.Valeur";q.Parameters.AddWithValue("$cle",pair.Key);q.Parameters.AddWithValue("$val",pair.Value??"");q.ExecuteNonQuery();}new Dal.ActionLogRepository(_cs).Enregistrer(auteur.Id,$"Modification des paramètres ({valeurs.Count})",c,tx);tx.Commit();}
    private static void ExigerAdmin(Utilisateur u){if(u.Role!=RoleUtilisateur.Administrateur)throw new UnauthorizedAccessException("Les paramètres sont réservés au responsable.");}
}
