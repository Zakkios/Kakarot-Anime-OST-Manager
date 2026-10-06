# Jalon 1 — Le cœur du logiciel, en lecture seule

Ce jalon écrit toute la logique qui **observe** le disque sans jamais le modifier : où se trouve le dossier BGM du jeu, quelles bandes-son contient le pack, quelle est l'empreinte d'un fichier, quels paramètres ont été mémorisés. Il n'y a toujours pas d'interface : ce code est exercé par 54 tests automatiques.

Séparer la lecture de l'écriture est volontaire. Tout ce qui est dangereux (écraser `Bgm.awb`) arrive au jalon 2 et s'appuiera sur des briques déjà vérifiées.

## 1. Ce qu'on a construit

```text
src/KakarotOstManager/
├── Models/                      les données, sans logique
│   ├── GameVersion.cs           Standard ou Remaster
│   ├── SoundtrackCategory.cs    Main, Dlc ou Other
│   ├── LocalizedText.cs         un texte en français et en anglais
│   ├── SoundtrackCatalog.cs     le contenu de soundtracks.json
│   ├── Soundtrack.cs            une bande-son trouvée sur le disque
│   └── AppSettings.cs           les paramètres mémorisés
├── Services/                    la logique
│   ├── AppPaths.cs              où l'application range ses propres fichiers
│   ├── JsonDefaults.cs          réglages JSON communs
│   ├── SettingsService.cs       lire et écrire settings.json
│   ├── GameService.cs           dossier BGM, version, validation du jeu
│   ├── HashService.cs           empreinte SHA-256 d'un fichier
│   ├── SoundtrackCatalogLoader.cs   charger soundtracks.json
│   └── SoundtrackService.cs     scanner le pack, précédente / suivante
└── Data/soundtracks.json        les 9 chapitres de l'histoire
```

Vérification sur tes vrais dossiers, en lecture seule :

```text
Version détectée : Standard
Dossier BGM      : D:\SteamLibrary\steamapps\common\DRAGON BALL Z KAKAROT\AT\Content\Sound\Bgm
Bgm.awb du jeu   : absent                      (le jeu est désinstallé)

10 bandes-son trouvées :
  [Main ] 01   Raditz      préc=-             suiv=02-saiyans
  [Main ] 02   Saiyans     préc=01-raditz     suiv=02.5-vegeta
  [Main ] 02.5 Vegeta      préc=02-saiyans    suiv=03-namek
  [Main ] 03   Namek       préc=02.5-vegeta   suiv=04-androids
  …
  [Main ] 07.5 Post Game   préc=07-kid-buu    suiv=-
  [Other]      backup      préc=-             suiv=-

SHA-256 Namek : 322F3BDD6ACDAABB… en 0,3 s     (identique au calcul fait par Windows)
```

## 2. Les notions à retenir

### `enum` : une liste fermée de valeurs

Quand une donnée ne peut prendre que quelques valeurs connues, on la déclare en `enum` plutôt qu'en texte. Le compilateur refuse alors toute valeur inventée. Voir [GameVersion.cs](../../src/KakarotOstManager/Models/GameVersion.cs) :

```csharp
public enum GameVersion
{
    Standard,
    Remaster,
}
```

### `class` et `record` : deux façons de décrire un objet

- Une **`class`** est l'objet classique : ses données peuvent changer au fil du temps. [AppSettings](../../src/KakarotOstManager/Models/AppSettings.cs) en est une, car on modifie les paramètres pendant que l'application tourne.
- Un **`record`** est un objet fait pour **porter des données qui ne changent plus** une fois créées. [Soundtrack](../../src/KakarotOstManager/Models/Soundtrack.cs) en est un : une bande-son trouvée sur le disque est un constat, pas quelque chose qu'on édite. En prime, deux records ayant les mêmes valeurs sont considérés comme égaux.

`sealed` devant `class` ou `record` interdit d'en hériter. C'est une bonne habitude par défaut : on n'ouvre l'héritage que lorsqu'on en a besoin.

### Les propriétés

Une **propriété** est une donnée exposée par un objet. Sa déclaration précise qui peut la lire et l'écrire :

```csharp
public string? GamePath { get; set; }          // lisible et modifiable à tout moment
public required string Id { get; init; }       // obligatoire, fixée à la création, puis figée
public string FolderName => Path.GetFileName(FolderPath);   // calculée à chaque lecture
```

- `get; set;` : lecture et écriture libres.
- `get; init;` : écriture permise uniquement à la création de l'objet.
- `required` : le compilateur refuse de créer l'objet si cette propriété n'est pas renseignée.
- `=>` : la propriété ne stocke rien, elle calcule sa valeur.

La création d'un objet se fait avec un **initialiseur** :

