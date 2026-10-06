using System.IO;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>
/// Emplacements des fichiers que l'application écrit pour son propre compte.
/// Le dossier racine est un paramètre afin que les tests puissent travailler
/// dans un dossier temporaire.
/// </summary>
public sealed class AppPaths(string rootDirectory)
{
    /// <summary>%LOCALAPPDATA%\KakarotAnimeOstManager</summary>
    public static AppPaths Default { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KakarotAnimeOstManager"));

    public string RootDirectory { get; } = rootDirectory;

    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public string FingerprintCacheFile => Path.Combine(RootDirectory, "fingerprints.json");

    /// <summary>Dossier de la sauvegarde de la musique originale. Chaque édition du jeu a la sienne.</summary>
    public string GetVanillaBackupDirectory(GameVersion version) =>
        Path.Combine(RootDirectory, "Backups", "Vanilla", version.ToString());

    /// <summary>Dossier des fichiers mis de côté avant d'être écrasés.</summary>
    public string GetArchiveDirectory(GameVersion version) =>
        Path.Combine(RootDirectory, "Backups", "Archive", version.ToString());
}
