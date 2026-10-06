# Jalon 2 — Écrire sur le disque sans rien casser

Ce jalon ajoute tout ce qui **modifie** des fichiers : la sauvegarde de la musique originale, l'installation d'une bande-son, la restauration, les contrôles préalables et la détection du jeu en cours d'exécution. Il n'y a toujours pas d'interface ; la suite compte maintenant 101 tests.

Une seule règle guide tout ce code : **quoi qu'il arrive, le `Bgm.awb` du jeu est soit l'ancien fichier complet, soit le nouveau fichier complet et vérifié. Jamais un fichier à moitié écrit, et jamais une musique originale perdue.**

## 1. Ce qu'on a construit

```text
src/KakarotOstManager/
├── Models/
│   ├── FileFingerprint.cs       empreinte + taille + date d'un fichier
│   ├── CheckResult.cs           un contrôle préalable et son résultat
│   ├── OperationResult.cs       le compte rendu d'une opération
│   ├── InstallProgress.cs       l'étape en cours et son avancement
│   └── InstalledState.cs        ce que contient réellement le jeu
└── Services/
    ├── AtomicFile.cs            écrire un petit fichier sans risque
    ├── ProcessProbe.cs          le jeu tourne-t-il ?
    ├── FileCopyService.cs       copier, vérifier, remplacer d'un coup
    ├── FileVerificationException.cs
    ├── FingerprintService.cs    mémoire des empreintes déjà calculées
    ├── BackupService.cs         sauvegarde et archives de l'original
    └── InstallService.cs        le chef d'orchestre
```

Essai en taille réelle, avec tes vrais fichiers de 418 Mo comme source et un **faux dossier de jeu temporaire** comme destination :

```text
Identifier (mémoire vide)            3,6 s  Unknown
Installer Namek (1re fois)           1,7 s  Success
   étapes : Analyzing > BackingUp > Copying > Verifying > Done
Identifier                           0,0 s  Soundtrack (03-namek)
Installer Cell                       1,1 s  Success
Réinstaller Cell (déjà en place)     0,0 s  Success
Restaurer l'original                 0,9 s  Success
Identifier                           0,0 s  Vanilla

Empreinte finale du faux jeu : 20A721B6E89D6D80…   (identique à ton backup\Bgm.awb)
Fichiers .tmp restants : 0
```

## 2. Comment se déroule une installation

Tout se passe dans `InstallAsync` de [InstallService.cs](../../src/KakarotOstManager/Services/InstallService.cs).

```text
1. Contrôles préalables ──── un seul échec ──▶ on s'arrête, rien n'est touché
        │
2. Analyse : quel fichier est en place dans le jeu ?
        │
        ├─ la sauvegarde de l'original ───▶ déjà à l'abri
        ├─ une bande-son du pack ─────────▶ rien à sauvegarder
        ├─ aucun fichier ─────────────────▶ rien à sauvegarder
        └─ un fichier inconnu ────────────▶ 3. le sauvegarder ── échec ──▶ on s'arrête
        │
4. Copie vers Bgm.awb.tmp, à côté du fichier du jeu
5. Relecture de la copie et comparaison des empreintes ── différence ──▶ on s'arrête
6. Renommage de Bgm.awb.tmp en Bgm.awb, d'un seul coup
7. Mémorisation de la bande-son active dans settings.json
```

### Les contrôles préalables

Ils reprennent le §13 du cahier des charges. Chacun produit un `CheckResult`, que l'interface pourra afficher sous forme de liste cochée.

| Contrôle | Ce qui est vérifié |
|---|---|
| `GameDirectory` | Le dossier du jeu est renseigné et existe. |
| `GameVersion` | Standard ou Remaster est choisi. |
| `BgmDirectory` | Le dossier BGM existe pour cette version. |
| `SoundtrackDirectory` | Le dossier du pack existe. |
| `SourceFile` | Le `Bgm.awb` à installer existe. |
| `GameNotRunning` | Le jeu n'est pas lancé. |
| `OriginalFileSafe` | La musique originale ne risque rien. |

### Reconnaître le fichier en place