```csharp
new Soundtrack
{
    Id = "03-namek",
    FolderPath = folder,
    BgmFilePath = bgmFile,
    Name = metadata.Name,
    Category = SoundtrackCategory.Main,
}
```

### La nullabilité : `string` contre `string?`

C# distingue une valeur **toujours présente** d'une valeur **peut-être absente** :

- `string` : il y a forcément un texte.
- `string?` : il peut ne rien y avoir (`null`).

Le compilateur suit cette information à la trace et avertit si l'on utilise une valeur peut-être absente sans avoir vérifié. Quelques opérateurs reviennent sans cesse :

| Écriture | Signification |
|---|---|
| `x is not null` | Teste que `x` est présent. |
| `x ?? y` | Vaut `x` s'il est présent, sinon `y`. |
| `x?.Id` | Vaut `x.Id` si `x` est présent, sinon `null`. |
| `x!` | « Fais-moi confiance, `x` n'est pas `null` ici. » À utiliser avec parcimonie. |

Exemple dans [LocalizedText.cs](../../src/KakarotOstManager/Models/LocalizedText.cs), qui renvoie la langue demandée ou, à défaut, l'autre :

```csharp
return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback ?? "";
```

`condition ? a : b` est l'**opérateur ternaire** : il vaut `a` si la condition est vraie, `b` sinon.

### `using` et l'exception `System.IO`

Une directive `using` en tête de fichier rend un espace de noms accessible sans le préfixer. Grâce à `ImplicitUsings`, la plupart sont ajoutés automatiquement, à une exception près : dans un projet WPF, **`System.IO` n'est pas importé d'office**. C'est pourquoi les fichiers qui manipulent des chemins commencent par `using System.IO;`.

### Les expressions `switch`

Elles associent une valeur à un résultat, sans cascade de `if`. Voici comment [GameService.cs](../../src/KakarotOstManager/Services/GameService.cs) calcule le dossier BGM :

```csharp
public string GetBgmDirectory(string gamePath, GameVersion version) => version switch
{
    GameVersion.Standard => Path.Combine(gamePath, "AT", "Content", "Sound", "Bgm"),
    GameVersion.Remaster => Path.Combine(gamePath, "dlc", "Remaster", "AT", "Content", "Sound", "Bgm"),
    _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
};
```

Le cas `_` signifie « tout le reste ». `Path.Combine` assemble des morceaux de chemin en plaçant lui-même les `\` : on ne concatène jamais des chemins à la main.

### LINQ : interroger une collection

**LINQ** permet d'enchaîner des opérations sur une liste. Chaque opération reçoit une **lambda**, c'est-à-dire une petite fonction écrite sur place sous la forme `paramètre => résultat`. Extrait de [SoundtrackService.cs](../../src/KakarotOstManager/Services/SoundtrackService.cs) :

```csharp
return soundtracks
    .OrderBy(soundtrack => soundtrack.Category)
    .ThenBy(soundtrack => soundtrack.Order)
    .ThenBy(soundtrack => soundtrack.FolderName, StringComparer.OrdinalIgnoreCase)
    .ToList();
