# La première release : un seul `.exe` à télécharger

Ce document explique comment l'application passe du code source à un fichier que n'importe qui peut télécharger et lancer, et la modification faite pour que ce fichier soit **unique**.

## 1. Ce qui a changé dans le code

Jusqu'ici, la publication produisait deux éléments indissociables : `KakarotOstManager.exe` et un dossier `Data` contenant `soundtracks.json`. Il fallait donc les distribuer dans un zip.

Le catalogue est maintenant **intégré dans l'exécutable**. La publication ne produit plus qu'un fichier de 62 Mo, qui fonctionne posé seul dans un dossier vide.

| Fichier | Modification |
|---|---|
| [KakarotOstManager.csproj](../../src/KakarotOstManager/KakarotOstManager.csproj) | `soundtracks.json` devient une ressource intégrée. |
| [SoundtrackCatalogLoader.cs](../../src/KakarotOstManager/Services/SoundtrackCatalogLoader.cs) | Lit le catalogue intégré, sauf si un fichier de remplacement existe. |
| [App.xaml.cs](../../src/KakarotOstManager/App.xaml.cs) | Appelle `LoadDefault()`. |

La suite compte 170 tests.

## 2. Les notions à retenir

### Fichier copié ou ressource intégrée

Un projet .NET peut embarquer un fichier de données de deux façons, selon ce qu'on écrit dans le `.csproj`.

| | Fichier copié | Ressource intégrée |
|---|---|---|
| Déclaration | `<None Update="…" CopyToOutputDirectory="PreserveNewest" />` | `<EmbeddedResource Include="…" />` |
| Où se trouve la donnée | Dans un fichier à côté de l'exécutable. | À l'intérieur de l'exécutable. |
| Modifiable par l'utilisateur | Oui, avec un éditeur de texte. | Non. |
| Peut se perdre | Oui, si on déplace l'exe sans lui. | Non. |

Notre déclaration :

```xml
<None Remove="Data\soundtracks.json" />
<EmbeddedResource Include="Data\soundtracks.json" LogicalName="KakarotOstManager.Data.soundtracks.json" />
```

`LogicalName` fixe le nom sous lequel le code retrouvera la ressource.

### Lire une ressource intégrée

Une ressource ne se lit pas avec `File.ReadAllText`, puisque ce n'est pas un fichier sur le disque. On la demande à l'**assembly**, c'est-à-dire au programme compilé lui-même, qui renvoie un flux :

```csharp
using Stream stream = typeof(SoundtrackCatalogLoader).Assembly.GetManifestResourceStream(EmbeddedResourceName)
    ?? throw new InvalidOperationException($"Ressource intégrée introuvable : {EmbeddedResourceName}");

return JsonSerializer.Deserialize<SoundtrackCatalog>(stream, JsonDefaults.Options) ?? new SoundtrackCatalog();
```

`GetManifestResourceStream` renvoie `null` si le nom est faux. Le `?? throw` transforme ce cas en erreur explicite, au lieu de laisser le programme échouer plus loin de façon obscure.

### Garder la possibilité de modifier le catalogue

Le cahier des charges (§7) voulait qu'on puisse changer les indications sans toucher au programme. Pour ne pas perdre cela, le chargement se fait en deux temps :

```csharp
public static SoundtrackCatalog LoadWithOverride(string overrideFile) =>
    File.Exists(overrideFile) ? Load(overrideFile) : LoadEmbedded();
```

Si un fichier `Data\soundtracks.json` existe à côté de l'exécutable, il **remplace** le catalogue intégré. Sinon, le catalogue intégré sert. L'utilisateur ordinaire n'a qu'un fichier ; l'utilisateur avancé garde la main.

## 3. Fabriquer l'exécutable

