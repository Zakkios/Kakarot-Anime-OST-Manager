# Jalon 3 — L'interface

Ce jalon donne enfin un visage au logiciel : une fenêtre où l'on choisit ses dossiers, où l'on voit les bandes-son du pack, et où l'on clique sur **Appliquer**. Elle est disponible en français et en anglais. C'est le « MVP » du §26 du cahier des charges : l'application est utilisable de bout en bout.

Toute la logique des jalons 1 et 2 est réutilisée telle quelle. Ce jalon n'ajoute aucune règle : il **affiche** et il **déclenche**.

## 1. Ce qu'on a construit

```text
src/KakarotOstManager/
├── App.xaml / App.xaml.cs           démarrage : création et assemblage des services
├── Localization/
│   ├── Strings.resx                 textes en anglais (langue par défaut)
│   ├── Strings.fr.resx              textes en français
│   ├── Localizer.cs                 donne le texte d'une clé dans la langue courante
│   └── LocExtension.cs              permet d'écrire {loc:Loc Apply} dans le XAML
├── ViewModels/
│   ├── MainViewModel.cs             l'état et les actions de la fenêtre
│   └── ViewModelTypes.cs            petits types d'affichage
└── Views/
    ├── MainWindow.xaml              la fenêtre
    └── FolderPicker.cs              la boîte « choisir un dossier »
```

La fenêtre comporte quatre zones, de haut en bas :

1. **L'en-tête** : le titre et le choix de la langue.
2. **Les réglages** : dossier du jeu, dossier du pack, version du jeu.
3. **La zone principale** : à gauche la liste des bandes-son regroupées par rubrique, à droite le détail de celle qui est sélectionnée, la bande-son actuellement installée et les boutons **Appliquer** et **Restaurer l'OST originale**.
4. **L'état** : le message de succès ou d'erreur, la barre d'avancement et la liste des contrôles effectués.

Ce qui a été vérifié sur la vraie fenêtre, pilotée automatiquement sur un faux jeu : la sélection de « 03 — Namek » puis le clic sur **Appliquer** remplacent bien le fichier du faux jeu, créent la sauvegarde, affichent le message de succès en vert, les sept contrôles cochés et le repère « ● installée » dans la liste.

## 2. Le principe : MVVM

**MVVM** signifie *Model – View – ViewModel*. C'est la façon habituelle d'organiser une application WPF.

| Couche | Rôle | Dans ce projet |
|---|---|---|
| **Model** | Les données et les règles du logiciel. Ne sait pas qu'une fenêtre existe. | `Models/` et `Services/` |
| **View** | Ce qui est dessiné à l'écran. Ne contient aucune règle. | `MainWindow.xaml` |
| **ViewModel** | L'état de l'écran (textes, listes, boutons actifs ou non) et ses actions. Ne connaît aucun contrôle graphique. | `MainViewModel.cs` |

La vue et le ViewModel sont reliés par des **liaisons de données** (*data bindings*). La vue dit « ce texte affiche la propriété `InstalledTitle` » ; quand la propriété change, le texte se met à jour tout seul.

L'intérêt est très concret : le ViewModel se teste **sans fenêtre**. Les 22 tests de [MainViewModelTests.cs](../../tests/KakarotOstManager.Tests/MainViewModelTests.cs) « cliquent » sur Appliquer et lisent le message affiché sans jamais rien dessiner.

## 3. Les notions à retenir

### La liaison de données

Dans [MainWindow.xaml](../../src/KakarotOstManager/Views/MainWindow.xaml) :

```xml
<TextBlock Text="{Binding InstalledTitle}" />
<Button Content="{loc:Loc Apply}" Command="{Binding ApplyCommand}" />
```

`{Binding InstalledTitle}` cherche une propriété `InstalledTitle` sur le **`DataContext`** de la fenêtre, c'est-à-dire l'objet qu'on lui a donné comme source de données. Ce `DataContext` est le ViewModel, attribué au démarrage dans [App.xaml.cs](../../src/KakarotOstManager/App.xaml.cs) :

