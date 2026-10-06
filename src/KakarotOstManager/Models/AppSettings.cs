namespace KakarotOstManager.Models;

/// <summary>
/// Paramètres mémorisés d'un lancement à l'autre (fichier settings.json).
/// Tout est facultatif : au premier lancement, rien n'est encore connu.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Valeur de <see cref="CurrentSoundtrackId"/> quand la musique originale est en place.</summary>
    public const string VanillaId = "vanilla";

    /// <summary>Dossier d'installation du jeu, par exemple « …\DRAGON BALL Z KAKAROT ».</summary>
    public string? GamePath { get; set; }

    /// <summary>Dossier du pack, celui dont les sous-dossiers contiennent un Bgm.awb.</summary>
    public string? SoundtrackPath { get; set; }

    public GameVersion? GameVersion { get; set; }

    /// <summary>Identifiant de la dernière bande-son installée par l'application.</summary>
    public string? CurrentSoundtrackId { get; set; }

    /// <summary>« fr » ou « en ». Tant que rien n'est choisi, la langue de Windows est utilisée.</summary>
    public string? Language { get; set; }
}
