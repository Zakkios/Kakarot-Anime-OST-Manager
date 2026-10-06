# Jalon 0 — Le socle du projet

Ce premier jalon ne contient aucune fonctionnalité. Il met en place tout ce qu'il faut pour pouvoir écrire, compiler, lancer et tester du code C# : les outils, l'organisation des dossiers, une fenêtre vide et un premier test automatique.

## 1. Ce qu'on a construit

- Le **SDK .NET 10** a été installé (version 10.0.401) à côté du SDK 9 que tu avais déjà.
- Le dossier local est relié à ton dépôt GitHub `Zakkios/Kakarot-Anime-OST-Manager`.
- Une **solution** regroupe deux **projets** : l'application et ses tests.
- L'application ouvre une fenêtre qui affiche le nom du logiciel.
- Un test automatique vérifie que le projet de tests « voit » bien l'application.

```text
Kakarot-Anime-OST-Manager/
├── KakarotOstManager.slnx          la solution : la liste des projets
├── global.json                     la version du SDK à utiliser
├── .gitignore                      ce que git doit ignorer (bin/, obj/…)
├── doc/
│   ├── Kakarot Anime OST Manager — Cahier des charges.md
│   └── journal/00-socle.md         ce document
├── src/KakarotOstManager/          le projet de l'application
│   ├── KakarotOstManager.csproj
│   ├── App.xaml  +  App.xaml.cs
│   ├── AssemblyInfo.cs
│   └── Views/MainWindow.xaml  +  MainWindow.xaml.cs
└── tests/KakarotOstManager.Tests/  le projet de tests
    ├── KakarotOstManager.Tests.csproj
    └── SmokeTests.cs
```

## 2. Les notions à retenir

### SDK et runtime

.NET se compose de deux choses distinctes.

- Le **runtime** exécute les programmes déjà compilés. C'est ce dont un utilisateur final a besoin.
- Le **SDK** (*Software Development Kit*) contient le runtime, plus le compilateur C# et l'outil en ligne de commande `dotnet`. C'est ce dont un développeur a besoin.

Plusieurs SDK peuvent cohabiter sur la même machine. Le fichier [global.json](../../global.json) indique lequel utiliser pour ce projet :

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.401"
  }
}
```

`rollForward: latestFeature` signifie : « utilise la 10.0.401, ou une version 10.0.x plus récente si elle est installée, mais jamais une version 11 ».

Nous ciblons .NET 10 parce que c'est une version **LTS** (*Long Term Support*, maintenue trois ans), alors que .NET 9 arrive en fin de support en novembre 2026.

### Solution et projet

- Un **projet** (fichier `.csproj`) décrit une chose à compiler : une application, une bibliothèque ou une suite de tests. Le résultat de la compilation s'appelle un **assembly** (un fichier `.dll`).
- Une **solution** (fichier `.slnx`) est une simple liste de projets. Elle permet de tout compiler et de tout tester d'un seul coup, et c'est elle qu'on ouvre dans un éditeur.

Le fichier [KakarotOstManager.slnx](../../KakarotOstManager.slnx) est très court :

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/KakarotOstManager/KakarotOstManager.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/KakarotOstManager.Tests/KakarotOstManager.Tests.csproj" />
  </Folder>
</Solution>
```

### Le fichier `.csproj`

