# Pharma - mini-pharmacie

Application WinForms .NET 8 utilisant `Microsoft.Data.Sqlite`.

## Licence

Ce projet est distribué sous licence MIT. Le texte complet se trouve dans le fichier [LICENSE](LICENSE).

## Déploiement Windows

Les scripts et la configuration de packaging sont dans `Deployment` : `publish.ps1` produit la publication autonome et le ZIP portable ; `build-installer.ps1` produit `Setup-Pharma.exe` si Inno Setup 6 est installé. La documentation complète est dans `Deployment/README_DEPLOIEMENT.md`.

## Organisation

- `Models` : POCO et énumérations métier.
- `Dal` : initialisation SQLite et accès aux produits.
- `Bll` : authentification, hachage PBKDF2-SHA256 et gestion FEFO du stock.
- `Ui` : formulaires WinForms et UserControls, sans requêtes SQL directes.
- `Database/schema.sql` : tables, contraintes, clés étrangères et index.

## Prérequis et démarrage

```powershell
dotnet restore
dotnet run
```

La base `pharma.db` est créée dans le dossier de l’application au premier lancement. Un compte administrateur est créé automatiquement si aucun administrateur n'existe :

- Identifiant : `admin`
- Mot de passe initial : `admin`

Changez ce mot de passe avant toute utilisation réelle. Ce compte de démonstration ne doit pas être conservé en production.

La validation d’une vente utilise une transaction SQLite locale unique. Chaque ligne vérifie atomiquement la quantité disponible, l’absence de blocage et la non-péremption du lot avant de décrémenter le stock et d’insérer le mouvement.

## Fonctions livrées

- Connexion par identifiant et mot de passe PBKDF2-SHA256 salé ; aucun écran de création de compte n'est proposé à l'utilisateur.
- Tableau de bord avec chiffre d'affaires journalier et alertes de stock/péremption.
- Catalogue filtrable par désignation/référence/code-barres/catégorie, état du stock et archivage ; CRUD responsable avec suppression physique refusée si un lot ou un historique existe.
- Caisse : lecture scanner clavier, tarif issu du catalogue (non éditable dans le panier), sélection par lots valides triés FEFO, contrôle de quantité, encaissement transactionnel, aperçu/impression et réimpression limitée aux tickets du vendeur connecté (responsable : tous les tickets).
- Lots/alertes et fournisseurs (CRUD/archivage responsable).
- Réceptions, corrections de stock, pertes, retours/annulations, journalisation et droits responsable.
- Clôture de caisse : fond, ventes nettes par mode de paiement, mouvements d'espèces, comptage et écart justifié.
- Rapports de ventes, produits les plus vendus, état de stock et péremptions ; export CSV et aperçu/impression des ventes.
- Paramètres pharmacie, sauvegarde quotidienne reportée au prochain démarrage, sauvegarde manuelle et restauration administrateur avec copie préalable.
- Runner d'acceptation intégré : `Pharma.exe --acceptance-tests`.

## Limites explicitement vérifiées

- L'impression et l'aperçu sont implémentés via WinForms ; l'export PDF natif n'est pas livré faute de bibliothèque PDF déjà retenue dans le projet.
- Les rapports autres que les ventes sont consultables dans la grille ; leur export CSV/PDF dédié doit encore être généralisé.
- La mesure de recherche est reproductible avec `--acceptance-tests` et affiche le matériel, le volume, le nombre d'itérations et le temps observé. Aucun seuil de 2 secondes n'est déclaré sans cette mesure.
- Pour restaurer, fermer les autres instances de l'application ; la base actuelle est copiée avant remplacement.

## Livraison et tests

Le dossier de livraison est généré séparément de la base utilisateur. Utiliser `dotnet restore`, `dotnet build -c Release` puis `Pharma.exe --acceptance-tests`. La commande `dotnet test` nécessite les paquets du SDK de test disponibles via NuGet ; dans un environnement hors réseau, le runner intégré reste la vérification exécutable de référence.