C'est le point le plus délicat du cahier des charges. Celui-ci disait : « à la première utilisation, sauvegarder le `Bgm.awb` du jeu ». Mais si tu avais déjà installé Namek à la main avant d'utiliser l'application, elle aurait enregistré **Namek** sous le nom « musique originale ».

La méthode `ClassifyAsync` compare donc l'empreinte du fichier en place à celle de la sauvegarde et à celles du pack. Un fichier du pack n'est **jamais** sauvegardé comme original. Seul un fichier inconnu l'est : tant qu'aucune sauvegarde n'existe, c'est précisément le cas de la musique originale.

### Le remplacement en trois temps

`CopyVerifiedAsync` de [FileCopyService.cs](../../src/KakarotOstManager/Services/FileCopyService.cs) ne modifie jamais directement la destination :

```csharp
string tempFile = destinationFile + ".tmp";
try
{
    string sourceHash = await CopyAndHashAsync(sourceFile, tempFile, copyProgress, cancellationToken);
    string copyHash = await hashService.ComputeSha256Async(tempFile, verifyProgress, cancellationToken);
    if (copyHash != sourceHash)
    {
        throw new FileVerificationException(sourceFile, tempFile);
    }

    File.Move(tempFile, destinationFile, overwrite: true);
    return sourceHash;
}
catch
{
    TryDelete(tempFile);
    throw;
}
```

Pendant toute la copie, qui est l'étape longue, le vrai `Bgm.awb` n'est pas touché. Le renommage final est **atomique** : Windows le réalise en une seule opération indivisible. Une coupure de courant à n'importe quel instant laisse donc un fichier de jeu utilisable.

Le jeu offre une protection supplémentaire sans le savoir : tant qu'il tourne, il garde `Bgm.awb` ouvert, et Windows refuse alors le renommage. Un test reproduit exactement ce cas.

### La mémoire des empreintes

Calculer l'empreinte de dix fichiers de 418 Mo prend quelques secondes. [FingerprintService.cs](../../src/KakarotOstManager/Services/FingerprintService.cs) enregistre donc chaque résultat dans `fingerprints.json`, avec la taille et la date de modification du fichier. Tant que celles-ci n'ont pas changé, l'empreinte est réutilisée. C'est ce qui fait passer « Identifier » de 3,6 s à 0,0 s dans l'essai ci-dessus.

### Les quatre règles de sauvegarde

| Fichier en place dans le jeu | Action avant d'installer |
|---|---|
| Identique à la sauvegarde | Aucune : l'original est déjà à l'abri. |
| Identique à une bande-son du pack | Aucune : ce n'est pas l'original, et le pack permet de le retrouver. |
| Inconnu, aucune sauvegarde | Sauvegarde obligatoire. Si elle échoue, l'installation est annulée. |
| Inconnu, une sauvegarde différente existe | L'ancienne sauvegarde est archivée sous un nom daté, le fichier en place devient la nouvelle sauvegarde. C'est le cas d'une mise à jour du jeu. |

Les fichiers sont rangés dans `%LOCALAPPDATA%\KakarotAnimeOstManager\Backups\` :

```text
Backups/
├── Vanilla/Standard/Bgm.awb                    la musique originale
└── Archive/Standard/Bgm.2026-10-06_18-30-00.awb   les fichiers mis de côté
```

## 3. Les notions à retenir

### Les flux de fichiers

Un **flux** (`FileStream`) lit ou écrit un fichier morceau par morceau. C'est indispensable pour un fichier de 418 Mo, qu'on ne veut pas charger en mémoire :

```csharp
byte[] buffer = new byte[BufferSize];           // un bloc réutilisé de 1 Mo
int bytesRead;
while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
{
    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
    hash.AppendData(buffer, 0, bytesRead);
}
```

`ReadAsync` remplit le bloc et renvoie le nombre d'octets lus ; zéro signifie la fin du fichier. La même boucle écrit la copie et alimente le calcul d'empreinte : le fichier source n'est lu qu'une fois.

### `try`, `catch`, `finally`, `throw`

- `try` entoure le code qui peut échouer.
- `catch` s'exécute si une exception survient.
- `finally` s'exécute dans tous les cas, erreur ou non. Il sert à libérer une ressource.
- `throw;` seul, dans un `catch`, relance la même exception après avoir fait le ménage.

Dans l'extrait de `CopyVerifiedAsync` ci-dessus, le `catch` supprime le fichier provisoire puis relance l'erreur : celui qui a appelé la méthode doit savoir que la copie a échoué.

### Créer sa propre exception

[FileVerificationException.cs](../../src/KakarotOstManager/Services/FileVerificationException.cs) hérite de `IOException` :

```csharp
public sealed class FileVerificationException(string sourceFile, string copiedFile)
    : IOException($"La copie « {copiedFile} » ne correspond pas à « {sourceFile} ».")