Voici celui de l'application, [KakarotOstManager.csproj](../../src/KakarotOstManager/KakarotOstManager.csproj) :

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UseWPF>true</UseWPF>
</PropertyGroup>
```

| Ligne | Signification |
|---|---|
| `OutputType = WinExe` | On produit un programme Windows avec fenêtre, sans console noire derrière. |
| `TargetFramework = net10.0-windows` | On cible .NET 10. Le suffixe `-windows` donne accès aux fonctions propres à Windows, dont WPF. |
| `Nullable = enable` | Le compilateur nous avertit quand une variable risque de valoir `null`. Cela évite la plus fréquente des erreurs à l'exécution. |
| `ImplicitUsings = enable` | Les espaces de noms les plus courants (`System`, `System.Linq`, `System.Collections.Generic`…) sont importés automatiquement dans chaque fichier. |
| `UseWPF = true` | Active WPF, la bibliothèque d'interface graphique. |

Il n'y a nulle part de liste des fichiers `.cs` : tout fichier `.cs` présent dans le dossier du projet est compilé automatiquement.

### NuGet

**NuGet** est le gestionnaire de paquets de .NET. Un paquet se déclare dans le `.csproj` par une ligne `PackageReference`. Pour l'instant seul le projet de tests en utilise, par exemple `xunit`. La commande `dotnet restore`, lancée automatiquement par `dotnet build`, télécharge les paquets manquants.

### Les dossiers `bin/` et `obj/`

Ils apparaissent dans chaque projet après une compilation.

- `obj/` contient les fichiers intermédiaires du compilateur.
- `bin/` contient le résultat final, ici `bin/Debug/net10.0-windows/KakarotOstManager.exe`.

On peut les supprimer sans risque, ils sont recréés à la compilation suivante. Le `.gitignore` les exclut du dépôt.

### Espaces de noms et classes partielles

Un **espace de noms** (`namespace`) range les classes par famille et évite les conflits de noms. Par convention il reprend le chemin des dossiers : la classe `MainWindow` du dossier `Views` vit dans `KakarotOstManager.Views`.

Le mot-clé **`partial`** indique qu'une classe est écrite en plusieurs morceaux. WPF s'en sert partout : une moitié de la classe est écrite par nous en C#, l'autre est générée par le compilateur à partir du fichier XAML.

## 3. Visite guidée du code

### L'application : `App.xaml`

[App.xaml](../../src/KakarotOstManager/App.xaml) représente l'application entière. La seule information utile pour l'instant est la fenêtre à ouvrir au démarrage :

```xml
<Application x:Class="KakarotOstManager.App"
             ...
             StartupUri="Views/MainWindow.xaml">
```

[App.xaml.cs](../../src/KakarotOstManager/App.xaml.cs) est sa moitié C#. Elle est vide : la méthode `Main`, par laquelle tout programme C# démarre, est générée automatiquement par WPF.

### La fenêtre : `MainWindow.xaml`

**XAML** est un langage à balises qui décrit l'interface. Chaque balise crée un objet, chaque attribut règle une de ses propriétés. Extrait de [MainWindow.xaml](../../src/KakarotOstManager/Views/MainWindow.xaml) :

```xml
<Window x:Class="KakarotOstManager.Views.MainWindow"
        Title="Kakarot Anime OST Manager"
        Height="600" Width="900">
    <Grid>
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <TextBlock Text="KAKAROT ANIME OST MANAGER" FontSize="28" FontWeight="Bold" />
            <TextBlock Text="Jalon 0 — le socle est en place." FontSize="14" />
        </StackPanel>
    </Grid>
