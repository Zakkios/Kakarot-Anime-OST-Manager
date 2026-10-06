using System.Globalization;
using System.IO;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Met à l'abri la musique originale du jeu.</summary>
public sealed class BackupService(AppPaths paths, FileCopyService copier)
{
    public string GetBackupFile(GameVersion version) =>
        Path.Combine(paths.GetVanillaBackupDirectory(version), GameService.BgmFileName);

    public bool BackupExists(GameVersion version) => File.Exists(GetBackupFile(version));

    /// <summary>
    /// Enregistre <paramref name="gameBgmFile"/> comme musique originale. Une
    /// sauvegarde précédente n'est jamais perdue : elle est mise de côté sous
    /// un nom daté, et remise en place si la nouvelle sauvegarde échoue.
    /// </summary>
    /// <returns>L'empreinte SHA-256 du fichier sauvegardé.</returns>
    public async Task<string> BackupVanillaAsync(
        string gameBgmFile,
        GameVersion version,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string backupFile = GetBackupFile(version);
        Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);

        string? previousBackup = null;
        if (File.Exists(backupFile))
        {
            previousBackup = NewArchiveFile(version);
            File.Move(backupFile, previousBackup);
        }

        try
        {
            return await copier.CopyVerifiedAsync(gameBgmFile, backupFile, progress, cancellationToken: cancellationToken);
        }
        catch when (previousBackup is not null)
        {
            File.Move(previousBackup, backupFile);
            throw;
        }
    }

    /// <summary>
    /// Conserve une copie datée d'un fichier que l'application ne reconnaît
    /// pas, avant qu'il ne soit écrasé.
    /// </summary>
    /// <returns>Le chemin de la copie.</returns>
    public async Task<string> ArchiveAsync(
        string file,
        GameVersion version,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string archiveFile = NewArchiveFile(version);
        await copier.CopyVerifiedAsync(file, archiveFile, progress, cancellationToken: cancellationToken);
        return archiveFile;
    }

    private string NewArchiveFile(GameVersion version)
    {
        string directory = paths.GetArchiveDirectory(version);
        Directory.CreateDirectory(directory);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string file = Path.Combine(directory, $"Bgm.{stamp}.awb");
        for (int suffix = 2; File.Exists(file); suffix++)
        {
            file = Path.Combine(directory, $"Bgm.{stamp}_{suffix}.awb");
        }

        return file;
    }
}
