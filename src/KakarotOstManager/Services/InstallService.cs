using System.IO;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>
/// Installe une bande-son dans le jeu ou restaure la musique originale.
/// C'est le seul endroit de l'application qui modifie le Bgm.awb du jeu.
/// </summary>
public sealed class InstallService(
    GameService gameService,
    BackupService backupService,
    FileCopyService copier,
    FingerprintService fingerprints,
    SettingsService settingsService)
{
    /// <summary>
    /// Détermine ce que contient réellement le dossier BGM du jeu, en
    /// comparant les empreintes. Ne modifie rien.
    /// </summary>
    public async Task<InstalledState> IdentifyInstalledAsync(
        AppSettings settings,
        IReadOnlyList<Soundtrack> pack,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.GamePath) || settings.GameVersion is null)
        {
            return InstalledState.NoGameFile;
        }

        GameVersion version = settings.GameVersion.Value;
        string gameFile = gameService.GetBgmFilePath(settings.GamePath, version);
        return await ClassifyAsync(gameFile, version, pack, settings.CurrentSoundtrackId, cancellationToken);
    }

    /// <summary>
    /// Remplace le Bgm.awb du jeu par celui de <paramref name="soundtrack"/>.
    /// Un échec, quel qu'il soit, laisse le fichier du jeu tel qu'il était.
    /// </summary>
    /// <param name="pack">Toutes les bandes-son du pack, pour reconnaître le fichier en place.</param>
    public async Task<OperationResult> InstallAsync(
        AppSettings settings,
        Soundtrack soundtrack,
        IReadOnlyList<Soundtrack> pack,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<CheckResult> checks = RunGameChecks(settings);
        checks.Add(new CheckResult(CheckId.SoundtrackDirectory, Directory.Exists(settings.SoundtrackPath)));
        checks.Add(new CheckResult(CheckId.SourceFile, File.Exists(soundtrack.BgmFilePath)));
        checks.Add(new CheckResult(CheckId.GameNotRunning, !gameService.IsKakarotRunning()));
        if (checks.Any(check => !check.Passed))
        {
            return new OperationResult(OperationStatus.PrecheckFailed, checks);
        }

        GameVersion version = settings.GameVersion!.Value;
        string gameFile = gameService.GetBgmFilePath(settings.GamePath!, version);

        try
        {
            progress?.Report(new InstallProgress(InstallStep.Analyzing, 0));
            InstalledState current = await ClassifyAsync(gameFile, version, pack, settings.CurrentSoundtrackId, cancellationToken);

            // Un fichier non reconnu est peut-être la musique originale : il
            // devient la sauvegarde avant toute autre action. Si cette étape
            // échoue, on s'arrête là.
            if (current.Kind == InstalledKind.Unknown)
            {
                try
                {
                    string backupHash = await backupService.BackupVanillaAsync(
                        gameFile, version, ForStep(progress, InstallStep.BackingUp), cancellationToken);
                    fingerprints.Remember(backupService.GetBackupFile(version), backupHash);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    checks.Add(new CheckResult(CheckId.OriginalFileSafe, false));
                    return new OperationResult(OperationStatus.BackupFailed, checks, ex);
                }
            }

            checks.Add(new CheckResult(CheckId.OriginalFileSafe, true));

            // Inutile de recopier 400 Mo si la bonne bande-son est déjà en place.
            if (current.Soundtrack?.Id != soundtrack.Id)
            {
                await ReplaceGameFileAsync(soundtrack.BgmFilePath, gameFile, progress, cancellationToken);
            }

            Remember(settings, soundtrack.Id);
            progress?.Report(new InstallProgress(InstallStep.Done, 1));
            return new OperationResult(OperationStatus.Success, checks);
        }
        catch (Exception ex) when (ToFailureStatus(ex) is { } status)
        {
            return new OperationResult(status, checks, ex);
        }
    }

    /// <summary>Remet en place la musique originale à partir de la sauvegarde.</summary>
    public async Task<OperationResult> RestoreVanillaAsync(
        AppSettings settings,
        IReadOnlyList<Soundtrack> pack,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<CheckResult> checks = RunGameChecks(settings);
        bool backupExists = settings.GameVersion is not null && backupService.BackupExists(settings.GameVersion.Value);
        checks.Add(new CheckResult(CheckId.VanillaBackupAvailable, backupExists));
        checks.Add(new CheckResult(CheckId.GameNotRunning, !gameService.IsKakarotRunning()));
        if (checks.Any(check => !check.Passed))
        {
            return new OperationResult(OperationStatus.PrecheckFailed, checks);
        }

        GameVersion version = settings.GameVersion!.Value;
        string gameFile = gameService.GetBgmFilePath(settings.GamePath!, version);

        try
        {
            progress?.Report(new InstallProgress(InstallStep.Analyzing, 0));
            InstalledState current = await ClassifyAsync(gameFile, version, pack, settings.CurrentSoundtrackId, cancellationToken);

            // Un fichier non reconnu (par exemple une nouvelle version de la
            // musique originale après une mise à jour du jeu) est mis de côté
            // plutôt que perdu.
            if (current.Kind == InstalledKind.Unknown)
            {
                try
                {
                    await backupService.ArchiveAsync(
                        gameFile, version, ForStep(progress, InstallStep.BackingUp), cancellationToken);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new OperationResult(OperationStatus.BackupFailed, checks, ex);
                }
            }

            if (current.Kind != InstalledKind.Vanilla)
            {
                await ReplaceGameFileAsync(backupService.GetBackupFile(version), gameFile, progress, cancellationToken);
            }

            Remember(settings, AppSettings.VanillaId);
            progress?.Report(new InstallProgress(InstallStep.Done, 1));
            return new OperationResult(OperationStatus.Success, checks);
        }
        catch (Exception ex) when (ToFailureStatus(ex) is { } status)
        {
            return new OperationResult(status, checks, ex);
        }
    }

    private List<CheckResult> RunGameChecks(AppSettings settings)
    {
        bool directoryFound = !string.IsNullOrWhiteSpace(settings.GamePath) && Directory.Exists(settings.GamePath);
        bool versionChosen = settings.GameVersion is not null;
        bool bgmDirectoryFound = directoryFound && versionChosen
            && Directory.Exists(gameService.GetBgmDirectory(settings.GamePath!, settings.GameVersion!.Value));

        return
        [
            new CheckResult(CheckId.GameDirectory, directoryFound),
            new CheckResult(CheckId.GameVersion, versionChosen),
            new CheckResult(CheckId.BgmDirectory, bgmDirectoryFound),
        ];
    }

    private async Task<InstalledState> ClassifyAsync(
        string gameFile,
        GameVersion version,
        IReadOnlyList<Soundtrack> pack,
        string? likelySoundtrackId,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(gameFile))
        {
            return InstalledState.NoGameFile;
        }

        string current = await fingerprints.GetSha256Async(gameFile, cancellationToken: cancellationToken);

        if (backupService.BackupExists(version))
        {
            string backup = await fingerprints.GetSha256Async(backupService.GetBackupFile(version), cancellationToken: cancellationToken);
            if (backup == current)
            {
                return InstalledState.Vanilla;
            }
        }

        // La bande-son mémorisée est la plus probable : la tester en premier
        // évite, la plupart du temps, de calculer l'empreinte de tout le pack.
        foreach (Soundtrack candidate in pack.OrderByDescending(soundtrack => soundtrack.Id == likelySoundtrackId))
        {
            if (!File.Exists(candidate.BgmFilePath))
            {
                continue;
            }

            string hash = await fingerprints.GetSha256Async(candidate.BgmFilePath, cancellationToken: cancellationToken);
            if (hash == current)
            {
                return InstalledState.Of(candidate);
            }
        }

        return InstalledState.Unknown;
    }

    private async Task ReplaceGameFileAsync(
        string sourceFile, string gameFile, IProgress<InstallProgress>? progress, CancellationToken cancellationToken)
    {
        string hash = await copier.CopyVerifiedAsync(
            sourceFile,
            gameFile,
            ForStep(progress, InstallStep.Copying),
            ForStep(progress, InstallStep.Verifying),
            cancellationToken);

        fingerprints.Remember(gameFile, hash);
    }

    private void Remember(AppSettings settings, string soundtrackId)
    {
        settings.CurrentSoundtrackId = soundtrackId;
        settingsService.Save(settings);
    }

    /// <summary>Traduit une erreur attendue en statut ; <c>null</c> pour une erreur imprévue, qui remonte.</summary>
    private static OperationStatus? ToFailureStatus(Exception exception) => exception switch
    {
        OperationCanceledException => OperationStatus.Cancelled,
        FileVerificationException => OperationStatus.VerificationFailed,
        IOException or UnauthorizedAccessException => OperationStatus.CopyFailed,
        _ => null,
    };

    private static IProgress<double>? ForStep(IProgress<InstallProgress>? progress, InstallStep step) =>
        progress is null ? null : new StepProgress(progress, step);

    /// <summary>Transforme l'avancement d'une étape (0 à 1) en <see cref="InstallProgress"/>.</summary>
    private sealed class StepProgress(IProgress<InstallProgress> target, InstallStep step) : IProgress<double>
    {
        public void Report(double value) => target.Report(new InstallProgress(step, value));
    }
}
