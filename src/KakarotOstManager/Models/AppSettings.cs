namespace KakarotOstManager.Models;

/// <summary>
/// Paramètres mémorisés d'un lancement à l'autre (fichier settings.json).
/// Tout est facultatif : au premier lancement, rien n'est encore connu.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Dossier d'installation du jeu, par exemple « …\DRAGON BALL Z KAKAROT ».</summary>
    public string? GamePath { get; set; }

    /// <summary>Dossier du pack, celui dont les sous-dossiers contiennent un Bgm.awb.</summary>
    public string? SoundtrackPath { get; set; }

    public GameVersion? GameVersion { get; set; }

    /// <summary>Identifiant de la dernière bande-son installée par l'application.</summary>
    public string? CurrentSoundtrackId { get; set; }
}
