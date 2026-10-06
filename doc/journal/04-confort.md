# Jalon 4 — Les fonctions de confort (V0.2)

Le jalon 3 a livré une application utilisable. Celui-ci retire les dernières manipulations évitables : trouver le dossier du jeu, deviner quelle bande-son vient ensuite, ouvrir Steam pour relancer le jeu.

## 1. Ce qu'on a construit

| Fonction | Ce que voit l'utilisateur | Cahier des charges |
|---|---|---|
| **Détection automatique** | Au premier lancement, le dossier du jeu est déjà rempli. Si le pack est rangé dans le dossier BGM du jeu, lui aussi. | §27 |
| **Précédente et suivante** | Sous le détail d'un chapitre : « Bande-son précédente » et « Bande-son suivante ». | §4 |
| **Installer l'OST suivante** | Sous la bande-son installée : le chapitre suivant et un bouton pour l'installer en un clic. | §30 |
| **Lancer le jeu** | Un bouton « Lancer Dragon Ball Z: Kakarot », qui passe par Steam. | §17 |

Fichiers ajoutés ou modifiés :

```text
src/KakarotOstManager/
├── Services/
│   ├── SteamLocator.cs      (nouveau) retrouver le jeu dans les bibliothèques Steam
│   ├── ShellLauncher.cs     (nouveau) demander à Windows d'ouvrir une adresse
│   └── GameService.cs       + LaunchKakarot()
├── ViewModels/MainViewModel.cs   + détection, précédente / suivante, commandes
└── Views/MainWindow.xaml         + les nouveaux éléments
```

Vérifié sur ta machine, en lecture seule : la détection retrouve `D:\SteamLibrary\steamapps\common\DRAGON BALL Z KAKAROT`, version Standard. Comme ton pack se trouve dans le dossier BGM de ce jeu, **ton premier lancement réel sera entièrement configuré sans rien choisir**.

La suite compte 143 tests.

## 2. Comment le jeu est retrouvé

Steam laisse deux traces exploitables :

1. Son dossier d'installation, écrit dans le **registre de Windows**.
2. La liste de ses **bibliothèques** (en général une par disque), dans le fichier `steamapps\libraryfolders.vdf` de ce dossier.

[SteamLocator.cs](../../src/KakarotOstManager/Services/SteamLocator.cs) lit la première, puis le second, et cherche dans chaque bibliothèque un dossier `steamapps\common\DRAGON BALL Z KAKAROT` contenant bien un dossier BGM.

### Lire le registre

Le registre est une base de réglages organisée comme une arborescence de dossiers, appelés **clés**, qui contiennent des **valeurs**.

```csharp
using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
return key?.GetValue("SteamPath") as string;
```

