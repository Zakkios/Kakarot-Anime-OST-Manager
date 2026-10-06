using System.IO;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Résultat de la vérification du dossier du jeu.</summary>
public enum GameDirectoryStatus
{
    Valid,

    /// <summary>Aucun chemin n'a encore été choisi.</summary>
    NotSet,

    /// <summary>Le dossier choisi n'existe pas.</summary>
    DirectoryNotFound,

    /// <summary>Le dossier existe mais ne contient pas le dossier BGM attendu.</summary>
    BgmDirectoryNotFound,
}

/// <summary>Tout ce qui concerne l'installation du jeu sur le disque.</summary>
public sealed class GameService
{
    public const string BgmFileName = "Bgm.awb";

    /// <summary>
    /// Dossier dans lequel le jeu lit Bgm.awb, calculé à partir du dossier
    /// d'installation. L'utilisateur n'a donc jamais à le chercher lui-même.
    /// </summary>
    public string GetBgmDirectory(string gamePath, GameVersion version) => version switch
    {
        GameVersion.Standard => Path.Combine(gamePath, "AT", "Content", "Sound", "Bgm"),
        GameVersion.Remaster => Path.Combine(gamePath, "dlc", "Remaster", "AT", "Content", "Sound", "Bgm"),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
    };

    public string GetBgmFilePath(string gamePath, GameVersion version) =>
        Path.Combine(GetBgmDirectory(gamePath, version), BgmFileName);

    /// <summary>
    /// Devine l'édition installée d'après les dossiers présents. Renvoie
    /// <c>null</c> si aucun dossier BGM n'est trouvé.
    /// </summary>
    public GameVersion? DetectVersion(string? gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            return null;
        }

        // Le dossier Remaster s'ajoute au dossier standard : s'il est là,
        // c'est lui que le jeu utilise.
        if (Directory.Exists(GetBgmDirectory(gamePath, GameVersion.Remaster)))
        {
            return GameVersion.Remaster;
        }

        if (Directory.Exists(GetBgmDirectory(gamePath, GameVersion.Standard)))
        {
            return GameVersion.Standard;
        }

        return null;
    }

    public GameDirectoryStatus ValidateGameDirectory(string? gamePath, GameVersion version)
    {
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            return GameDirectoryStatus.NotSet;
        }

        if (!Directory.Exists(gamePath))
        {
            return GameDirectoryStatus.DirectoryNotFound;
        }

        return Directory.Exists(GetBgmDirectory(gamePath, version))
            ? GameDirectoryStatus.Valid
            : GameDirectoryStatus.BgmDirectoryNotFound;
    }
}
