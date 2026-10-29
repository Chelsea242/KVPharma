using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Bll;

public sealed class SauvegardeService
{
    private readonly string _databasePath; private readonly string _connectionString; private readonly ParametresService _parametres;
    public SauvegardeService(string connectionString){_connectionString=connectionString;_databasePath=new SqliteConnectionStringBuilder(connectionString).DataSource;_parametres=new ParametresService(connectionString);}
    public string DossierParDefaut=>Path.Combine(Path.GetDirectoryName(_databasePath)!,"Sauvegardes");
    public string Sauvegarder(string destination,Utilisateur auteur)
    { ExigerAdmin(auteur);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);using var source=new SqliteConnection(_connectionString);source.Open();using var checkpoint=source.CreateCommand();checkpoint.CommandText="PRAGMA wal_checkpoint(FULL)";checkpoint.ExecuteNonQuery();var cs=new SqliteConnectionStringBuilder{DataSource=destination,Mode=SqliteOpenMode.ReadWriteCreate,Cache=SqliteCacheMode.Private}.ToString();using var cible=new SqliteConnection(cs);cible.Open();source.BackupDatabase(cible);File.SetLastWriteTimeUtc(destination,DateTime.UtcNow);return destination;}
    public string? ExecuterSauvegardeQuotidienne(Utilisateur auteur,DateTime maintenant)
    { ExigerAdmin(auteur);var dossier=_parametres.Obtenir("DossierSauvegarde",DossierParDefaut);var heure=_parametres.Obtenir("HeureSauvegarde","22:00");if(!TimeSpan.TryParse(heure,out var horaire))horaire=new TimeSpan(22,0,0);var dernier=_parametres.Obtenir("DerniereSauvegardeUtc","");if(DateTime.TryParse(dernier,out var date)&&date.Date==maintenant.Date)return null;if(maintenant.TimeOfDay<horaire)return null;Directory.CreateDirectory(dossier);var chemin=Path.Combine(dossier,$"pharma-{maintenant:yyyyMMdd-HHmmss}.db");var result=Sauvegarder(chemin,auteur);_parametres.Enregistrer(auteur,new Dictionary<string,string>{{"DerniereSauvegardeUtc",maintenant.ToString("O")}});return result;}
    public string Restaurer(string sauvegarde,Utilisateur auteur)
    { ExigerAdmin(auteur);if(!File.Exists(sauvegarde))throw new FileNotFoundException("Sauvegarde introuvable.",sauvegarde);var securite=$"{_databasePath}.avant-restauration-{DateTime.Now:yyyyMMdd-HHmmss}.db";Sauvegarder(securite,auteur);using var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=sauvegarde,Mode=SqliteOpenMode.ReadOnly}.ToString());source.Open();using var cible=new SqliteConnection(_connectionString);cible.Open();source.BackupDatabase(cible);return securite;}
    private static void ExigerAdmin(Utilisateur u){if(u.Role!=RoleUtilisateur.Administrateur)throw new UnauthorizedAccessException("Les sauvegardes sont réservées au responsable.");}
}