- `Registry.CurrentUser` désigne la branche de l'utilisateur connecté.
- `OpenSubKey` renvoie `null` si la clé n'existe pas, par exemple quand Steam n'est pas installé. D'où le `?.`.
- `as string` tente une conversion et renvoie `null` si la valeur n'est pas un texte, au lieu de lancer une erreur.
- Le `@` devant la chaîne en fait une **chaîne littérale** : les `\` y sont de vrais caractères, inutile de les doubler.

Tout est entouré d'un `try / catch`. La détection est un confort ; si elle échoue, l'utilisateur choisit son dossier avec « Parcourir… ».

### Lire `libraryfolders.vdf`

Ce fichier a un format propre à Valve :

```text
"1"
{
    "path"      "D:\\SteamLibrary"
    …
}
```

Seules les lignes `"path"` nous intéressent. Plutôt que d'écrire un analyseur complet du format, une expression régulière les extrait :

```csharp
Regex.Matches(vdfContent, "\"path\"\\s+\"([^\"]*)\"", RegexOptions.IgnoreCase)
    .Select(match => match.Groups[1].Value.Replace(@"\\", @"\"))
```

Les parenthèses de l'expression forment un **groupe de capture** : `match.Groups[1]` contient ce qui se trouve entre les guillemets. `[^\"]*` signifie « tout sauf un guillemet, autant de fois que possible ».

### Le pack dans le dossier du jeu

Les instructions du mod font copier `Bgm.awb` dans le dossier BGM du jeu ; beaucoup de joueurs y extraient donc tout le pack, comme toi. `TryDetectPackInGameFolder`, dans [MainViewModel.cs](../../src/KakarotOstManager/ViewModels/MainViewModel.cs), en tire parti : si aucun dossier de pack n'est choisi et que le dossier BGM contient des bandes-son, il est proposé d'office.

Un dossier déjà enregistré n'est jamais remplacé par la détection. Et quand elle a servi, un message le signale : « Dossiers détectés automatiquement. Vérifiez-les avant de continuer. »

## 3. Lancer le jeu par Steam

Lancer directement l'exécutable du jeu ferait perdre l'overlay Steam, les sauvegardes dans le cloud et le décompte du temps de jeu. L'application ouvre donc une **adresse** :

```csharp
public const string SteamAppId = "851850";
public const string SteamLaunchUri = "steam://rungameid/" + SteamAppId;
```

De même que Windows ouvre un navigateur pour une adresse `https://`, il transmet à Steam une adresse `steam://`. Il suffit de la lui confier, dans [ShellLauncher.cs](../../src/KakarotOstManager/Services/ShellLauncher.cs) :

```csharp
Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
```

`UseShellExecute = true` veut dire : « ne lance pas ceci comme un programme, demande à Windows quoi en faire », exactement comme un double-clic.

Le mot-clé **`const`** déclare une valeur fixée à la compilation et qui ne changera jamais.

## 4. Précédente, suivante, et « la suite »

Les méthodes `GetPrevious` et `GetNext` existaient depuis le jalon 1. Il restait à les afficher, de deux façons qu'il ne faut pas confondre :

| Affichage | Par rapport à quoi | Méthode du ViewModel |
|---|---|---|
| « Bande-son précédente / suivante » | La bande-son **sélectionnée** dans la liste. | `RefreshSelection` |
| « Prochaine bande-son à installer » | La bande-son **installée** dans le jeu. | `RefreshNextUp` |

La seconde suit cette règle :

```csharp
_nextUp = _installed.Kind switch
{
    InstalledKind.Soundtrack => SoundtrackService.GetNext(_pack, _installed.Soundtrack!),
    InstalledKind.Vanilla or InstalledKind.Unknown =>
        _pack.FirstOrDefault(soundtrack => soundtrack.Category == SoundtrackCategory.Main),
    _ => null,
};
```

Si un chapitre est en place, on propose le suivant. Si la musique d'origine est en place, on propose le premier chapitre. Après le dernier chapitre, le bloc disparaît.

Le bouton ne contient aucune logique d'installation à lui : il sélectionne le chapitre concerné puis appelle `ApplyAsync`, la même méthode que le bouton Appliquer. **Un seul chemin de code installe une bande-son**, donc toutes les sécurités du jalon 2 s'appliquent sans avoir été réécrites.

## 5. Les notions à retenir

### Deux nouvelles interfaces pour tester l'intestable

Lancer Steam et lire le registre sont, comme la détection du jeu en cours d'exécution au jalon 2, des actions qu'un test ne doit pas réellement accomplir. Même réponse : une interface, une réalisation réelle, une réalisation factice.

| Interface | Réalisation réelle | Réalisation des tests |
|---|---|---|
| `IShellLauncher` | `SystemShellLauncher` ouvre l'adresse. | `FakeShellLauncher` note l'adresse demandée. |
| `ISteamLocator` | `SteamLocator` lit le registre et le disque. | `FakeSteamLocator` renvoie le dossier qu'on lui indique. |

Le test peut alors affirmer une chose précise :

```csharp
screen.ViewModel.LaunchGameCommand.Execute(null);

Assert.Equal(["steam://rungameid/851850"], screen.Env.Shell.Opened);
```

La partie de `SteamLocator` qui ne dépend pas du registre, soit la lecture du fichier et la recherche dans les bibliothèques, est testée directement dans [SteamLocatorTests.cs](../../tests/KakarotOstManager.Tests/SteamLocatorTests.cs), à partir d'un extrait d'un vrai `libraryfolders.vdf`.

### `is { } nom` : tester et nommer d'un coup

```csharp
if (string.IsNullOrWhiteSpace(_settings.GamePath) && _steamLocator.FindGameDirectory() is { } gameDirectory)
{
    _settings.GamePath = gameDirectory;
}
```

`is { } gameDirectory` se lit : « si le résultat n'est pas `null`, appelle-le `gameDirectory` ». Cela évite de déclarer une variable puis de la tester sur la ligne suivante.

### `StaticResource` et `DynamicResource`

Le bouton Appliquer prend la couleur d'accentuation de Windows grâce à un style fourni par le thème Fluent :

```xml
<Button Style="{DynamicResource AccentButtonStyle}" … />
```

- `StaticResource` cherche la ressource une seule fois, au chargement, et **échoue** si elle n'existe pas.
- `DynamicResource` la recherche au fil de l'eau et **se met à jour** si elle change, par exemple quand Windows passe du mode clair au mode sombre. Si elle n'existe pas, le contrôle garde simplement son apparence normale.

### `WrapPanel`

Les trois boutons du bas sont dans un `WrapPanel` : un conteneur qui place ses enfants côte à côte et **passe à la ligne** quand la largeur manque. Avec des textes plus longs en français qu'en anglais, c'est plus sûr qu'une rangée fixe.

## 6. Exercices facultatifs

1. **Changer la règle de « la suite ».** Fais en sorte qu'après « 07.5 — Post Game », l'application propose le premier DLC présent dans le pack. Commence par écrire le test, dans `MainViewModelTests.cs`, puis modifie `RefreshNextUp`.
2. **Un second raccourci.** Ajoute un bouton « Ouvrir le dossier BGM » qui appelle `IShellLauncher.Open` avec le chemin du dossier BGM du jeu. Windows ouvrira l'Explorateur de fichiers.
3. **Lire une autre valeur du fichier.** Écris une méthode `ParseInstalledAppIds` qui renvoie les identifiants des jeux installés, c'est-à-dire les lignes `"851850"  "…"` des blocs `"apps"`. Teste-la avec l'extrait de `SteamLocatorTests.cs`.

Annule tes essais avec `git restore .` avant de continuer.

## 7. Points ouverts

- **Le lancement par Steam n'a pas été essayé pour de bon**, puisque le jeu n'est pas installé chez toi. Les tests vérifient l'adresse demandée, pas la réaction de Steam.
- **La détection considère ton dossier actuel comme valide** alors que le jeu est désinstallé : il reste un dossier BGM, c'est le critère retenu. Tant que le jeu n'est pas réinstallé, « Aucun fichier Bgm.awb dans le dossier du jeu » s'affichera comme bande-son installée, ce qui est exact.
- **Le texte Nexus manque toujours** : les indications « quand activer » sont vides et les DLC apparaissent dans « Autres ».

## 8. La suite : jalon 5

Le jalon 5 prépare la distribution : une icône, un exécutable autonome en un seul fichier, un README en français et en anglais, puis un essai complet sur le jeu réinstallé.
