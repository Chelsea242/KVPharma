# Déploiement Pharma

## Pré-requis

- Windows 10/11 64 bits.
- Pour `Setup-Pharma.exe` : Inno Setup 6 installé sur la machine de construction.
- Aucun runtime .NET n'est nécessaire sur la machine cliente pour la version autonome.

## Générer les livrables

Depuis le dossier du projet :

```powershell
.\Deployment\publish.ps1
.\Deployment\build-installer.ps1
```

Les sorties sont placées dans `Livrable\Deployment` :

- `publish\Pharma.exe` : publication autonome ;
- `Pharma-1.0.0-win-x64-portable.zip` : version portable ;
- `installer\Setup-Pharma.exe` : installateur Windows, si Inno Setup 6 est disponible.

## Données et mises à jour

L'installation est placée dans `C:\Program Files\Pharma`. La base et les journaux sont stockés dans `%ProgramData%\Pharma`, jamais dans le répertoire d'installation. La variable d'environnement `PHARMA_DATA_DIR` permet de changer ce dossier.

Au démarrage, une copie de la base existante est créée dans `%ProgramData%\Pharma\Backups` avant l'application du schéma et des migrations. Une mise à jour ne remplace pas `pharma.db`.

La désinstallation retire les fichiers du programme mais conserve `%ProgramData%\Pharma` et les sauvegardes.

## Restauration

Utiliser la fonction de restauration de l'application avec un compte administrateur. Une copie de la base courante est créée avant le remplacement.

## Compte de démonstration

Sur une base neuve uniquement : `admin` / `admin`. Changez ce mot de passe avant toute utilisation réelle. Aucun mot de passe de production n'est inclus dans l'installateur.
