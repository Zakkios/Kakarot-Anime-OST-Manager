# Kakarot Anime OST Manager

## 1. Objectif

Créer une petite application Windows permettant de gérer facilement les différentes bandes-son du mod anime de **Dragon Ball Z: Kakarot** qui est à cette adresse : https://www.nexusmods.com/dragonballzkakarot/mods/94.

Le problème actuel est que le mod fournit plusieurs variantes du fichier `Bgm.awb`, correspondant aux différents arcs de l'histoire.

Pour passer d'un arc à l'autre, l'utilisateur doit actuellement :

1. quitter Kakarot ;
2. retrouver le bon fichier du mod ;
3. retrouver le dossier BGM du jeu ;
4. remplacer manuellement `Bgm.awb` ;
5. relancer le jeu.

L'objectif de l'application est de transformer cette manipulation en quelques clics.

---

# 2. Workflow utilisateur idéal

Exemple réel : passage à Namek.

L'utilisateur joue à Kakarot.

Il arrive au moment où l'histoire va passer à l'arc Namek.

Il sauvegarde et quitte le jeu.

Il ouvre **Kakarot Anime OST Manager**.

L'application connaît déjà :

- le chemin d'installation de Kakarot ;
- le chemin du répertoire contenant les BGM du mod ;
- la version du jeu utilisée ;
- la bande-son actuellement installée.

L'utilisateur sélectionne :

**03 — Namek**

L'application lui affiche également :

> À activer juste avant le départ pour Namek.

L'utilisateur clique sur :

**Appliquer**

L'application :

1. vérifie que Kakarot est fermé ;
2. vérifie que les fichiers existent ;
3. récupère le `Bgm.awb` correspondant à Namek ;
4. copie le fichier dans le répertoire BGM du jeu ;
5. remplace l'ancien fichier ;
6. vérifie que l'opération s'est correctement déroulée ;
7. mémorise que Namek est maintenant la bande-son active.

Puis :

> ✓ Bande-son Namek installée avec succès.  
> Vous pouvez lancer Dragon Ball Z: Kakarot.

Un bouton permet ensuite de lancer directement le jeu.

---

# 3. Interface principale

Exemple :

```text
KAKAROT ANIME OST MANAGER

Jeu
C:\Steam\steamapps\common\DRAGON BALL Z KAKAROT
[Parcourir]

Pack OST
D:\Mods\Kakarot Anime Soundtrack
[Parcourir]

Version du jeu
(•) HD / Remaster
( ) Standard


HISTOIRE PRINCIPALE

○ 01 — Raditz
○ 02 — Saiyans
○ 02.5 — Vegeta
● 03 — Namek
○ 04 — Androids
○ 05 — Cell
○ 06 — Majin Buu


DLC

○ Trunks — Warrior of Hope
○ Trunks — Epilogue
○ Bardock
○ 23rd Tenkaichi Budokai
○ Goku's Next Journey
○ DAIMA
...

--------------------------------

03 — NAMEK

Quand l'activer :
Juste avant le départ pour Namek.

Bande-son actuellement installée :
03 — Namek

[ APPLIQUER ]

[ Restaurer OST originale ]

[ Lancer Dragon Ball Z: Kakarot ]
```

---

# 4. Informations de progression dans l'histoire

C'est une fonctionnalité importante de l'application.

Chaque bande-son doit avoir une indication expliquant précisément **quand l'installer**.

Exemple :

```text
03 — Namek

À installer :
Juste avant le départ de Gohan, Krillin et Bulma vers Namek.

Bande-son précédente :
02.5 — Vegeta

Bande-son suivante :
04 — Androids
```

L'objectif est que l'utilisateur n'ait jamais besoin de retourner sur Nexus Mods pour savoir quel fichier utiliser.

Les informations peuvent être affichées directement lorsqu'on sélectionne un arc.

---

# 5. Gestion des spoilers

Comme les indications peuvent révéler des événements de l'histoire, prévoir éventuellement deux niveaux.

### Mode simple

```text
À installer :
Après l'arc Saiyan, avant le départ pour Namek.
```

### Mode précis

```text
À installer :
Juste avant le départ de Gohan, Krillin et Bulma pour Namek.
```

Une option pourrait permettre :

```text
☑ Afficher les indications détaillées
```