```text
dotnet publish src/KakarotOstManager -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

| Option | Effet |
|---|---|
| `-c Release` | Compile en mode optimisé, et non en mode débogage. |
| `-r win-x64` | Cible Windows 64 bits. |
| `--self-contained true` | Embarque .NET : l'utilisateur n'a rien à installer. C'est ce qui explique les 62 Mo. |
| `-p:PublishSingleFile=true` | Regroupe tout en un seul `.exe`, y compris les textes français. |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | Y inclut aussi les bibliothèques natives de WPF. |
| `-p:EnableCompressionInSingleFile=true` | Compresse le contenu pour réduire la taille. |
| `-p:DebugType=none` | N'ajoute pas le fichier de symboles de débogage. |
| `-o publish` | Dossier de sortie. |

L'autre mode, « dépendant du framework » (`--self-contained false`), donnerait un fichier d'environ 1 Mo, mais chaque utilisateur devrait d'abord installer .NET 10. Pour un outil destiné à des joueurs, l'exécutable autonome est le bon choix.

Le dossier `publish` est ignoré par git (voir `.gitignore`) : **on ne met jamais un exécutable compilé dans le dépôt**. Le dépôt contient les sources, la release contient le résultat.

## 4. Tag, release, numéro de version

- Un **tag** est une étiquette posée sur un commit précis, par exemple `v0.1.0`. Il permet de retrouver exactement le code d'une version.
- Une **release** GitHub est une page attachée à un tag, avec une description et des fichiers à télécharger.

Le numéro suit le versionnage sémantique, `MAJEUR.MINEUR.CORRECTIF` :

| Nombre | Il augmente quand… |
|---|---|
| Correctif | on corrige un bogue sans rien ajouter (`0.1.0` → `0.1.1`). |
| Mineur | on ajoute une fonction (`0.1.1` → `0.2.0`). |
| Majeur | on casse la compatibilité, ou on déclare le logiciel stable (`0.9.0` → `1.0.0`). |

Un `0` en tête signale un logiciel encore jeune. Le numéro est déclaré dans le `.csproj` (`<Version>0.1.0</Version>`) et se retrouve dans les propriétés de l'exécutable.

Le fichier déposé s'appelle `KakarotAnimeOstManager.exe`, sans numéro de version. GitHub fournit alors une adresse permanente vers la dernière version :

```text
https://github.com/Zakkios/Kakarot-Anime-OST-Manager/releases/latest/download/KakarotAnimeOstManager.exe
```

## 5. Deux choses à savoir pour les utilisateurs

- **SmartScreen.** L'exécutable n'est pas signé numériquement, ce qui demande un certificat payant. Windows affiche donc « Windows a protégé votre ordinateur » au premier lancement. Il faut cliquer sur « Informations complémentaires » puis « Exécuter quand même ».
- **Pré-version.** Tant que l'outil n'a pas été essayé sur un jeu réellement installé, la release est marquée « Pre-release ».

## 6. Un piège évité : `git add -A`

Un fichier `publish.rar` de 117 Mo s'est retrouvé à la racine du projet pendant la préparation de la release. La commande `git add -A`, qui ajoute **tout** ce qui est nouveau, l'aurait inclus dans le commit. GitHub refuse les fichiers de plus de 100 Mo, et retirer un gros fichier d'un historique git est pénible.

Deux réflexes : regarder `git status` avant de commiter, et ajouter les fichiers par leur nom quand le dossier contient autre chose que du code.

## 7. Exercices facultatifs

1. **Voir la ressource.** Ajoute temporairement, dans un test, `typeof(App).Assembly.GetManifestResourceNames()` et affiche le résultat. Tu y verras `KakarotOstManager.Data.soundtracks.json`, ainsi que les ressources générées pour le XAML et les textes.
2. **Essayer le remplacement.** Lance la publication, pose à côté de l'exécutable un dossier `Data` contenant un `soundtracks.json` réduit à une seule entrée, et relance : seule cette bande-son garde son nom, les autres passent dans « Autres ».
3. **Comparer les deux modes.** Relance la publication avec `--self-contained false` dans un autre dossier de sortie, et compare la taille des deux exécutables.