```csharp
var window = new MainWindow { DataContext = viewModel };
```

### `INotifyPropertyChanged` : prévenir la vue

Pour que la vue se rafraîchisse, le ViewModel doit **annoncer** chaque changement. C'est le rôle de l'interface `INotifyPropertyChanged`. L'écrire à la main est répétitif ; le paquet `CommunityToolkit.Mvvm` le fait pour nous. Il suffit d'hériter de `ObservableObject` et de marquer une propriété :

```csharp
public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string InstalledTitle { get; set; }
}
```

`[ObservableProperty]` est un **attribut** : une étiquette posée sur du code. Ici elle est lue par un **générateur de source**, un outil qui écrit du C# pendant la compilation. Il produit le corps de la propriété, qui compare l'ancienne et la nouvelle valeur puis prévient la vue. C'est pour cela que la classe et la propriété sont `partial` : nous écrivons une moitié, le générateur écrit l'autre.

### Les commandes

Un bouton ne déclenche pas une méthode directement : il est lié à une **commande**, un objet qui sait s'exécuter et dire s'il en a le droit à cet instant.

```csharp
private bool CanApply() => IsIdle && SelectedSoundtrack is not null;

[RelayCommand(CanExecute = nameof(CanApply))]
private async Task ApplyAsync()
{
    …
}
```

`[RelayCommand]` génère une propriété `ApplyCommand` à partir de la méthode `ApplyAsync`. Tant que `CanApply()` renvoie `false`, WPF **grise le bouton tout seul**. Encore faut-il lui dire quand reposer la question, ce que fait cet attribut posé sur les propriétés concernées :

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
public partial SoundtrackItemViewModel? SelectedSoundtrack { get; set; }
```

`nameof(ApplyCommand)` vaut le texte `"ApplyCommand"`, mais vérifié par le compilateur : si la commande est renommée, cette ligne ne compile plus au lieu de casser en silence.

### Réagir à un changement

Le générateur prévoit un point d'accroche par propriété : une méthode `On…Changed` appelée après chaque modification.

```csharp
partial void OnSelectedSoundtrackChanged(SoundtrackItemViewModel? value) => RefreshSelection();
```

Quand l'utilisateur clique sur une ligne de la liste, WPF modifie `SelectedSoundtrack`, cette méthode s'exécute, et le panneau de détail se met à jour.

### Les listes : `ObservableCollection` et `DataTemplate`

Une `ObservableCollection<T>` est une liste qui prévient la vue quand on y ajoute ou retire un élément. La vue décrit ensuite, avec un **`DataTemplate`**, comment dessiner **un** élément ; WPF répète ce modèle pour chacun :

```xml
<ListBox.ItemTemplate>
    <DataTemplate>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="{Binding Title}" />
            <TextBlock Visibility="{Binding IsActive, Converter={StaticResource BoolToVisibility}}" …>
                <Run Text="●" />
                <Run Text="{loc:Loc InstalledBadge}" />
            </TextBlock>
        </StackPanel>
    </DataTemplate>
</ListBox.ItemTemplate>
```

À l'intérieur d'un `DataTemplate`, les liaisons portent sur l'élément de la liste, ici un `SoundtrackItemViewModel`, et non plus sur le `MainViewModel`.

Un **convertisseur** transforme une valeur au passage. `BoolToVisibility` change `true` en « visible » et `false` en « masqué ».

Le regroupement par rubrique est déclaré sans une ligne de C#, par un `CollectionViewSource` :

```xml
<CollectionViewSource x:Key="GroupedSoundtracks" Source="{Binding Soundtracks}">
    <CollectionViewSource.GroupDescriptions>
        <PropertyGroupDescription PropertyName="CategoryTitle" />
    </CollectionViewSource.GroupDescriptions>