```

Le code qui intercepte `IOException` intercepte donc aussi celle-ci, tout en pouvant la distinguer quand il le souhaite. Le `$"…{variable}…"` est une **chaîne interpolée** : les expressions entre accolades sont remplacées par leur valeur.

### Renvoyer un résultat plutôt que lancer une exception

Les services de bas niveau lancent des exceptions. `InstallService`, lui, les **traduit** en un `OperationResult` :

```csharp
private static OperationStatus? ToFailureStatus(Exception exception) => exception switch
{
    OperationCanceledException => OperationStatus.Cancelled,
    FileVerificationException => OperationStatus.VerificationFailed,
    IOException or UnauthorizedAccessException => OperationStatus.CopyFailed,
    _ => null,
};
```

Un jeu lancé ou un dossier protégé ne sont pas des accidents : ce sont des situations prévues, que l'interface doit expliquer à l'utilisateur. Un statut se prête mieux à cela qu'une exception. L'ordre des cas compte : `FileVerificationException` étant aussi une `IOException`, elle doit être testée en premier.

Le filtre `catch (Exception ex) when (ToFailureStatus(ex) is { } status)` se lit : « intercepte l'erreur seulement si elle correspond à un statut connu, et nomme ce statut `status` ». Une erreur imprévue, signe d'un bogue, continue de remonter.

### Interface et injection de dépendances

Comment tester « le jeu est lancé » sans lancer le jeu ? En ne dépendant pas directement de Windows, mais d'un **contrat**. Une `interface` décrit ce qu'un objet sait faire, sans dire comment. Voir [ProcessProbe.cs](../../src/KakarotOstManager/Services/ProcessProbe.cs) :

```csharp
public interface IProcessProbe
{
    bool IsRunning(string processName);
}
```

Il en existe deux réalisations. `SystemProcessProbe` interroge réellement Windows. `FakeProcessProbe`, dans les tests, répond ce qu'on lui demande de répondre. `GameService` reçoit l'une ou l'autre sans faire de différence :

```csharp
public sealed class GameService(IProcessProbe? processProbe = null)
{
    private readonly IProcessProbe _processProbe = processProbe ?? new SystemProcessProbe();