Ce n'est pas indispensable pour la V1.

---

# 6. Organisation des fichiers OST

L'application ne devrait pas avoir la liste des arcs codée directement dans le programme.

Elle devrait idéalement scanner le dossier fourni par l'utilisateur.

Exemple :

```text
Kakarot Anime OST/
│
├── 01 - Raditz/
│   └── Bgm.awb
│
├── 02 - Saiyans/
│   └── Bgm.awb
│
├── 02.5 - Vegeta/
│   └── Bgm.awb
│
├── 03 - Namek/
│   └── Bgm.awb
│
├── 04 - Androids/
│   └── Bgm.awb
│
├── 05 - Cell/
│   └── Bgm.awb
│
└── ...
```

L'application détecte automatiquement les dossiers contenant un `Bgm.awb`.

Avantage :

si le créateur du mod ajoute plus tard :

```text
17 - Nouveau DLC/
    Bgm.awb
```

l'application peut le détecter sans modification majeure du programme.

---

# 7. Métadonnées des arcs

Les informations concernant l'histoire peuvent être conservées dans un fichier séparé.

Par exemple :

```text
soundtracks.json
```

Conceptuellement :

```json
{
  "03 - Namek": {
    "displayName": "Namek",
    "category": "Main Story",
    "order": 3,
    "activationHint": "Juste avant le départ pour Namek.",
    "previous": "02.5 - Vegeta",
    "next": "04 - Androids"
  }
}
```

Cela permet de modifier les indications sans toucher au code C#.

---

# 8. Paramètres mémorisés

Les chemins ne doivent être demandés qu'à la première utilisation.

Exemple de configuration :

```json
{
  "gamePath": "C:\\Steam\\steamapps\\common\\DRAGON BALL Z KAKAROT",
  "soundtrackPath": "D:\\Mods\\Kakarot Anime OST",
  "gameVersion": "Remaster",
  "currentSoundtrack": "03 - Namek"
}
```

L'application mémorise :

- chemin du jeu ;
- chemin du mod ;
- version du jeu ;
- OST actuellement sélectionnée ;
- éventuellement les préférences d'affichage.

---

# 9. Détection du chemin BGM

Le chemin final dépend de la version du jeu.

L'application ne devrait donc pas demander à l'utilisateur de sélectionner directement le dossier contenant `Bgm.awb`.

Il sélectionne simplement :

```text
...\DRAGON BALL Z KAKAROT
```

Puis l'application construit automatiquement le chemin nécessaire.

Concept :

```text
GamePath
    ↓
Version sélectionnée
    ↓
Chemin BGM calculé automatiquement
```

Cela évite les erreurs utilisateur.

---

# 10. Sauvegarde de la musique originale

Fonction indispensable.

À la première utilisation, avant toute modification :

```text
Backups/
└── Vanilla/
    └── Bgm.awb
```

L'application vérifie d'abord si une sauvegarde existe.

Si elle n'existe pas :

```text
Bgm.awb du jeu
        ↓
Backups/Vanilla/Bgm.awb
```

Puis seulement ensuite le mod est installé.

---

# 11. Restaurer la musique originale

Bouton :

**Restaurer OST originale**

Fonctionnement :

```text
Backups/Vanilla/Bgm.awb
        ↓
répertoire BGM de Kakarot
```

Puis :

> ✓ Bande-son originale restaurée.

L'application indique alors :

```text
Bande-son active :
Vanilla
```

---

# 12. Vérifier que Kakarot est fermé

Avant toute modification, l'application doit vérifier si le processus de Kakarot fonctionne.

Si le jeu tourne :

```text
⚠ Dragon Ball Z: Kakarot est actuellement lancé.

La bande-son ne peut pas être modifiée pendant
que le jeu est en cours d'exécution.

Fermez le jeu puis cliquez sur Réessayer.

[ Réessayer ]
```

Aucun fichier ne doit être remplacé tant que Kakarot tourne.

---

# 13. Vérifications avant installation

Avant de copier quoi que ce soit :

```text
✓ Répertoire Kakarot valide
✓ Version du jeu définie
✓ Répertoire du mod valide
✓ Bgm.awb source trouvé
✓ Répertoire BGM du jeu trouvé
✓ Sauvegarde Vanilla disponible
✓ Kakarot n'est pas lancé
```

