namespace KakarotOstManager.Models;

/// <summary>Contrôles effectués avant de toucher au fichier du jeu.</summary>
public enum CheckId
{
    /// <summary>Le dossier du jeu est renseigné et existe.</summary>
    GameDirectory,

    /// <summary>L'édition du jeu (Standard ou Remaster) est choisie.</summary>
    GameVersion,

    /// <summary>Le dossier BGM du jeu existe pour cette édition.</summary>
    BgmDirectory,

    /// <summary>Le dossier du pack est renseigné et existe.</summary>
    SoundtrackDirectory,

    /// <summary>Le Bgm.awb de la bande-son à installer existe.</summary>
    SourceFile,

    /// <summary>Le jeu n'est pas en cours d'exécution.</summary>
    GameNotRunning,

    /// <summary>
    /// La musique originale ne risque rien : elle est déjà sauvegardée,
    /// vient de l'être, ou n'est pas le fichier actuellement en place.
    /// </summary>
    OriginalFileSafe,

    /// <summary>Une sauvegarde de la musique originale existe (pour restaurer).</summary>
    VanillaBackupAvailable,
}

public sealed record CheckResult(CheckId Id, bool Passed);
