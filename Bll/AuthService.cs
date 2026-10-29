using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Bll;

public sealed class AuthService
{
    private readonly string _cs;
    public string DatabasePath { get; }
    public AuthService(string connectionString) { _cs = connectionString; DatabasePath = new SqliteConnectionStringBuilder(connectionString).DataSource; }
    public Utilisateur? Authentifier(string identifiant, string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(identifiant) || string.IsNullOrEmpty(motDePasse)) return null;
        using var cn = new SqliteConnection(_cs); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id,Identifiant,MotDePasseHache,Role FROM Utilisateurs WHERE Identifiant=$id COLLATE NOCASE"; cmd.Parameters.AddWithValue("$id", identifiant.Trim());
        using var r = cmd.ExecuteReader(); if (!r.Read() || !PasswordHasher.Verify(motDePasse, r.GetString(2))) return null;
        return new Utilisateur { Id=r.GetInt64(0), Identifiant=r.GetString(1), MotDePasseHache=r.GetString(2), Role=Enum.Parse<RoleUtilisateur>(r.GetString(3)) };
    }

    public bool ExisteAuMoinsUnUtilisateur()
    {
        using var cn=new SqliteConnection(_cs);cn.Open();using var cmd=cn.CreateCommand();cmd.CommandText="SELECT EXISTS(SELECT 1 FROM Utilisateurs LIMIT 1)";return Convert.ToInt64(cmd.ExecuteScalar())==1;
    }

    /// <summary>Crée le premier compte responsable uniquement si aucun compte n'existe.</summary>
    public bool InitialiserPremierAdministrateur(string identifiant,string motDePasse)
    {
        if(string.IsNullOrWhiteSpace(identifiant)||identifiant.Trim().Length<3)throw new ArgumentException("L’identifiant doit contenir au moins 3 caractères.");
        if(string.IsNullOrWhiteSpace(motDePasse)||motDePasse.Length<12)throw new ArgumentException("Le mot de passe initial doit contenir au moins 12 caractères.");
        using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();using var check=cn.CreateCommand();check.Transaction=tx;check.CommandText="SELECT EXISTS(SELECT 1 FROM Utilisateurs LIMIT 1)";if(Convert.ToInt64(check.ExecuteScalar())==1){tx.Rollback();return false;}
        using var insert=cn.CreateCommand();insert.Transaction=tx;insert.CommandText="INSERT INTO Utilisateurs(Identifiant,MotDePasseHache,Role) VALUES($id,$hash,'Administrateur')";insert.Parameters.AddWithValue("$id",identifiant.Trim());insert.Parameters.AddWithValue("$hash",PasswordHasher.Hash(motDePasse));insert.ExecuteNonQuery();tx.Commit();return true;
    }
}
