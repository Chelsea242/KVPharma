using Microsoft.Data.Sqlite;
using Pharma.Bll;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class UtilisateurRepository
{
    private readonly string _connectionString;
    public UtilisateurRepository(string connectionString) => _connectionString = connectionString;

    public long Inscrire(string identifiant, string motDePasse, RoleUtilisateur role)
    {
        if (string.IsNullOrWhiteSpace(identifiant) || string.IsNullOrWhiteSpace(motDePasse)) throw new ArgumentException("Identifiant et mot de passe obligatoires.");
        using var cn = new SqliteConnection(_connectionString); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = "INSERT INTO Utilisateurs(Identifiant,MotDePasseHache,Role) VALUES($id,$hash,$role); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$id", identifiant.Trim()); cmd.Parameters.AddWithValue("$hash", PasswordHasher.Hash(motDePasse)); cmd.Parameters.AddWithValue("$role", role.ToString());
        try { return Convert.ToInt64(cmd.ExecuteScalar()); } catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { throw new InvalidOperationException("Cet identifiant existe déjà.", ex); }
    }

    public Utilisateur? Verifier(string identifiant, string motDePasse)
    {
        using var cn = new SqliteConnection(_connectionString); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id,Identifiant,MotDePasseHache,Role FROM Utilisateurs WHERE Identifiant=$id"; cmd.Parameters.AddWithValue("$id", identifiant.Trim());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read() || !PasswordHasher.Verify(motDePasse, reader.GetString(2))) return null;
        return new Utilisateur { Id=reader.GetInt64(0), Identifiant=reader.GetString(1), MotDePasseHache=reader.GetString(2), Role=Enum.Parse<RoleUtilisateur>(reader.GetString(3)) };
    }
}
