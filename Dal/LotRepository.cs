using Microsoft.Data.Sqlite;
using Pharma.Models;

namespace Pharma.Dal;

public sealed class LotRepository
{
    private readonly string _cs;
    public LotRepository(string connectionString) => _cs = connectionString;

    public IReadOnlyList<(Lot Lot, string Reference, string Designation)> Lister()
    {
        using var cn = new SqliteConnection(_cs); cn.Open(); using var cmd = cn.CreateCommand();
        cmd.CommandText = """
            SELECT l.Id,l.ProduitId,l.NumeroLot,l.DatePeremption,l.QuantitePhysique,l.QuantiteVendable,l.CoutAchat,l.IsBloque,p.Reference,p.Designation
            FROM Lots l JOIN Produits p ON p.Id=l.ProduitId WHERE p.IsArchive=0 ORDER BY l.DatePeremption,p.Designation;
            """;
        using var r=cmd.ExecuteReader(); var list=new List<(Lot,string,string)>();
        while(r.Read()) list.Add((new Lot{Id=r.GetInt64(0),ProduitId=r.GetInt64(1),NumeroLot=r.GetString(2),DatePeremption=DateTime.Parse(r.GetString(3)),QuantitePhysique=r.GetInt32(4),QuantiteVendable=r.GetInt32(5),CoutAchat=r.GetDecimal(6),IsBloque=r.GetBoolean(7)},r.GetString(8),r.GetString(9)));
        return list;
    }
}