Si une vérification échoue, l'installation est annulée.

---

# 14. Installation d'une OST

Exemple avec Namek :

Source :

```text
D:\Mods\Kakarot OST\
03 - Namek\
Bgm.awb
```

Destination :

```text
Kakarot\
...\Sound\Bgm\
Bgm.awb
```

L'application :

```text
Copy(source, destination, overwrite: true)
```

Puis vérifie que le fichier de destination existe.

---

# 15. Écran de chargement

Même si la copie est très rapide, montrer clairement que quelque chose se passe.

```text
Installation de Namek...

██████████████████ 100 %

✓ Installation terminée.
```

Éviter cependant un faux chargement artificiellement long.

---

# 16. Confirmation

Après installation :

```text
✓ Namek installé avec succès.

Bande-son active :
03 — Namek

Vous pouvez maintenant lancer
Dragon Ball Z: Kakarot.

[ LANCER KAKAROT ]
```

---

# 17. Lancement du jeu

Ajouter :

**Lancer Dragon Ball Z: Kakarot**

Idéalement lancer le jeu via Steam plutôt qu'en démarrant directement l'exécutable.

Cela permet de conserver :

- Steam Overlay ;
- Steam Cloud ;
- temps de jeu ;
- paramètres Steam ;
- comportement normal du jeu.

---

# 18. Bande-son actuellement active

L'application doit toujours afficher clairement :

```text
Bande-son active :

03 — Namek
```

Pour éviter de se demander :

> « Quel fichier avais-je installé la dernière fois ? »

La valeur est mémorisée dans les paramètres.

---

# 19. Liste dynamique des OST

Lors du démarrage :

```text
ScanSoundtracks()
```

L'application parcourt le dossier du mod.

Elle recherche les dossiers contenant :

```text
Bgm.awb
```

Elle construit ensuite automatiquement la liste.

Cela évite d'avoir à mettre à jour le programme lorsque de nouvelles OST apparaissent.

---

# 20. Architecture C#

Pour une première version :

**C# + .NET + WPF**

WPF est probablement préférable à WinUI pour ce projet :

- très documenté ;
- stable ;
- simple pour un petit logiciel Windows ;
- excellent support C# ;
- facile à distribuer ;
- parfaitement suffisant visuellement.

Architecture recommandée :

```text
KakarotOSTManager
│
├── Models
│   ├── AppSettings.cs
│   └── Soundtrack.cs
│
├── Services
│   ├── SettingsService.cs
│   ├── SoundtrackService.cs
│   ├── GameService.cs
│   └── BackupService.cs
│
├── ViewModels
│   └── MainViewModel.cs
│
├── Views
│   └── MainWindow.xaml
│
└── Data
    └── soundtracks.json
```

On peut utiliser une architecture MVVM légère.

Pas besoin d'une architecture complexe.

---

# 21. Modèle Soundtrack

Concept :

```csharp
Soundtrack
{
    Id
    Name
    Category
    Order
    FolderPath
    ActivationHint
    PreviousSoundtrack
    NextSoundtrack
}
```

Exemple :

```text
Id:
03-namek

Name:
Namek

Category:
Main Story

Order:
3

Folder:
03 - Namek

ActivationHint:
Juste avant le départ pour Namek.

Previous:
Vegeta

Next:
Androids
```

---

# 22. Services principaux

### SettingsService

Responsabilités :

```text
LoadSettings()
SaveSettings()
```

---

### SoundtrackService

Responsabilités :

```text
FindSoundtracks()
GetSoundtrackInfo()
InstallSoundtrack()
```

---

### BackupService

Responsabilités :

```text
BackupVanillaBgm()
RestoreVanillaBgm()
BackupExists()
```

---

### GameService

Responsabilités :

```text
IsKakarotRunning()
ValidateGameDirectory()
GetBgmDirectory()
LaunchKakarot()
```

---

# 23. Fonctions principales

Version simplifiée :

```text
LoadSettings()

SaveSettings()

ValidateGamePath()

ValidateSoundtrackPath()

FindSoundtracks()

GetBgmPath()

IsKakarotRunning()

BackupVanillaBgm()

InstallSoundtrack(soundtrack)

RestoreVanilla()

LaunchKakarot()
```

---

# 24. Gestion des erreurs

Cas à gérer :

