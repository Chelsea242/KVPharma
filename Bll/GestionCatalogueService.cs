using Pharma.Dal;
using Pharma.Models;

namespace Pharma.Bll;

/// <summary>Façade métier du catalogue : contrôle les droits avant chaque commande d'écriture.</summary>
public sealed class GestionCatalogueService
{
    private readonly ProduitRepository _lecture;
    private readonly ProduitCrudRepository _ecriture;
    private readonly FournisseurRepository _fournisseurs;
    private readonly string _cs;

    public GestionCatalogueService(string connectionString)
    {
        _cs = connectionString;
        _lecture = new ProduitRepository(connectionString);
        _ecriture = new ProduitCrudRepository(connectionString);
        _fournisseurs = new FournisseurRepository(connectionString);
    }

    public IReadOnlyList<(Produit Produit, int StockPhysique, int StockVendable)> RechercherProduits(string texte, string? categorie, bool archives, RoleUtilisateur role)
    {
        if (archives && role != RoleUtilisateur.Administrateur) throw new UnauthorizedAccessException("Seul un responsable peut consulter les produits archivés.");
        return _lecture.ListerCatalogue(texte, categorie, archives);
    }

    public IReadOnlyList<string> Categories() => _ecriture.ListerCategories();
    public IReadOnlyList<(Produit Produit, Lot Lot)> RechercherPourVente(string texte) => _lecture.Rechercher(texte);
    public IReadOnlyList<LotStockVue> Lots(Utilisateur demandeur)
    {
        ExigerResponsable(demandeur);
        return new LotRepository(_cs).Lister().Select(x => new LotStockVue(x.Lot.Id,x.Reference,x.Designation,x.Lot.NumeroLot,x.Lot.DatePeremption,x.Lot.QuantitePhysique,x.Lot.QuantiteVendable,x.Lot.IsBloque,x.Lot.CoutAchat)).ToList();
    }
    public IReadOnlyList<Produit> Produits(RoleUtilisateur role, bool archives = false)
    {
        if (archives && role != RoleUtilisateur.Administrateur) throw new UnauthorizedAccessException("Accès réservé au responsable.");
        return _ecriture.Lister(archives);
    }

    public long AjouterProduit(Produit produit, Utilisateur auteur) { ExigerResponsable(auteur); return _ecriture.Ajouter(produit, auteur.Id); }
    public void ModifierProduit(Produit produit, Utilisateur auteur) { ExigerResponsable(auteur); _ecriture.Modifier(produit, auteur.Id); }
    public void ArchiverProduit(long id, Utilisateur auteur) { ExigerResponsable(auteur); _ecriture.Archiver(id, auteur.Id); }
    public bool SupprimerProduitSansHistorique(long id, Utilisateur auteur) { ExigerResponsable(auteur); return _ecriture.SupprimerSiSansHistorique(id, auteur.Id); }
    public IReadOnlyList<Fournisseur> Fournisseurs(Utilisateur auteur) { ExigerResponsable(auteur); return _fournisseurs.Lister(); }
    public long AjouterFournisseur(Fournisseur fournisseur, Utilisateur auteur) { ExigerResponsable(auteur); return _fournisseurs.Ajouter(fournisseur, auteur.Id); }
    public void ModifierFournisseur(Fournisseur fournisseur, Utilisateur auteur) { ExigerResponsable(auteur); _fournisseurs.Modifier(fournisseur, auteur.Id); }
    public void ArchiverFournisseur(long id, Utilisateur auteur) { ExigerResponsable(auteur); _fournisseurs.Archiver(id, auteur.Id); }

    private static void ExigerResponsable(Utilisateur auteur)
    {
        ArgumentNullException.ThrowIfNull(auteur);
        if (auteur.Role != RoleUtilisateur.Administrateur) throw new UnauthorizedAccessException("Cette opération est réservée au responsable.");
    }
}

public sealed record LotStockVue(long Id,string Reference,string Designation,string NumeroLot,DateTime DatePeremption,int QuantitePhysique,int QuantiteVendable,bool IsBloque,decimal CoutAchat);