</CollectionViewSource>
```

### Styles et déclencheurs

Un **`Style`** regroupe des réglages réutilisables. Un **déclencheur** (*trigger*) les modifie selon une condition. Voici comment un message d'erreur disparaît entièrement quand il est vide :

```xml
<Style x:Key="ErrorText" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{StaticResource ErrorBrush}" />
    <Style.Triggers>
        <Trigger Property="Text" Value="">
            <Setter Property="Visibility" Value="Collapsed" />
        </Trigger>
    </Style.Triggers>
</Style>
```

Un `DataTrigger` fait de même à partir d'une propriété du ViewModel. C'est lui qui colore le message d'état en vert ou en rouge selon `Status`.

### Le fil de l'interface et `Task.Run`

Une application WPF possède un **fil d'exécution de l'interface**. Lui seul dessine la fenêtre et réagit aux clics. S'il est occupé à copier 400 Mo, la fenêtre se fige. Le ViewModel envoie donc le travail lourd sur un autre fil :

```csharp
OperationResult result = await Task.Run(() => operation(progress));
```

Pendant ce temps le fil de l'interface reste libre. Après le `await`, l'exécution **revient automatiquement sur le fil de l'interface**, où l'on a le droit de modifier ce qui est affiché.

L'avancement pose le problème inverse : il est émis depuis le fil de travail, alors que la barre de progression ne peut être modifiée que par le fil de l'interface. La petite classe `UiProgress`, en bas de [MainViewModel.cs](../../src/KakarotOstManager/ViewModels/MainViewModel.cs), renvoie chaque valeur vers le bon fil.

### La traduction : fichiers `.resx`

Les textes ne sont jamais écrits dans le code. Chacun a une **clé**, et sa valeur figure dans deux fichiers :

| Fichier | Langue | Exemple pour la clé `Apply` |
|---|---|---|
| [Strings.resx](../../src/KakarotOstManager/Localization/Strings.resx) | anglais, langue par défaut | `Apply` |
| [Strings.fr.resx](../../src/KakarotOstManager/Localization/Strings.fr.resx) | français | `Appliquer` |

À la compilation, le fichier français devient un fichier à part, `fr\KakarotOstManager.resources.dll`, appelé **assembly satellite**. Ajouter une langue revient à ajouter un fichier `Strings.xx.resx`.

[Localizer.cs](../../src/KakarotOstManager/Localization/Localizer.cs) renvoie le texte d'une clé dans la langue courante. Il possède un **indexeur**, c'est-à-dire qu'on l'utilise comme un tableau : `_loc["Apply"]`. Pour un texte à trous : `_loc.Format("ErrorSourceMissing", titre)` remplace `{0}` par le titre.

Côté XAML, [LocExtension.cs](../../src/KakarotOstManager/Localization/LocExtension.cs) définit une **extension de balisage**, ce qui autorise l'écriture `{loc:Loc Apply}`. Elle crée une liaison vers le `Localizer`. Changer de langue dans la liste déroulante retraduit ainsi toute la fenêtre instantanément, sans redémarrage.

Les messages construits par le ViewModel sont mémorisés sous forme de **fonction** et non de texte, afin de pouvoir être recalculés dans la nouvelle langue :

```csharp
SetStatus(StatusKind.Success, () => _loc.Format("SuccessInstalled", TitleOf(soundtrack)));
```

`() => …` est une lambda sans paramètre. Le type `Func<string>` désigne « une fonction qui renvoie un `string` ».

### Le point d'assemblage

[App.xaml.cs](../../src/KakarotOstManager/App.xaml.cs) est le seul endroit où les services sont créés avec `new` et reliés entre eux. Chaque classe reçoit ses dépendances par son constructeur et ne va jamais les chercher elle-même. En remplaçant quelques éléments de cet assemblage (un faux sélecteur de dossier, un dossier temporaire), les tests obtiennent une application complète qui ne touche à rien de réel.

### L'accessibilité

Un lecteur d'écran identifie chaque contrôle par son **nom d'automatisation**. Par défaut, une ligne de liste porte le nom de sa classe C#, ce qui ne veut rien dire pour l'utilisateur. La fenêtre le précise donc explicitement :

```xml
<Setter Property="AutomationProperties.Name" Value="{Binding Title}" />
```

Ce défaut a été repéré en pilotant la fenêtre par programme : l'outil ne trouvait pas la ligne « 03 — Namek ».

## 4. Un bogue trouvé en route

Au premier lancement de la fenêtre sur un bac à sable, les dossiers préconfigurés n'apparaissaient pas, et `settings.json` avait été **vidé**.

La cause : le constructeur du ViewModel affectait `SelectedLanguage` pour afficher la langue par défaut. Cette affectation déclenchait `OnSelectedLanguageChanged`, prévue pour enregistrer le choix de l'utilisateur. Elle enregistrait donc des paramètres encore vides, **avant** que `InitializeAsync` n'ait lu le fichier.

La correction tient en un drapeau, `_applyingLanguageFromSettings`, levé pendant que le programme règle lui-même la langue. Le test `Creer_puis_initialiser_le_ViewModel_n_ecrase_pas_les_parametres_enregistres` empêche ce bogue de revenir.

Deux leçons : une réaction à un changement s'exécute aussi quand c'est le programme qui change la valeur, et les tests des jalons précédents ne pouvaient pas voir ce défaut, puisqu'il se situait dans une couche qui n'existait pas encore.

## 5. Lancer et essayer

```text
dotnet run --project src/KakarotOstManager
```

Pour essayer l'application **sans toucher à ta vraie configuration**, l'option `--data-dir` range paramètres et sauvegardes dans le dossier de ton choix :

```text
dotnet run --project src/KakarotOstManager -- --data-dir C:\Temp\kaom-essai
```

Le `--` isolé sépare les options de `dotnet run` de celles de l'application.

Sans cette option, les données vont dans `%LOCALAPPDATA%\KakarotAnimeOstManager\`.

## 6. Exercices facultatifs

1. **Corriger un texte.** Modifie la valeur de `SuccessRestored` dans `Strings.fr.resx`, relance, installe puis restaure une bande-son.
2. **Afficher le chemin complet.** Ajoute au ViewModel une propriété `SelectedBgmFile` remplie dans `RefreshSelection()` à partir de `soundtrack.BgmFilePath`, puis un `TextBlock` lié dans le panneau de détail.
3. **Oublier de prévenir la vue.** Dans `ViewModelTypes.cs`, remplace la propriété `IsActive` par une propriété ordinaire, `public bool IsActive { get; set; }`, sans `[ObservableProperty]` ni `partial`. Les tests passent-ils encore ? Lance ensuite l'application avec `--data-dir` et installe une bande-son : le repère « ● installée » se déplace-t-il dans la liste ? Tu viens de voir ce qu'apporte la notification, et ce que les tests du ViewModel ne peuvent pas détecter.
4. **Ajouter une langue.** Copie `Strings.fr.resx` en `Strings.es.resx`, traduis trois textes, puis ajoute l'espagnol à `Languages` dans le ViewModel et à `ToSupportedCulture` dans `Localizer`.

Annule tes essais avec `git restore .` avant de continuer.

## 7. Écarts par rapport au plan

- **Thème Fluent.** Le plan repoussait l'amélioration visuelle au jalon 4. Une seule ligne dans [App.xaml](../../src/KakarotOstManager/App.xaml) donne l'apparence de Windows 11, en clair ou en sombre selon le réglage du système et avec sa couleur d'accentuation. Elle a été ajoutée dès maintenant.
- **Regroupement par rubrique et barre d'avancement**, prévus au jalon 4, sont déjà là : les services les fournissaient et ton dossier `backup` aurait sinon été mêlé aux chapitres de l'histoire.
- **Les dossiers ne se saisissent pas au clavier**, seulement avec « Parcourir… ». C'est volontaire pour cette version : cela évite d'avoir à valider un chemin en cours de frappe.

## 8. La suite : jalon 4

Le jalon 4 ajoute le bouton « Lancer Dragon Ball Z: Kakarot » via Steam, l'affichage de la bande-son précédente et suivante, le bouton « Installer l'OST suivante » et la détection automatique du dossier du jeu à partir de Steam.
