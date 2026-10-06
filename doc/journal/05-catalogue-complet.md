# Complément — Le catalogue complet du mod

Jusqu'ici le catalogue ne connaissait que les 9 dossiers présents sur ton disque, sans aucune indication « quand activer », parce que la page Nexus du mod m'était inaccessible. Tu m'as fourni sa description : le catalogue est maintenant complet.

## 1. Ce qui a changé

| Avant | Après |
|---|---|
| 9 bandes-son (l'histoire principale) | 18 bandes-son : 9 pour l'histoire, 9 pour les DLC |
| Indications « quand activer » vides | 18 indications, en anglais et en français |
| Les DLC tombaient dans « Autres » | Les DLC ont leur rubrique |

Les 9 bandes-son de DLC ajoutées :

| N° | Nom | DLC du jeu concerné |
|---|---|---|
| 08 | A New Power Awakens Part 1 | A New Power Awakens Part 1 |
| 09 | A New Power Awakens Part 2 | A New Power Awakens Part 2 |
| 10 | Trunks - The Warrior of Hope | Trunks - The Warrior of Hope |
| 11 | Trunks - The Warrior of Hope - Epilogue | le même, après son générique de fin |
| 12 | Bardock - Alone Against Fate | Bardock - Alone Against Fate |
| 13 | 23rd Tenkaichi Budokai | 23rd Tenkaichi Budokai |
| 14 | End of Z | Goku's Next Journey |
| 15 | Daima Part 1 | DAIMA - Adventure Through The Demon Realm, partie 1 |
| 16 | Daima Part 2 | DAIMA - Adventure Through The Demon Realm, partie 2 |

Les textes anglais sont ceux de l'auteur du mod, repris tels quels. **Les textes français sont ma traduction** : relis-les, en particulier les noms propres (« le monde des Kaio », « Gingertown », « le Cell Game »), que le jeu en français écrit peut-être autrement.

## 2. Ce que la description a tranché

**Le champ « Version du jeu » reste nécessaire.** La description indique deux dossiers d'installation, un par version :

```text
…\DRAGON BALL Z KAKAROT\AT\Content\Sound\Bgm                  version standard
…\DRAGON BALL Z KAKAROT\dlc\Remaster\AT\Content\Sound\Bgm     version HD
```

Le mod fonctionne donc sur les deux, et l'application doit savoir laquelle viser. Ce second chemin, que je tenais jusque-là de pages web, est désormais confirmé par l'auteur. Il reste à l'essayer sur un jeu réellement installé.

**Les dossiers ont changé de nom.** Le pack actuel les appelle `DBZ - Kakarot Soundtrack Mod 03 - Namek`, alors que les tiens, téléchargés en 2024, s'appellent `DBZ Kakarot Soundtrack JPN Mod 03 - Namek`. Les deux formes sont reconnues, puisque les motifs ne regardent que la fin du nom (`03 - Namek`). Ton pack est en revanche une ancienne version : il ne contient pas les DLC.

## 3. La leçon de conception : zéro ligne de C# modifiée

Tout l'ajout tient dans [soundtracks.json](../../src/KakarotOstManager/Data/soundtracks.json). Aucun fichier `.cs` de l'application n'a été touché. C'est exactement ce que demandait le §7 du cahier des charges : « modifier les indications sans toucher au code C# ».

Voici une entrée complète :

```json
{
  "id": "11-trunks-epilogue",
  "match": "11\\s*-\\s*Trunks",
  "number": "11",
  "name": { "fr": "Trunks - The Warrior of Hope - Epilogue", "en": "Trunks - The Warrior of Hope - Epilogue" },
  "category": "dlc",
  "order": 110,
  "activationHint": {
    "fr": "Après avoir terminé le DLC « Trunks - The Warrior of Hope », une fois passée la cinématique du générique de fin du DLC.",
    "en": "Use this after finishing the \"Trunks - The Warrior of Hope\" DLC. Specifically after making it past the DLC credits cutscene!"
  }
}
```

Deux détails :

- Les dossiers 10 et 11 commencent tous deux par « Trunks ». C'est le **numéro** dans le motif (`10\\s*-\\s*Trunks`, `11\\s*-\\s*Trunks`) qui les départage.
- Dans du JSON, un guillemet à l'intérieur d'un texte s'écrit `\"`.

Comme ce fichier est copié à côté de l'exécutable, **un utilisateur peut lui-même le corriger ou le compléter** si le mod évolue, sans attendre une nouvelle version de l'application.

## 4. Une notion de test : `[MemberData]`

Le jalon 1 a présenté `[Theory]` avec `[InlineData]`, qui convient pour deux ou trois valeurs simples. Pour dix-huit lignes de trois valeurs, on fournit plutôt les données par une **propriété**. Voir [SoundtrackServiceTests.cs](../../tests/KakarotOstManager.Tests/SoundtrackServiceTests.cs) :

```csharp
public static TheoryData<string, string, SoundtrackCategory> CurrentPackFolders => new()
{
    { "DBZ - Kakarot Soundtrack Mod 01 - Raditz", "01-raditz", SoundtrackCategory.Main },
    …
    { "DBZ - Kakarot Soundtrack Mod 16 - Daima Part 2", "16-daima-2", SoundtrackCategory.Dlc },
};

[Theory]
[MemberData(nameof(CurrentPackFolders))]
public void Chaque_dossier_du_pack_actuel_est_reconnu(string folderName, string expectedId, SoundtrackCategory expectedCategory)
```

xUnit exécute le test une fois par ligne, soit dix-huit tests. Si un motif est mal écrit, le rapport désigne précisément le dossier fautif.

Des garde-fous ont aussi été ajoutés sur le fichier livré, dans [SoundtrackCatalogTests.cs](../../tests/KakarotOstManager.Tests/SoundtrackCatalogTests.cs) : le catalogue contient bien 18 bandes-son dont 9 de DLC, chacune a son indication dans les deux langues, les numéros sont uniques, et l'ordre du fichier est celui de l'affichage. La suite compte maintenant 168 tests.

## 5. Un ajustement d'affichage

Le titre « 11 — Trunks - The Warrior of Hope - Epilogue » est trop long pour la liste. Dans [MainWindow.xaml](../../src/KakarotOstManager/Views/MainWindow.xaml), chaque ligne est désormais un `DockPanel` :

```xml
<DockPanel ToolTip="{Binding Title}">
    <TextBlock DockPanel.Dock="Right" …>● installée</TextBlock>
    <TextBlock Text="{Binding Title}" TextTrimming="CharacterEllipsis" />
</DockPanel>
```

- Le repère « ● installée » est **ancré à droite** et garde toute sa place.
- Le titre occupe le reste, et `TextTrimming` l'abrège par « … » s'il déborde.
- L'info-bulle (`ToolTip`) donne le titre entier au survol.

Le précédent conteneur, un `StackPanel` horizontal, n'aurait pas permis cela : il laisse à chaque enfant toute la largeur qu'il demande, donc rien n'y est jamais « trop long ».

## 6. Un piège pratique : l'exécutable verrouillé

Pendant ce travail, `dotnet build` a échoué avec ce message :

```text
error MSB3027: Impossible de copier "…\apphost.exe" vers "bin\Debug\…\KakarotOstManager.exe".
Le fichier est verrouillé par : "KakarotOstManager (10820)"
```

L'application était ouverte chez toi. Windows interdit de remplacer un programme en cours d'exécution, donc le compilateur ne peut pas écrire le nouvel exécutable. **Il suffit de fermer l'application avant de recompiler.**

C'est la même mécanique que celle qui protège `Bgm.awb` quand le jeu tourne (jalon 2), vue cette fois du côté du développeur.

## 7. Points ouverts

- **Les traductions françaises des indications sont à relire.**
- **Les noms sont ceux du mod, en anglais, dans les deux langues.** C'est cohérent avec les dossiers que l'utilisateur voit sur son disque. Les titres officiels français des DLC pourront les remplacer si tu le souhaites.
- **Le nom du processus du jeu et le lancement par Steam** restent à vérifier sur le jeu installé, d'autant que la version HD possède son propre exécutable.
