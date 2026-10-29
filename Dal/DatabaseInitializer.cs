using Microsoft.Data.Sqlite;

namespace Pharma.Dal;

public sealed class DatabaseInitializer
{
    public string DatabasePath { get; }
    private readonly bool _seedDefaultAdmin;
    public string ConnectionString => $"Data Source={DatabasePath};Foreign Keys=True";

    public DatabaseInitializer(string? databasePath = null, bool seedDefaultAdmin = true)
    {
        DatabasePath = databasePath ?? Path.Combine(ResolveDataDirectory(), "pharma.db");
        _seedDefaultAdmin = seedDefaultAdmin;
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
    }

    public void Initialize()
    {
        BackupBeforeSchemaChangeIfNeeded();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Database", "schema.sql"));
        command.ExecuteNonQuery();
        ApplySmallMigrations(connection);
        if (_seedDefaultAdmin) SeedAdmin(connection);
    }

    public static string ResolveDataDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("PHARMA_DATA_DIR");
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Pharma")
            : configured.Trim();
        Directory.CreateDirectory(directory);
        return directory;
    }

    private void BackupBeforeSchemaChangeIfNeeded()
    {
        if (!File.Exists(DatabasePath) || new FileInfo(DatabasePath).Length == 0) return;
        var backupDirectory = Path.Combine(Path.GetDirectoryName(DatabasePath) ?? ResolveDataDirectory(), "Backups");
        Directory.CreateDirectory(backupDirectory);
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            using var checkpoint = connection.CreateCommand();
            checkpoint.CommandText = "PRAGMA wal_checkpoint(FULL);";
            checkpoint.ExecuteNonQuery();
        }
        var backupPath = Path.Combine(backupDirectory, $"pharma-backup-before-update-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.db");
        if (!File.Exists(backupPath)) File.Copy(DatabasePath, backupPath, false);
    }

    private static void ApplySmallMigrations(SqliteConnection connection)
    {
        // Permet de conserver une base créée avec une version antérieure du prototype.
        foreach (var sql in new[] { "ALTER TABLE Produits ADD COLUMN PrixVente NUMERIC NOT NULL DEFAULT 0", "ALTER TABLE Produits ADD COLUMN CodeBarres TEXT", "ALTER TABLE Lots ADD COLUMN FournisseurId INTEGER REFERENCES Fournisseurs(Id) ON DELETE SET NULL", "ALTER TABLE Ventes ADD COLUMN Statut TEXT NOT NULL DEFAULT 'Terminee'", "ALTER TABLE Ventes ADD COLUMN SessionId INTEGER REFERENCES CaisseSessions(Id) ON DELETE SET NULL" })
        {
            try { using var migration = connection.CreateCommand(); migration.CommandText = sql; migration.ExecuteNonQuery(); }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        }
        using var index = connection.CreateCommand();
        index.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS UX_Produits_CodeBarres ON Produits(CodeBarres) WHERE CodeBarres IS NOT NULL AND trim(CodeBarres) <> ''; CREATE INDEX IF NOT EXISTS IX_Ventes_Session ON Ventes(SessionId, ModePaiement, Statut);";
        index.ExecuteNonQuery();
    }

    private static void SeedAdmin(SqliteConnection connection)
    {
        using var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM Utilisateurs WHERE Identifiant = $id COLLATE NOCASE";
        check.Parameters.AddWithValue("$id", "admin");
        if (Convert.ToInt64(check.ExecuteScalar()) != 0) return;
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO Utilisateurs (Identifiant, MotDePasseHache, Role) VALUES ($id,$hash,'Administrateur')";
        insert.Parameters.AddWithValue("$id", "admin");
        insert.Parameters.AddWithValue("$hash", Bll.PasswordHasher.Hash("admin"));
        insert.ExecuteNonQuery();
    }

}
