using Microsoft.Data.Sqlite;
using Pharma.Dal;
using Pharma.Models;

namespace Pharma.Bll;

public sealed class GestionStockService
{
    private readonly string _cs;
    private readonly ActionLogRepository _logs;

    public GestionStockService(string connectionString)
    {
        _cs = connectionString;
        _logs = new ActionLogRepository(connectionString);
    }

    /// Retourne les lots FEFO à consommer, dans l'ordre de péremption.
    public List<Lot> ObtenirLotPrioritaire(int produitId, int quantiteDemandee)
    {
        if (produitId <= 0) throw new ArgumentOutOfRangeException(nameof(produitId));
        if (quantiteDemandee <= 0) throw new ArgumentOutOfRangeException(nameof(quantiteDemandee));

        using var cn = new SqliteConnection(_cs); cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, ProduitId, NumeroLot, DatePeremption,
                   QuantitePhysique, QuantiteVendable, CoutAchat, IsBloque
            FROM Lots
            WHERE ProduitId = $produit
              AND IsBloque = 0
              AND QuantiteVendable > 0
              AND DatePeremption >= $dateMin
            ORDER BY DatePeremption ASC, Id ASC;
            """;
        cmd.Parameters.AddWithValue("$produit", produitId);
        cmd.Parameters.AddWithValue("$dateMin", DateTime.Today.ToString("yyyy-MM-dd"));

        using var reader = cmd.ExecuteReader();
        var lots = new List<Lot>();
        var stockGlobal = 0;
        while (reader.Read())
        {
            var lot = LireLot(reader);
            stockGlobal += lot.QuantiteVendable;
            lots.Add(lot);
        }

        if (stockGlobal < quantiteDemandee)
            throw new InvalidOperationException($"Stock insuffisant : disponible {stockGlobal}, demandé {quantiteDemandee}.");

        var selection = new List<Lot>();
        var restant = quantiteDemandee;
        foreach (var lot in lots)
        {
            selection.Add(lot);
            restant -= lot.QuantiteVendable;
            if (restant <= 0) break;
        }
        return selection;
    }

    public void ValiderVenteTransactionnelle(Vente vente, List<LigneVente> lignes)
    {
        ArgumentNullException.ThrowIfNull(vente);
        ArgumentNullException.ThrowIfNull(lignes);
        if (lignes.Count == 0) throw new InvalidOperationException("Le panier est vide.");

        using var cn = new SqliteConnection(_cs); cn.Open();
        using var tx = cn.BeginTransaction();
        try
        {
            decimal totalCalcule = 0m;
            foreach (var ligne in lignes)
            {
                if (ligne.ProduitId <= 0 || ligne.LotId <= 0 || ligne.Quantite <= 0 || ligne.PrixVente < 0)
                    throw new InvalidOperationException("Une ligne de vente est invalide.");
                using var prix = cn.CreateCommand(); prix.Transaction = tx; prix.CommandText = "SELECT PrixVente FROM Produits WHERE Id=$produit AND IsArchive=0"; prix.Parameters.AddWithValue("$produit", ligne.ProduitId);
                var prixBase = prix.ExecuteScalar();
                if (prixBase is null || prixBase is DBNull) throw new InvalidOperationException("Un produit du panier est archivé ou introuvable.");
                var prixAutorise = Convert.ToDecimal(prixBase);
                if (ligne.PrixVente != prixAutorise) throw new InvalidOperationException("Un prix du panier ne correspond plus au tarif du catalogue.");
                totalCalcule += ligne.Quantite * prixAutorise;
            }
            if (vente.Total != totalCalcule) throw new InvalidOperationException("Le total du panier a changé. Recalculez la vente avant encaissement.");

            using var sale = cn.CreateCommand(); sale.Transaction = tx;
            sale.CommandText = "INSERT INTO Ventes(DateVente,ModePaiement,UtilisateurId,SessionId,Total) VALUES($date,$mode,$user,$session,$total); SELECT last_insert_rowid();";
            sale.Parameters.AddWithValue("$date", vente.DateVente.ToString("O")); sale.Parameters.AddWithValue("$mode", vente.ModePaiement.ToString()); sale.Parameters.AddWithValue("$user", vente.UtilisateurId); sale.Parameters.AddWithValue("$session", (object?)vente.SessionId ?? DBNull.Value); sale.Parameters.AddWithValue("$total", vente.Total);
            vente.Id = Convert.ToInt64(sale.ExecuteScalar());

            foreach (var ligne in lignes)
            {
                if (ligne.ProduitId <= 0 || ligne.LotId <= 0 || ligne.Quantite <= 0 || ligne.PrixVente < 0)
                    throw new InvalidOperationException("Une ligne de vente est invalide.");

                using (var prixCourant = cn.CreateCommand())
                {
                    prixCourant.Transaction = tx;
                    prixCourant.CommandText = "SELECT PrixVente FROM Produits WHERE Id=$produit AND IsArchive=0";
                    prixCourant.Parameters.AddWithValue("$produit", ligne.ProduitId);
                    var valeur = prixCourant.ExecuteScalar();
                    if (valeur is null || valeur is DBNull) throw new InvalidOperationException("Le produit n'est plus disponible à la vente.");
                    if (Convert.ToDecimal(valeur) != ligne.PrixVente) throw new InvalidOperationException("Le prix du produit a changé. Actualisez le panier avant d’encaisser.");
                }

                using var update = cn.CreateCommand(); update.Transaction = tx;
                update.CommandText = "UPDATE Lots SET QuantiteVendable=QuantiteVendable-$q, QuantitePhysique=QuantitePhysique-$q WHERE Id=$lot AND ProduitId=$produit AND IsBloque=0 AND QuantiteVendable >= $q AND DatePeremption >= $dateMin";
                update.Parameters.AddWithValue("$q", ligne.Quantite); update.Parameters.AddWithValue("$lot", ligne.LotId); update.Parameters.AddWithValue("$produit", ligne.ProduitId); update.Parameters.AddWithValue("$dateMin", DateTime.Today.ToString("yyyy-MM-dd"));
                if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Stock insuffisant ou lot non vendable : {ligne.LotId}.");

                using var line = cn.CreateCommand(); line.Transaction = tx; line.CommandText = "INSERT INTO LignesVente(VenteId,ProduitId,LotId,Quantite,PrixVente) VALUES($vente,$produit,$lot,$q,$prix)";
                line.Parameters.AddWithValue("$vente", vente.Id); line.Parameters.AddWithValue("$produit", ligne.ProduitId); line.Parameters.AddWithValue("$lot", ligne.LotId); line.Parameters.AddWithValue("$q", ligne.Quantite); line.Parameters.AddWithValue("$prix", ligne.PrixVente); line.ExecuteNonQuery();

                using var movement = cn.CreateCommand(); movement.Transaction = tx; movement.CommandText = "INSERT INTO MouvementsStock(DateMouvement,LotId,Quantite,Motif,UtilisateurId) VALUES($date,$lot,$q,'Vente',$user)";
                movement.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("O")); movement.Parameters.AddWithValue("$lot", ligne.LotId); movement.Parameters.AddWithValue("$q", -ligne.Quantite); movement.Parameters.AddWithValue("$user", vente.UtilisateurId); movement.ExecuteNonQuery();
            }

            _logs.Enregistrer(vente.UtilisateurId, $"Vente {vente.Id} validée", cn, tx);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public List<AlerteStock> RecupererAlertesStock(int delaiPeremptionJours = 90)
    {
        if (delaiPeremptionJours < 0) throw new ArgumentOutOfRangeException(nameof(delaiPeremptionJours));
        using var cn = new SqliteConnection(_cs); cn.Open();
        var result = new List<AlerteStock>();

        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT p.Id,p.Designation,COALESCE(SUM(l.QuantiteVendable),0)
                FROM Produits p LEFT JOIN Lots l ON l.ProduitId=p.Id AND l.IsBloque=0 AND l.DatePeremption>=date('now')
                WHERE p.IsArchive=0 GROUP BY p.Id,p.Designation,p.SeuilStockMinimum
                HAVING COALESCE(SUM(l.QuantiteVendable),0)=0 OR COALESCE(SUM(l.QuantiteVendable),0)<=p.SeuilStockMinimum;
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read()) { var stock = r.GetInt64(2); result.Add(new AlerteStock(stock == 0 ? "Rupture" : "SousSeuil", $"{r.GetString(1)} : stock disponible {stock}", r.GetInt64(0))); }
        }

        using (var cmd = cn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT p.Id,p.Designation,l.Id,l.DatePeremption
                FROM Produits p JOIN Lots l ON l.ProduitId=p.Id
                WHERE p.IsArchive=0 AND l.IsBloque=0 AND l.QuantiteVendable>0
                  AND l.DatePeremption BETWEEN date('now') AND date('now','+' || $jours || ' day')
                ORDER BY l.DatePeremption;
                """;
            cmd.Parameters.AddWithValue("$jours", delaiPeremptionJours);
            using var r = cmd.ExecuteReader();
            while (r.Read()) result.Add(new AlerteStock("Peremption", $"{r.GetString(1)} expire le {r.GetString(3)}", r.GetInt64(0), r.GetInt64(2)));
        }
        return result;
    }

    public decimal ObtenirChiffreAffairesDuJour()
    {
        using var cn = new SqliteConnection(_cs); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(Total), 0) FROM Ventes WHERE date(DateVente) = date('now','localtime') AND Statut <> 'Annulee'";
        return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
    }

    // Compatibilité avec les appels déjà présents dans l'interface.
    public List<Lot> ObtenirLotsDisponibles(long produitId)
    {
        using var cn = new SqliteConnection(_cs); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Id,ProduitId,NumeroLot,DatePeremption,QuantitePhysique,QuantiteVendable,CoutAchat,IsBloque FROM Lots WHERE ProduitId=$produit AND IsBloque=0 AND QuantiteVendable>0 AND DatePeremption>= $dateMin ORDER BY DatePeremption,Id";
        cmd.Parameters.AddWithValue("$produit", produitId); cmd.Parameters.AddWithValue("$dateMin", DateTime.Today.ToString("yyyy-MM-dd"));
        using var r = cmd.ExecuteReader(); var lots = new List<Lot>(); while (r.Read()) lots.Add(LireLot(r)); return lots;
    }
    public void ValiderVente(Vente vente, List<LigneVente> lignes) => ValiderVenteTransactionnelle(vente, lignes);
    public List<AlerteStock> RecupererAlertes() => RecupererAlertesStock();

    private static Lot LireLot(SqliteDataReader r) => new()
    {
        Id=r.GetInt64(0), ProduitId=r.GetInt64(1), NumeroLot=r.GetString(2), DatePeremption=DateTime.Parse(r.GetString(3)),
        QuantitePhysique=r.GetInt32(4), QuantiteVendable=r.GetInt32(5), CoutAchat=r.GetDecimal(6), IsBloque=r.GetBoolean(7)
    };
}