```

Les opérations les plus courantes :

| Opération | Rôle |
|---|---|
| `Where(x => condition)` | Garde les éléments qui satisfont la condition. |
| `Select(x => valeur)` | Transforme chaque élément. |
| `OrderBy` / `ThenBy` | Trie, puis départage les égalités. |
| `Single(x => condition)` | Renvoie l'unique élément correspondant, et échoue s'il y en a zéro ou plusieurs. |
| `ToList()` | Exécute la chaîne et range le résultat dans une liste. |

Une chaîne LINQ est **paresseuse** : rien n'est calculé tant qu'on ne parcourt pas le résultat, par exemple avec `ToList()`.

### JSON avec `System.Text.Json`

Deux appels suffisent pour passer d'un objet à du texte JSON et inversement. Dans [SettingsService.cs](../../src/KakarotOstManager/Services/SettingsService.cs) :

```csharp
string json = JsonSerializer.Serialize(settings, JsonDefaults.Options);
AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.Options);
```

`Deserialize<AppSettings>` est une méthode **générique** : le type entre chevrons indique ce qu'on veut obtenir. Les réglages partagés de [JsonDefaults.cs](../../src/KakarotOstManager/Services/JsonDefaults.cs) donnent un fichier agréable à lire :

```json
{
  "gamePath": "D:\\SteamLibrary\\steamapps\\common\\DRAGON BALL Z KAKAROT",
  "soundtrackPath": "D:\\Mods\\Bande-son animé",
  "gameVersion": "remaster",
  "currentSoundtrackId": "03-namek"
}
```

### Les exceptions et `catch … when`

Une erreur à l'exécution se manifeste par une **exception**. On l'intercepte avec `try / catch`. Le filtre `when` précise quelles erreurs on accepte de traiter ; toutes les autres continuent de remonter :

```csharp
catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
{
    return new AppSettings();
}
```

Ici, un `settings.json` abîmé ne doit pas empêcher l'application de démarrer : on repart de paramètres vides.

### Les constructeurs primaires

Depuis C# 12, les paramètres du constructeur peuvent s'écrire directement après le nom de la classe et servir dans toutes ses méthodes :

```csharp
public sealed class SettingsService(AppPaths paths)
{
    public AppSettings Load()
    {
        if (!File.Exists(paths.SettingsFile)) { … }
    }
}
```

`SettingsService` ne décide pas elle-même où se trouve `settings.json` : on le lui fournit. C'est ce qui permet aux tests de la faire travailler dans un dossier temporaire. Cette technique s'appelle l'**injection de dépendances**.

### `async` / `await`

Lire un fichier de 400 Mo prend du temps. Si ce travail bloquait le programme, la fenêtre se figerait. Une méthode **asynchrone** rend la main pendant les attentes :

```csharp
public async Task<string> ComputeSha256Async(string filePath, …)
{
    …
    while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
    {
        hash.AppendData(buffer, 0, bytesRead);
    }
    return Convert.ToHexString(hash.GetHashAndReset());
}
```

- `async` autorise l'usage de `await` dans la méthode.
- `Task<string>` signifie « un `string` qui sera disponible plus tard ».
- `await` attend le résultat sans bloquer le reste du programme.
- Par convention, le nom d'une méthode asynchrone se termine par `Async`.

`using var` et `await using var` garantissent qu'une ressource, ici le fichier ouvert, est refermée à la sortie de la méthode, même en cas d'erreur.

### Les expressions régulières

Une **expression régulière** décrit une forme de texte. Chaque bande-son de [soundtracks.json](../../src/KakarotOstManager/Data/soundtracks.json) en possède une dans son champ `match` :

```json
"match": "02\\.5\\s*-\\s*Vegeta"
```

| Morceau | Signification |
|---|---|
| `02` | Les caractères « 02 ». |
| `\.` | Un vrai point (sans la barre oblique, `.` signifie « n'importe quel caractère »). |
| `\s*` | Zéro, un ou plusieurs espaces. |
| `-` | Un tiret. |

En JSON chaque `\` doit être doublé, d'où `\\.` et `\\s`. Ce motif reconnaît aussi bien `02.5 - Vegeta` que `DBZ Kakarot Soundtrack JPN Mod 02.5 - Vegeta`, et ne confond pas `02.5 - Vegeta` avec `02 - Saiyans`.

## 3. Visite guidée du code

### Le scan du pack

La méthode `FindSoundtracks` de [SoundtrackService.cs](../../src/KakarotOstManager/Services/SoundtrackService.cs) procède en quatre temps.

1. **Chercher tous les `Bgm.awb`** sous le dossier du pack, jusqu'à trois niveaux de profondeur. Cela couvre le cas d'une archive extraite dans un dossier du même nom.
2. **Écarter celui qui est posé à la racine.** Ton pack est rangé dans le dossier BGM du jeu : le `Bgm.awb` de la racine est celui que le jeu utilise, pas une bande-son du pack.
3. **Rattacher chaque dossier à ses métadonnées** en testant les motifs de `soundtracks.json`. Un dossier sans correspondance n'est pas rejeté : il est rangé dans la rubrique « Autres » sous son propre nom. C'est ce qui arrive à ton dossier `backup`, et c'est ce qui arriverait à un futur `17 - Nouveau DLC`.
4. **Trier** par rubrique, puis par ordre.

### Précédente et suivante

Le cahier des charges prévoyait d'écrire « précédente » et « suivante » en dur dans le JSON. Elles sont ici **calculées** par `GetPrevious` et `GetNext` à partir du champ `order`, parmi les bandes-son réellement présentes. S'il te manque le dossier « 02.5 - Vegeta », la précédente de Namek devient « 02 - Saiyans » au lieu de pointer vers un dossier absent.

### Les paramètres

[SettingsService.cs](../../src/KakarotOstManager/Services/SettingsService.cs) écrit dans `%LOCALAPPDATA%\KakarotAnimeOstManager\settings.json`. L'écriture se fait en deux temps : d'abord dans `settings.json.tmp`, puis un renommage qui remplace l'ancien fichier d'un seul coup. Si le programme est interrompu en pleine écriture, l'ancien fichier reste intact. Le jalon 2 appliquera exactement la même technique au `Bgm.awb` du jeu.

### L'empreinte d'un fichier

Tous les `Bgm.awb` du mod font exactement 417 700 608 octets : la taille ne permet pas de les distinguer. [HashService.cs](../../src/KakarotOstManager/Services/HashService.cs) calcule donc leur **empreinte SHA-256**, une suite de 64 caractères qui change du tout au tout dès qu'un seul octet diffère. Le fichier est lu par blocs de 1 Mo afin de ne jamais occuper 400 Mo de mémoire.

## 4. Les tests

Un test suit toujours le même plan en trois temps, **préparer, agir, vérifier** :

```csharp
[Fact]
public void Le_Bgm_awb_pose_a_la_racine_n_est_pas_une_bande_son()
{
    using var pack = new TempDirectory();                    // préparer
    pack.CreateFile("fichier actif du jeu", "Bgm.awb");
    AddSoundtrackFolder(pack, "03 - Namek");

    Soundtrack only = Assert.Single(CreateService().FindSoundtracks(pack.Root));   // agir

    Assert.Equal("03-namek", only.Id);                       // vérifier
}
```

- `[Fact]` marque un test simple.
- `[Theory]` avec plusieurs `[InlineData(…)]` exécute le même test sur plusieurs jeux de valeurs.
- [TempDirectory.cs](../../tests/KakarotOstManager.Tests/TempDirectory.cs) crée un dossier temporaire et le supprime à la fin du test. Les tests fabriquent de faux `Bgm.awb` de quelques octets et ne touchent jamais à tes vrais fichiers.

| Fichier de tests | Ce qu'il garantit |
|---|---|
| [GameServiceTests.cs](../../tests/KakarotOstManager.Tests/GameServiceTests.cs) | Chemins BGM des deux versions, détection de la version, validation du dossier du jeu. |
| [SoundtrackServiceTests.cs](../../tests/KakarotOstManager.Tests/SoundtrackServiceTests.cs) | Reconnaissance des vrais noms de dossiers, rubrique « Autres », doublons, précédente / suivante. |
| [SoundtrackCatalogTests.cs](../../tests/KakarotOstManager.Tests/SoundtrackCatalogTests.cs) | Le `soundtracks.json` livré est valide : identifiants uniques, motifs corrects, noms dans les deux langues. |
| [SettingsServiceTests.cs](../../tests/KakarotOstManager.Tests/SettingsServiceTests.cs) | Aller-retour des paramètres, fichier abîmé, absence de fichier provisoire oublié. |
| [HashServiceTests.cs](../../tests/KakarotOstManager.Tests/HashServiceTests.cs) | Empreinte conforme à la valeur de référence, avancement, annulation. |
| [LocalizedTextTests.cs](../../tests/KakarotOstManager.Tests/LocalizedTextTests.cs) | Choix de la langue et repli sur l'autre langue. |

Pour les lancer : `dotnet test`. Résultat à la fin de ce jalon : 54 tests réussis sur 54.

## 5. Exercices facultatifs

1. **Ajouter une bande-son au catalogue.** Dans `soundtracks.json`, ajoute une entrée `"id": "bardock"`, `"match": "Bardock"`, `"category": "dlc"`. Écris ensuite, dans `SoundtrackServiceTests.cs`, un test qui crée un dossier `DLC - Bardock` et vérifie qu'il est reconnu dans la rubrique `Dlc`.
2. **Casser un motif.** Remplace `02\\.5` par `02.5` dans le JSON. Les tests passent-ils encore ? Pourquoi ? Trouve un nom de dossier que ce motif accepterait à tort.
3. **Lire un message d'échec.** Dans `GameService.cs`, remplace `"Sound"` par `"Audio"`, lance `dotnet test` et observe ce que xUnit affiche : valeur attendue, valeur obtenue, nom du test.

Annule tes essais avec `git restore .` avant de continuer.

## 6. Ce qui reste ouvert

- **Les indications « quand activer » sont vides** dans `soundtracks.json`, et **aucun DLC n'y figure** : j'attends le texte de la page Nexus, que je ne peux pas consulter. D'ici là, un dossier DLC apparaîtra dans « Autres » et restera installable. *Résolu depuis : voir [05-catalogue-complet](05-catalogue-complet.md).*
- **Le chemin de la version Remaster** vient de pages web, pas d'une installation réelle. À confirmer quand le jeu sera réinstallé. *Confirmé depuis par la description du mod ; reste à l'essayer sur le jeu installé.*

## 7. La suite : jalon 2

Le jalon 2 ajoute tout ce qui écrit sur le disque : la sauvegarde du `Bgm.awb` original, l'installation d'une bande-son avec vérification par empreinte, la restauration, les contrôles préalables et la détection du jeu en cours d'exécution.