    public bool IsKakarotRunning() => _processProbe.IsRunning(GameProcessName);
}
```

Par convention, le nom d'une interface commence par `I`, et celui d'un champ privé par `_`.

### `virtual` et `override`

Autre façon de remplacer un comportement dans un test : une méthode marquée `virtual` peut être **redéfinie** par une classe qui hérite, avec `override`. C'est ainsi qu'un test simule un disque défaillant, en renvoyant une fausse empreinte pour le fichier fraîchement copié :

```csharp
private sealed class CorruptedCopyHashService : HashService
{
    public override Task<string> ComputeSha256Async(string filePath, …) =>
        filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult(new string('0', 64))
            : base.ComputeSha256Async(filePath, progress, cancellationToken);
}
```

`base.` appelle la version d'origine de la méthode.

### `IProgress<T>` : rendre compte de l'avancement

Une opération longue reçoit un objet `IProgress<T>` et appelle sa méthode `Report` au fil de l'eau. Elle ignore totalement ce qui en sera fait : une barre de progression dans l'interface, une liste dans un test. Ici, `T` est `InstallProgress`, qui porte l'étape en cours et sa part accomplie de 0 à 1.

### `CancellationToken` : pouvoir annuler

Un `CancellationToken` est un signal d'arrêt transmis de méthode en méthode jusqu'aux lectures et écritures de fichiers. Quand l'annulation est demandée, l'opération en cours lance une `OperationCanceledException`, le fichier provisoire est supprimé, et l'installation renvoie le statut `Cancelled`.

## 4. Les tests

| Fichier de tests | Ce qu'il garantit |
|---|---|
| [InstallServiceTests.cs](../../tests/KakarotOstManager.Tests/InstallServiceTests.cs) | Installation, changement, restauration, identification, et tous les cas d'échec. |
| [FingerprintServiceTests.cs](../../tests/KakarotOstManager.Tests/FingerprintServiceTests.cs) | Une empreinte n'est calculée qu'une fois, et recalculée si le fichier change. |
| [FileCopyAndBackupTests.cs](../../tests/KakarotOstManager.Tests/FileCopyAndBackupTests.cs) | Copie vérifiée, archivage, retour en arrière si une sauvegarde échoue, détection du processus. |

[TestEnvironment.cs](../../tests/KakarotOstManager.Tests/TestEnvironment.cs) fabrique pour chaque test un bac à sable complet : un faux jeu, un faux pack de trois bandes-son et un dossier de données. Comme les vrais `Bgm.awb`, les faux fichiers ont tous la même taille.

Les cas d'échec sont provoqués pour de bon, pas simulés par un simple drapeau :

| Situation | Comment le test la provoque | Résultat exigé |
|---|---|---|
| Jeu lancé | `FakeProcessProbe` déclare le processus actif. | `PrecheckFailed`, rien n'est modifié. |
| Sauvegarde impossible | Un fichier occupe la place du dossier `Backups`. | `BackupFailed`, l'original n'est pas écrasé. |
| Fichier du jeu verrouillé | Le test garde le fichier ouvert, comme le ferait le jeu. | `CopyFailed`, fichier intact, aucun `.tmp` oublié. |
| Copie corrompue | La relecture renvoie une fausse empreinte. | `VerificationFailed`, fichier intact. |
| Annulation | Le signal d'annulation est déjà levé. | `Cancelled`, fichier intact. |

## 5. Exercices facultatifs

1. **Retirer une sécurité.** Dans `FileCopyService.cs`, mets en commentaire le bloc `if (copyHash != sourceHash) { … }`. Quels tests échouent ? Ce sont eux qui protègent cette règle.
2. **Suivre une installation pas à pas.** Place un point d'arrêt sur la première ligne de `InstallAsync`, puis lance en mode débogage le test `La_premiere_installation_sauvegarde_l_original_puis_installe_la_bande_son`. Avance ligne par ligne et observe la variable `current`.
3. **Ajouter un contrôle.** Ajoute une valeur `EnoughDiskSpace` à `CheckId`, et vérifie dans `InstallAsync` que le disque du jeu dispose d'au moins la taille du fichier source. Indice : `new DriveInfo(Path.GetPathRoot(gameFile)!).AvailableFreeSpace`.

Annule tes essais avec `git restore .` avant de continuer.

## 6. Écarts par rapport au plan et points ouverts

- **Un `InstallService` distinct.** Le cahier des charges plaçait `InstallSoundtrack()` dans `SoundtrackService` et `RestoreVanillaBgm()` dans `BackupService`. Ces deux opérations mobilisent cinq services chacune ; les réunir dans un chef d'orchestre garde les autres services simples.
- **Un cas ajouté : restaurer par-dessus un fichier inconnu.** Le plan ne le prévoyait pas. Le fichier est archivé avant d'être écrasé, par cohérence avec la règle « ne jamais perdre un fichier que l'application ne reconnaît pas ».
- **Le nom du processus du jeu, `AT-Win64-Shipping`, n'est pas vérifié.** Il découle des conventions d'Unreal Engine. À confirmer dès que le jeu sera réinstallé. Si le nom était faux, le verrouillage du fichier par le jeu empêcherait tout de même le remplacement.

## 7. La suite : jalon 3

Le jalon 3 construit enfin l'interface : choix des dossiers, liste des bandes-son, panneau de détail, boutons Appliquer et Restaurer, messages de succès et d'erreur, en français et en anglais.
