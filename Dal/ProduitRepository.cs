using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class ProduitRepository
{
    private readonly string _connectionString;
    public ProduitRepository(string connectionString) => _connectionString = connectionString;

    public List<(Produit Produit, Lot Lot)> Rechercher(string texte)
    {
        using var cn = new SqliteConnection(_connectionString); cn.Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"SELECT p.Id,p.Reference,p.Designation,p.Categorie,p.SeuilStockMinimum,p.IsArchive,p.PrixVente,
                   l.Id,l.NumeroLot,l.DatePeremption,l.QuantitePhysique,l.QuantiteVendable,l.CoutAchat,l.IsBloque
            FROM Produits p JOIN Lots l ON l.ProduitId=p.Id
            WHERE p.IsArchive=0 AND l.IsBloque=0 AND l.QuantiteVendable>0
              AND l.DatePeremption >= $today
              AND (p.CodeBarres=$q OR p.Reference LIKE $q || '%' COLLATE NOCASE OR p.Designation LIKE '%' || $q || '%' COLLATE NOCASE)
            ORDER BY p.Designation, l.DatePeremption LIMIT 100";
        cmd.Parameters.AddWithValue("$q", texte.Trim()); cmd.Parameters.AddWithValue("$today", DateTime.Today.ToString("yyyy-MM-dd"));
        using var reader = cmd.ExecuteReader(); var result = new List<(Produit, Lot)>();
        while (reader.Read()) result.Add((new Produit { Id=reader.GetInt64(0), Reference=reader.GetString(1), Designation=reader.GetString(2), Categorie=reader.GetString(3), SeuilStockMinimum=reader.GetInt32(4), IsArchive=reader.GetBoolean(5), PrixVente=reader.GetDecimal(6) }, new Lot { Id=reader.GetInt64(7), ProduitId=reader.GetInt64(0), NumeroLot=reader.GetString(8), DatePeremption=DateTime.Parse(reader.GetString(9)), QuantitePhysique=reader.GetInt32(10), QuantiteVendable=reader.GetInt32(11), CoutAchat=reader.GetDecimal(12), IsBloque=reader.GetBoolean(13) }));
        return result;
    }

    public List<(Produit Produit, int StockPhysique, int StockVendable)> ListerCatalogue(string texte, string? categorie, bool inclureArchives)
    {
        using var cn = new SqliteConnection(_connectionString); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT p.Id,p.Reference,p.CodeBarres,p.Designation,p.Categorie,p.SeuilStockMinimum,p.PrixVente,p.IsArchive,
                   COALESCE(SUM(l.QuantitePhysique),0),
                   COALESCE(SUM(CASE WHEN l.IsBloque=0 AND l.DatePeremption>=date('now','localtime') THEN l.QuantiteVendable ELSE 0 END),0)
            FROM Produits p LEFT JOIN Lots l ON l.ProduitId=p.Id
            WHERE ($archives=1 OR p.IsArchive=0)
              AND ($q='' OR p.CodeBarres=$q OR p.Reference LIKE $prefix ESCAPE '\' COLLATE NOCASE OR p.Designation LIKE $prefix ESCAPE '\' COLLATE NOCASE)
              AND ($categorie IS NULL OR p.Categorie=$categorie COLLATE NOCASE)
            GROUP BY p.Id ORDER BY p.Designation COLLATE NOCASE LIMIT 1000;
            """;
        var q = texte.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        cmd.Parameters.AddWithValue("$archives", inclureArchives ? 1 : 0);
        cmd.Parameters.AddWithValue("$q", q); cmd.Parameters.AddWithValue("$prefix", q + "%");
        cmd.Parameters.AddWithValue("$categorie", (object?)categorie ?? DBNull.Value);
        using var r = cmd.ExecuteReader(); var list = new List<(Produit, int, int)>();
        while (r.Read()) list.Add((new Produit { Id=r.GetInt64(0),Reference=r.GetString(1),CodeBarres=r.IsDBNull(2)?"":r.GetString(2),Designation=r.GetString(3),Categorie=r.GetString(4),SeuilStockMinimum=r.GetInt32(5),PrixVente=r.GetDecimal(6),IsArchive=r.GetBoolean(7) },r.GetInt32(8),r.GetInt32(9)));
        return list;
    }
}
