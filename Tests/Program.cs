using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Pharma.Bll;
using Pharma.Dal;
using Pharma.Models;

namespace Pharma.Tests;

public static class AcceptanceRunner
{
    private static int _passed;
    private static int _failed;
    private static readonly string TestHash = PasswordHasher.Hash("test-password");

    public static int Run()
    {
        Run("Produit, doublon et archivage", TestProduit);
        Run("Réception 20 puis vente 3", TestReceptionEtVente);
        Run("FEFO, péremption, blocage et rollback", TestFefoEtRollback);
        Run("Droits vendeur", TestDroits);
        Run("Sauvegarde/restauration", TestSauvegarde);
        Run("Migration base existante", TestMigration);
        Run("Recherche 10 000 produits", TestPerformance);
        Console.WriteLine($"Résultat : {_passed} succès, {_failed} échec(s).");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        Console.WriteLine($"START - {name}");
        try { test(); _passed++; Console.WriteLine($"PASS - {name}"); }
        catch (Exception ex) { _failed++; Console.WriteLine($"FAIL - {name}: {ex}"); }
    }

    private static (string Cs, string Path, Utilisateur Admin, Utilisateur Vendeur) NewDb()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pharma-accept-{Guid.NewGuid():N}.db");
        new DatabaseInitializer(path, seedDefaultAdmin: false).Initialize();
        var cs = $"Data Source={path};Foreign Keys=True";
        using var c = new SqliteConnection(cs); c.Open();
        var admin = AddUser(c, "admin-test", RoleUtilisateur.Administrateur);
        var seller = AddUser(c, "vendeur-test", RoleUtilisateur.Vendeur);
        return (cs, path, admin, seller);
    }

    private static Utilisateur AddUser(SqliteConnection c, string id, RoleUtilisateur role)
    {
        using var q = c.CreateCommand(); q.CommandText = "INSERT INTO Utilisateurs(Identifiant,MotDePasseHache,Role) VALUES($i,$h,$r); SELECT last_insert_rowid();";
        q.Parameters.AddWithValue("$i", id); q.Parameters.AddWithValue("$h", TestHash); q.Parameters.AddWithValue("$r", role.ToString());
        return new Utilisateur { Id = Convert.ToInt64(q.ExecuteScalar()), Identifiant = id, Role = role };
    }

    private static long Product(SqliteConnection c, string reference = "REF-001", decimal price = 1000m)
    {
        using var q = c.CreateCommand(); q.CommandText = "INSERT INTO Produits(Reference,Designation,Categorie,PrixVente) VALUES($r,'Produit test','Test',$p); SELECT last_insert_rowid();";
        q.Parameters.AddWithValue("$r", reference); q.Parameters.AddWithValue("$p", price); return Convert.ToInt64(q.ExecuteScalar());
    }

    private static long Lot(SqliteConnection c, long product, int quantity, DateTime expiry, bool blocked = false)
    {
        using var q = c.CreateCommand(); q.CommandText = "INSERT INTO Lots(ProduitId,NumeroLot,DatePeremption,QuantitePhysique,QuantiteVendable,CoutAchat,IsBloque) VALUES($p,$n,$d,$q,$q,500,$b); SELECT last_insert_rowid();";
        q.Parameters.AddWithValue("$p", product); q.Parameters.AddWithValue("$n", Guid.NewGuid().ToString("N")); q.Parameters.AddWithValue("$d", expiry.ToString("yyyy-MM-dd")); q.Parameters.AddWithValue("$q", quantity); q.Parameters.AddWithValue("$b", blocked ? 1 : 0); return Convert.ToInt64(q.ExecuteScalar());
    }

    private static void TestProduit()
    {
        var x = NewDb(); using var c = new SqliteConnection(x.Cs); c.Open(); var p = Product(c);
        var duplicate = false; try { Product(c); } catch (SqliteException) { duplicate = true; } Check(duplicate, "une référence dupliquée doit être refusée");
        Exec(c, $"INSERT INTO Lots(ProduitId,NumeroLot,DatePeremption,QuantitePhysique,QuantiteVendable,CoutAchat) VALUES({p},'HIST','2099-01-01',1,1,1)");
        var catalogue = new GestionCatalogueService(x.Cs); catalogue.ArchiverProduit((int)p, x.Admin); Check(GetInt(c, $"SELECT IsArchive FROM Produits WHERE Id={p}") == 1, "archivage attendu");
    }

    private static void TestReceptionEtVente()
    {
        Console.WriteLine("  réception: préparation"); var x = NewDb(); using var c = new SqliteConnection(x.Cs); c.Open(); var p = Product(c, "RECEPT-001", 1200m); var ops = new GestionOperationsStockService(x.Cs);
        c.Close(); Console.WriteLine("  réception: validation");
        ops.ValiderReception(new Reception { FournisseurId = null, ReferenceDocument = "BL-001", UtilisateurId = x.Admin.Id }, [(p, "LOT-20", DateTime.Today.AddYears(1), 20, 500m)], x.Admin);
        Console.WriteLine("  réception: vente");
        var lot = GetLong(c, $"SELECT Id FROM Lots WHERE ProduitId={p}"); var stock = new GestionStockService(x.Cs); stock.ValiderVenteTransactionnelle(new Vente { UtilisateurId = x.Vendeur.Id, ModePaiement = ModePaiement.Especes, Total = 3600m }, [new LigneVente { ProduitId = p, LotId = lot, Quantite = 3, PrixVente = 1200m }]);
        Check(GetInt(c, $"SELECT QuantiteVendable FROM Lots WHERE Id={lot}") == 17, "le lot doit conserver 17 unités"); Check(GetInt(c, "SELECT COUNT(*) FROM MouvementsStock WHERE Motif='Vente'") == 1, "mouvement de vente attendu");
    }

    private static void TestFefoEtRollback()
    {
        Console.WriteLine("  FEFO: préparation"); var x = NewDb(); Console.WriteLine("  FEFO: base prête"); using var c = new SqliteConnection(x.Cs); c.Open(); var p = Product(c); var early = Lot(c, p, 3, DateTime.Today.AddDays(10)); var late = Lot(c, p, 10, DateTime.Today.AddDays(30)); Lot(c, p, 10, DateTime.Today.AddDays(5), true); Lot(c, p, 10, DateTime.Today.AddDays(-1));
        c.Close(); Console.WriteLine("  FEFO: sélection"); var service = new GestionStockService(x.Cs); var selected = service.ObtenirLotPrioritaire((int)p, 5); Check(selected[0].Id == early && selected[1].Id == late, "ordre FEFO attendu");
        Console.WriteLine("  FEFO: rollback");
        var failed = false; try { service.ValiderVenteTransactionnelle(new Vente { UtilisateurId = x.Vendeur.Id, ModePaiement = ModePaiement.Especes, Total = 99000m }, [new LigneVente { ProduitId = p, LotId = late, Quantite = 99, PrixVente = 1000m }]); } catch (InvalidOperationException) { failed = true; } Check(failed && GetInt(c, $"SELECT QuantiteVendable FROM Lots WHERE Id={late}") == 10, "rollback complet attendu");
    }

    private static void TestDroits()
    {
        var x = NewDb(); var service = new GestionOperationsStockService(x.Cs); var denied = false; try { service.AjusterStock(1, 5, "test", x.Vendeur); } catch (UnauthorizedAccessException) { denied = true; } Check(denied, "le vendeur ne doit pas ajuster le stock");
    }

    private static void TestSauvegarde()
    {
        var x = NewDb(); using (var c = new SqliteConnection(x.Cs)) { c.Open(); Product(c, "BACKUP-001"); }
        var backup = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pharma-backup-{Guid.NewGuid():N}.db"); var service = new SauvegardeService(x.Cs); service.Sauvegarder(backup, x.Admin); Exec(new SqliteConnection(x.Cs), "");
        Check(File.Exists(backup) && GetInt(new SqliteConnection($"Data Source={backup}"), "SELECT COUNT(*) FROM Produits") >= 1, "sauvegarde lisible attendue");
    }

    private static void TestMigration()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pharma-legacy-{Guid.NewGuid():N}.db"); var cs = $"Data Source={path};Foreign Keys=True"; using (var c = new SqliteConnection(cs)) { c.Open(); Exec(c, "CREATE TABLE Utilisateurs(Id INTEGER PRIMARY KEY, Identifiant TEXT, MotDePasseHache TEXT, Role TEXT); CREATE TABLE Produits(Id INTEGER PRIMARY KEY, Reference TEXT, Designation TEXT, Categorie TEXT, SeuilStockMinimum INTEGER DEFAULT 0, IsArchive INTEGER DEFAULT 0); CREATE TABLE Fournisseurs(Id INTEGER PRIMARY KEY, Nom TEXT); CREATE TABLE Lots(Id INTEGER PRIMARY KEY, ProduitId INTEGER, NumeroLot TEXT, DatePeremption TEXT, QuantitePhysique INTEGER, QuantiteVendable INTEGER, CoutAchat NUMERIC, IsBloque INTEGER DEFAULT 0); CREATE TABLE Ventes(Id INTEGER PRIMARY KEY, DateVente TEXT, ModePaiement TEXT, UtilisateurId INTEGER, Total NUMERIC); CREATE TABLE LignesVente(Id INTEGER PRIMARY KEY, VenteId INTEGER, ProduitId INTEGER, LotId INTEGER, Quantite INTEGER, PrixVente NUMERIC);" ); }
        new DatabaseInitializer(path).Initialize(); using var check = new SqliteConnection(cs); check.Open(); Check(HasColumn(check, "Produits", "CodeBarres") && HasColumn(check, "Ventes", "SessionId"), "migration des colonnes attendue");
    }

    private static void TestPerformance()
    {
        var x = NewDb(); using var c = new SqliteConnection(x.Cs); c.Open(); using var tx = c.BeginTransaction(); for (var i = 0; i < 10000; i++) { using var q = c.CreateCommand(); q.Transaction = tx; q.CommandText = "INSERT INTO Produits(Reference,Designation,Categorie,PrixVente) VALUES($r,$d,'Test',1)"; q.Parameters.AddWithValue("$r", $"REF-{i:00000}"); q.Parameters.AddWithValue("$d", $"Paracetamol {i:00000}"); q.ExecuteNonQuery(); } tx.Commit();
        var repo = new Pharma.Dal.ProduitRepository(x.Cs); var sw = Stopwatch.StartNew(); for (var i = 0; i < 10; i++) _ = repo.ListerCatalogue("REF-09", null, false); sw.Stop(); var average = sw.Elapsed.TotalMilliseconds / 10; Console.WriteLine($"  Mesure: {average:N2} ms/requête, 10 itérations, 10 000 produits, machine {Environment.MachineName}."); Check(sw.Elapsed.TotalSeconds < 20, "recherche anormalement lente");
    }

    private static bool HasColumn(SqliteConnection c, string table, string column) { using var q = c.CreateCommand(); q.CommandText = $"PRAGMA table_info({table})"; using var r = q.ExecuteReader(); while (r.Read()) if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return true; return false; }
    private static int GetInt(SqliteConnection c, string sql) { if (c.State != System.Data.ConnectionState.Open) c.Open(); using var q = c.CreateCommand(); q.CommandText = sql; return Convert.ToInt32(q.ExecuteScalar()); }
    private static long GetLong(SqliteConnection c, string sql) { if (c.State != System.Data.ConnectionState.Open) c.Open(); using var q = c.CreateCommand(); q.CommandText = sql; return Convert.ToInt64(q.ExecuteScalar()); }
    private static void Exec(SqliteConnection c, string sql) { if (string.IsNullOrWhiteSpace(sql)) return; if (c.State != System.Data.ConnectionState.Open) c.Open(); using var q = c.CreateCommand(); q.CommandText = sql; q.ExecuteNonQuery(); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
