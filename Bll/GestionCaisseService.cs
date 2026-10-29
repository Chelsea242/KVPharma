using Microsoft.Data.Sqlite;
using Pharma.Dal;
using Pharma.Models;

namespace Pharma.Bll;

public sealed record ResumeCaisse(decimal Fond, decimal EspecesVentes, decimal Entrees, decimal Sorties, decimal Attendu, decimal? Compte, decimal? Ecart, decimal VentesCarte, decimal VentesMobile);

/// <summary>Gère les sessions de caisse et les mouvements d'espèces dans des transactions SQLite locales.</summary>
public sealed class GestionCaisseService
{
    private readonly string _cs;
    private readonly ActionLogRepository _logs;
    public GestionCaisseService(string connectionString) { _cs=connectionString; _logs=new ActionLogRepository(connectionString); }

    public SessionCaisse ObtenirOuOuvrir(long utilisateurId, decimal fondCaisse = 0m)
    {
        if (utilisateurId<=0 || fondCaisse<0) throw new ArgumentException("Session de caisse invalide.");
        using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();
        using var find=cn.CreateCommand();find.Transaction=tx;find.CommandText="SELECT Id,DateOuverture,UtilisateurId,FondCaisse,Statut FROM CaisseSessions WHERE Statut='Ouverte' ORDER BY Id DESC LIMIT 1";using var r=find.ExecuteReader();if(r.Read()){var s=LireSession(r);r.Close();tx.Commit();return s;}r.Close();
        using var insert=cn.CreateCommand();insert.Transaction=tx;insert.CommandText="INSERT INTO CaisseSessions(DateOuverture,UtilisateurId,FondCaisse,Statut) VALUES($date,$user,$fond,'Ouverte'); SELECT last_insert_rowid()";insert.Parameters.AddWithValue("$date",DateTime.Now.ToString("O"));insert.Parameters.AddWithValue("$user",utilisateurId);insert.Parameters.AddWithValue("$fond",fondCaisse);var id=Convert.ToInt64(insert.ExecuteScalar());_logs.Enregistrer(utilisateurId,$"Ouverture de la caisse {id}",cn,tx);tx.Commit();return new SessionCaisse{Id=id,DateOuverture=DateTime.Now,UtilisateurId=utilisateurId,FondCaisse=fondCaisse};
    }

    public void EnregistrerMouvement(long sessionId,long utilisateurId,decimal montant,string motif,TypeMouvementCaisse type)
    {
        if(sessionId<=0||utilisateurId<=0||montant<=0||string.IsNullOrWhiteSpace(motif))throw new ArgumentException("Mouvement de caisse invalide.");
        using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();VerifierSessionOuverte(cn,tx,sessionId);using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO MouvementsCaisse(SessionId,DateMouvement,UtilisateurId,Montant,Motif) VALUES($session,$date,$user,$montant,$motif)";c.Parameters.AddWithValue("$session",sessionId);c.Parameters.AddWithValue("$date",DateTime.Now.ToString("O"));c.Parameters.AddWithValue("$user",utilisateurId);c.Parameters.AddWithValue("$montant",type==TypeMouvementCaisse.Sortie?-montant:montant);c.Parameters.AddWithValue("$motif",motif.Trim());c.ExecuteNonQuery();_logs.Enregistrer(utilisateurId,$"Mouvement de caisse {type} {montant:N2} ({motif.Trim()})",cn,tx);tx.Commit();
    }

    public ResumeCaisse Calculer(long sessionId)
    {
        using var cn=new SqliteConnection(_cs);cn.Open();using var c=cn.CreateCommand();c.CommandText="""
            SELECT s.FondCaisse,
              COALESCE(SUM(CASE WHEN v.ModePaiement='Especes' AND v.Statut='Terminee' THEN v.Total ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN v.ModePaiement='Carte' AND v.Statut='Terminee' THEN v.Total ELSE 0 END),0),
              COALESCE(SUM(CASE WHEN v.ModePaiement='Mobile' AND v.Statut='Terminee' THEN v.Total ELSE 0 END),0),
              COALESCE((SELECT SUM(m.Montant) FROM MouvementsCaisse m WHERE m.SessionId=s.Id),0)
            FROM CaisseSessions s LEFT JOIN Ventes v ON v.SessionId=s.Id WHERE s.Id=$id GROUP BY s.Id,s.FondCaisse;
            """;c.Parameters.AddWithValue("$id",sessionId);using var r=c.ExecuteReader();if(!r.Read())throw new KeyNotFoundException("Session de caisse introuvable.");var fond=r.GetDecimal(0);var especes=r.GetDecimal(1);var carte=r.GetDecimal(2);var mobile=r.GetDecimal(3);var mouvements=r.GetDecimal(4);var attendu=fond+especes+mouvements;return new ResumeCaisse(fond,especes,mouvements>=0?mouvements:0,mouvements<0?-mouvements:0,attendu,null,null,carte,mobile);
    }

    public ResumeCaisse Cloturer(long sessionId,long utilisateurId,decimal montantCompte,string justificationEcart)
    {
        if(montantCompte<0)throw new ArgumentOutOfRangeException(nameof(montantCompte));var resume=Calculer(sessionId);var ecart=montantCompte-resume.Attendu;if(ecart!=0&&string.IsNullOrWhiteSpace(justificationEcart))throw new InvalidOperationException("Une justification est obligatoire lorsque la caisse présente un écart.");
        using var cn=new SqliteConnection(_cs);cn.Open();using var tx=cn.BeginTransaction();VerifierSessionOuverte(cn,tx,sessionId);using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="UPDATE CaisseSessions SET DateCloture=$date,TotalEspecesAttendu=$attendu,TotalEspecesCompte=$compte,Ecart=$ecart,Statut='Cloturee' WHERE Id=$id";c.Parameters.AddWithValue("$date",DateTime.Now.ToString("O"));c.Parameters.AddWithValue("$attendu",resume.Attendu);c.Parameters.AddWithValue("$compte",montantCompte);c.Parameters.AddWithValue("$ecart",ecart);c.Parameters.AddWithValue("$id",sessionId);c.ExecuteNonQuery();_logs.Enregistrer(utilisateurId,$"Clôture caisse {sessionId}, attendu {resume.Attendu:N2}, compté {montantCompte:N2}, écart {ecart:N2}. {justificationEcart.Trim()}",cn,tx);tx.Commit();return resume with {Compte=montantCompte,Ecart=ecart};
    }

    private static void VerifierSessionOuverte(SqliteConnection cn,SqliteTransaction tx,long id){using var c=cn.CreateCommand();c.Transaction=tx;c.CommandText="SELECT COUNT(*) FROM CaisseSessions WHERE Id=$id AND Statut='Ouverte'";c.Parameters.AddWithValue("$id",id);if(Convert.ToInt64(c.ExecuteScalar())!=1)throw new InvalidOperationException("La session de caisse est inexistante ou déjà clôturée.");}
    private static SessionCaisse LireSession(SqliteDataReader r)=>new(){Id=r.GetInt64(0),DateOuverture=DateTime.Parse(r.GetString(1)),UtilisateurId=r.GetInt64(2),FondCaisse=r.GetDecimal(3),Statut=Enum.Parse<StatutSessionCaisse>(r.GetString(4))};
}