</Window>
```

- `Window` est la fenêtre. `x:Class` la relie à sa classe C#.
- `Grid` et `StackPanel` sont des **conteneurs de mise en page**. Le premier dispose ses enfants en lignes et colonnes, le second les empile.
- `TextBlock` affiche du texte.

L'imbrication des balises donne l'imbrication à l'écran : la fenêtre contient une grille, qui contient une pile centrée, qui contient deux textes.

### Le code associé : `MainWindow.xaml.cs`

[MainWindow.xaml.cs](../../src/KakarotOstManager/Views/MainWindow.xaml.cs) est appelé le **code-behind** de la fenêtre :

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

- `: Window` signifie que `MainWindow` **hérite** de la classe `Window` fournie par WPF.
- `public MainWindow()` est le **constructeur**, la méthode exécutée à la création de l'objet.
- `InitializeComponent()` est générée à partir du XAML : c'est elle qui crée réellement les contrôles.

### Le premier test : `SmokeTests.cs`

Nous utilisons **xUnit**, la bibliothèque de tests la plus répandue en .NET. Un test est une méthode marquée `[Fact]` qui échoue si une vérification `Assert` n'est pas satisfaite. Voici [SmokeTests.cs](../../tests/KakarotOstManager.Tests/SmokeTests.cs) :

```csharp
[Fact]
public void Le_projet_de_tests_reference_l_application()
{
    var assembly = typeof(App).Assembly;

    Assert.Equal("KakarotOstManager", assembly.GetName().Name);
    Assert.Equal(new Version(0, 1, 0, 0), assembly.GetName().Version);
}
```

Ce test ne vérifie aucune règle du logiciel. Il prouve seulement que la tuyauterie fonctionne : le projet de tests accède à la classe `App` de l'application, et la version déclarée dans le `.csproj` (`0.1.0`) se retrouve bien dans l'assembly compilé. Il sera remplacé par de vrais tests au jalon 1.

Le lien entre les deux projets est déclaré dans [KakarotOstManager.Tests.csproj](../../tests/KakarotOstManager.Tests/KakarotOstManager.Tests.csproj) :

```xml
<ProjectReference Include="..\..\src\KakarotOstManager\KakarotOstManager.csproj" />
```

## 4. Lancer et tester soi-même

Toutes les commandes se lancent depuis la racine du dépôt.

| Commande | Effet |
|---|---|
| `dotnet build` | Compile toute la solution. |
| `dotnet test` | Compile puis exécute tous les tests. |
| `dotnet run --project src/KakarotOstManager` | Compile puis lance l'application. |

Résultat obtenu à la fin de ce jalon : compilation sans avertissement, 1 test réussi sur 1, et la fenêtre s'ouvre avec le titre « Kakarot Anime OST Manager ».

Pour lire et modifier le code, trois éditeurs conviennent : **Visual Studio** (le plus complet pour WPF), **JetBrains Rider**, ou **VS Code** avec l'extension *C# Dev Kit*. Dans les trois cas, on ouvre le fichier `KakarotOstManager.slnx`.

## 5. Exercices facultatifs

1. **Modifier un texte.** Change le second `TextBlock` de `MainWindow.xaml`, puis relance l'application.
2. **Ajouter un bouton.** Ajoute `<Button Content="Bonjour" Click="OnHelloClick" />` dans le `StackPanel`, puis cette méthode dans la classe `MainWindow` :
   ```csharp
   private void OnHelloClick(object sender, RoutedEventArgs e)
   {
       MessageBox.Show("Bonjour depuis C# !");
   }
   ```
   C'est ton premier **gestionnaire d'événement** : du code exécuté quand l'utilisateur agit.
3. **Faire échouer un test.** Passe `<Version>` à `0.2.0` dans le `.csproj` de l'application, lance `dotnet test` et lis le message d'erreur. Remets ensuite `0.1.0`.

Pense à annuler ces essais avant le jalon suivant, par exemple avec `git restore .`.

## 6. Écarts par rapport au plan

- **Solution au format `.slnx`** au lieu de `.sln`. C'est le format par défaut depuis .NET 10 ; il est en XML lisible, là où l'ancien `.sln` est difficile à lire à l'œil.
- **Le projet de tests cible `net10.0-windows`** et non `net10.0`. Un projet ne peut référencer qu'un projet compatible avec sa propre cible, et l'application est réservée à Windows.

## 7. La suite : jalon 1

Le jalon 1 écrit la partie « lecture seule » du logiciel, sans interface : les modèles de données, la lecture et l'écriture des paramètres, le scan du dossier du pack OST, le calcul du chemin BGM selon la version du jeu et le calcul des empreintes de fichiers. Rien n'y modifie encore les fichiers du jeu.