### Mauvais dossier du jeu

```text
Le dossier sélectionné ne semble pas contenir
Dragon Ball Z: Kakarot.
```

### Mauvais dossier OST

```text
Aucun fichier Bgm.awb n'a été trouvé.
```

### Jeu lancé

```text
Kakarot doit être fermé avant de modifier
la bande-son.
```

### Fichier manquant

```text
Le fichier Bgm.awb de Namek est introuvable.
```

### Erreur de copie

```text
Impossible de remplacer le fichier.

Vérifiez les permissions du dossier.
```

---

# 25. Ne jamais supprimer le fichier original sans sauvegarde

Principe important :

```text
SI backup Vanilla inexistant
    ALORS créer backup

SI création backup échoue
    ALORS annuler installation
```

Le logiciel ne doit jamais écraser l'OST originale si aucune sauvegarde valide n'existe.

---

# 26. MVP — Première version

La V0.1 doit rester extrêmement simple.

Fonctions :

- sélectionner le dossier Kakarot ;
- sélectionner le dossier du mod ;
- mémoriser les deux chemins ;
- détecter les OST ;
- afficher les OST ;
- afficher l'indication de changement d'arc ;
- sélectionner une OST ;
- détecter si Kakarot tourne ;
- sauvegarder le BGM original ;
- installer le nouveau `Bgm.awb` ;
- restaurer Vanilla ;
- afficher l'OST active ;
- afficher confirmation ou erreur.

C'est suffisant pour avoir une application réellement utilisable.

---

# 27. V0.2

Ajouter :

- meilleure interface graphique ;
- catégories Histoire / DLC ;
- icônes ;
- barre de progression ;
- bouton Lancer Kakarot ;
- indication précédente / suivante ;
- détection automatique de la version du jeu si possible.

---

# 28. V0.3

Possibilités :

- thème Dragon Ball ;
- images des différents arcs ;
- recherche d'OST ;
- mode sombre ;
- informations détaillées sur chaque arc ;
- système anti-spoiler ;
- détection d'une nouvelle version du pack OST ;
- historique des changements.

---

# 29. Idée avancée : timeline

À terme, l'application pourrait montrer :

```text
Raditz
   ↓
Saiyans
   ↓
Vegeta
   ↓
Namek        ← VOUS ÊTES ICI
   ↓
Androids
   ↓
Cell
   ↓
Majin Buu
```

Puis les DLC séparément.

Cela rendrait le guide particulièrement clair.

---

# 30. Idée avancée : bouton OST suivante

Une fois l'ordre connu :

```text
Bande-son active :
03 — Namek

Prochaine bande-son :
04 — Androids

Quand changer :
Après ...

[ INSTALLER L'OST SUIVANTE ]
```

Cela pourrait devenir encore plus pratique que de sélectionner manuellement un arc.

---

# 31. Ce que l'application ne fait pas

L'application n'a pas besoin :

- d'un serveur ;
- d'une base de données ;
- d'un compte utilisateur ;
- d'une connexion internet ;
- d'une API ;
- de modifier les sauvegardes Kakarot ;
- de modifier l'exécutable du jeu.

Elle fait essentiellement :

```text
GUI
+
gestion de fichiers
+
configuration
+
détection de processus
+
métadonnées sur les arcs
```

---

# 32. Philosophie du projet

L'application ne distribue pas elle-même la bande-son.

L'utilisateur télécharge normalement le mod.

L'application sert ensuite de **gestionnaire local** permettant de sélectionner et installer facilement les fichiers déjà présents sur la machine.

C'est important pour garder le projet simple et séparer :

```text
Le mod
≠
Le gestionnaire du mod
```

---

# 33. Expérience finale recherchée

À terme, l'utilisateur ne devrait plus jamais manipuler lui-même `Bgm.awb`.

Son expérience devrait être :

```text
Je viens d'arriver à Namek.

↓

Je quitte Kakarot.

↓

J'ouvre Kakarot Anime OST Manager.

↓

L'application m'indique :
"À partir de ce moment : Namek"

↓

Je sélectionne Namek.

↓

APPLIQUER

↓

✓ Namek installé.

↓

LANCER KAKAROT

↓

Je continue ma partie.
```

Le logiciel transforme donc une installation manuelle de mod en véritable **gestionnaire de bande-son par arc**.