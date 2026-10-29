using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

/// Centralise l'audit afin que les opérations sensibles soient journalisées de façon uniforme.
public sealed class ActionLogRepository
{
    private readonly string _connectionString;
    public ActionLogRepository(string connectionString) => _connectionString = connectionString;

    public long Enregistrer(long? utilisateurId, string description, SqliteConnection? connection = null, SqliteTransaction? transaction = null)
    {
        var ownsConnection = connection is null;
        using var cn = ownsConnection ? new SqliteConnection(_connectionString) : null;
        var active = connection ?? cn!;
        if (ownsConnection) active.Open();
        using var command = active.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO LogsActions(DateLog,UtilisateurId,DescriptionAction) VALUES($date,$user,$description); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$user", (object?)utilisateurId ?? DBNull.Value);
        command.Parameters.AddWithValue("$description", description);
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
