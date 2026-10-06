using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class InstallServiceTests
{
    private static void AssertCheck(OperationResult result, CheckId id, bool expected) =>
        Assert.Equal(expected, result.Checks.Single(check => check.Id == id).Passed);

    // ---------------------------------------------------------------- installation

    [Fact]
    public async Task La_premiere_installation_sauvegarde_l_original_puis_installe_la_bande_son()
    {
        using var env = new TestEnvironment();

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.All(result.Checks, check => Assert.True(check.Passed));
        Assert.Equal(TestEnvironment.ContentOf(env.Namek), env.GameFileContent);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), File.ReadAllText(env.BackupFile));
        Assert.Empty(env.LeftoverTempFiles());
    }

    [Fact]
    public async Task L_installation_memorise_la_bande_son_active_dans_les_parametres()
    {
        using var env = new TestEnvironment();

        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal("03-namek", env.Settings.CurrentSoundtrackId);
        Assert.Equal("03-namek", env.SettingsService.Load().CurrentSoundtrackId);
    }

    [Fact]
    public async Task Changer_de_bande_son_ne_touche_plus_a_la_sauvegarde()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Cell, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.ContentOf(env.Cell), env.GameFileContent);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), File.ReadAllText(env.BackupFile));
        Assert.False(Directory.Exists(env.ArchiveDirectory));
    }

    [Fact]
    public async Task Un_fichier_du_pack_deja_en_place_n_est_jamais_sauvegarde_comme_original()
    {
        // L'utilisateur avait installé Raditz à la main avant d'utiliser l'application.
        using var env = new TestEnvironment();
        File.Copy(env.Raditz.BgmFilePath, env.GameFile, overwrite: true);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.ContentOf(env.Namek), env.GameFileContent);
        Assert.False(env.Backups.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task Sans_Bgm_awb_dans_le_jeu_l_installation_se_fait_sans_sauvegarde()
    {
        using var env = new TestEnvironment();
        File.Delete(env.GameFile);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.ContentOf(env.Namek), env.GameFileContent);
        Assert.False(env.Backups.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task Reinstaller_la_bande_son_deja_en_place_ne_recopie_pas_le_fichier()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        DateTime writtenAt = File.GetLastWriteTimeUtc(env.GameFile);
        var progress = new RecordingProgress<InstallProgress>();

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack, progress);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(writtenAt, File.GetLastWriteTimeUtc(env.GameFile));
        Assert.DoesNotContain(progress.Values, value => value.Step == InstallStep.Copying);
    }

    [Fact]
    public async Task Apres_une_mise_a_jour_du_jeu_le_nouvel_original_est_sauvegarde_et_l_ancien_archive()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        // Steam remplace le fichier par une nouvelle version de la musique originale.
        env.ReplaceGameFileExternally(TestEnvironment.Content("musique originale v2"));

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Cell, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.Content("musique originale v2"), File.ReadAllText(env.BackupFile));
        string archived = Assert.Single(Directory.GetFiles(env.ArchiveDirectory));
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), File.ReadAllText(archived));
    }

    [Fact]
    public async Task La_version_Remaster_ecrit_dans_le_dossier_Remaster()
    {
        using var env = new TestEnvironment(GameVersion.Remaster);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Contains(Path.Combine("dlc", "Remaster"), env.GameFile);
        Assert.Equal(TestEnvironment.ContentOf(env.Namek), env.GameFileContent);
        Assert.True(env.Backups.BackupExists(GameVersion.Remaster));
        Assert.False(env.Backups.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task L_avancement_passe_par_toutes_les_etapes_dans_l_ordre()
    {
        using var env = new TestEnvironment();
        var progress = new RecordingProgress<InstallProgress>();

        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack, progress);

        var steps = progress.Values.Select(value => value.Step).Distinct();
        Assert.Equal(
            [InstallStep.Analyzing, InstallStep.BackingUp, InstallStep.Copying, InstallStep.Verifying, InstallStep.Done],
            steps);
        Assert.All(progress.Values, value => Assert.InRange(value.Fraction, 0, 1));
    }

    // ---------------------------------------------------------------- contrôles préalables

    [Fact]
    public async Task Rien_n_est_modifie_tant_que_le_jeu_tourne()
    {
        using var env = new TestEnvironment();
        env.StartGame();

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.GameNotRunning, expected: false);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        Assert.False(env.Backups.BackupExists(GameVersion.Standard));
        Assert.Null(env.Settings.CurrentSoundtrackId);
    }

    [Fact]
    public async Task Un_fichier_source_manquant_annule_l_installation()
    {
        using var env = new TestEnvironment();
        File.Delete(env.Namek.BgmFilePath);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.SourceFile, expected: false);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
    }

    [Fact]
    public async Task Un_dossier_de_jeu_inexistant_annule_l_installation()
    {
        using var env = new TestEnvironment();
        env.Settings.GamePath = env.Temp.Combine("pas-un-jeu");

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.GameDirectory, expected: false);
        AssertCheck(result, CheckId.BgmDirectory, expected: false);
    }

    [Fact]
    public async Task Une_version_du_jeu_non_choisie_annule_l_installation()
    {
        using var env = new TestEnvironment();
        string gameFile = env.GameFile;
        env.Settings.GameVersion = null;

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.GameDirectory, expected: true);
        AssertCheck(result, CheckId.GameVersion, expected: false);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), File.ReadAllText(gameFile));
    }

    [Fact]
    public async Task Un_dossier_BGM_absent_pour_la_version_choisie_annule_l_installation()
    {
        using var env = new TestEnvironment(GameVersion.Standard);
        env.Settings.GameVersion = GameVersion.Remaster;

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.BgmDirectory, expected: false);
    }

    [Fact]
    public async Task Un_dossier_de_pack_inexistant_annule_l_installation()
    {
        using var env = new TestEnvironment();
        env.Settings.SoundtrackPath = env.Temp.Combine("pas-un-pack");

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.SoundtrackDirectory, expected: false);
    }

    // ---------------------------------------------------------------- échecs en cours de route

    [Fact]
    public async Task Si_la_sauvegarde_echoue_l_original_n_est_pas_ecrase()
    {
        using var env = new TestEnvironment();
        // Un fichier occupe la place du dossier « Backups » : impossible d'y créer la sauvegarde.
        env.Temp.CreateFile("obstacle", "appdata", "Backups");

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.BackupFailed, result.Status);
        AssertCheck(result, CheckId.OriginalFileSafe, expected: false);
        Assert.NotNull(result.Error);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        Assert.Null(env.Settings.CurrentSoundtrackId);
    }

    [Fact]
    public async Task Si_la_sauvegarde_est_corrompue_l_original_n_est_pas_ecrase()
    {
        using var env = new TestEnvironment(hashService: new CorruptedCopyHashService());

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.BackupFailed, result.Status);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        Assert.False(env.Backups.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task Si_le_fichier_du_jeu_est_verrouille_il_reste_intact()
    {
        using var env = new TestEnvironment();
        File.Copy(env.Raditz.BgmFilePath, env.GameFile, overwrite: true);

        OperationResult result;
        // Comme le jeu, on garde le fichier ouvert : il reste lisible mais ne peut plus être remplacé.
        using (new FileStream(env.GameFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        }

        Assert.Equal(OperationStatus.CopyFailed, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(TestEnvironment.ContentOf(env.Raditz), env.GameFileContent);
        Assert.Empty(env.LeftoverTempFiles());
        Assert.Null(env.Settings.CurrentSoundtrackId);
    }

    [Fact]
    public async Task Si_la_copie_ne_correspond_pas_a_la_source_le_fichier_du_jeu_reste_intact()
    {
        using var env = new TestEnvironment(hashService: new CorruptedCopyHashService());
        // Raditz est déjà en place : aucune sauvegarde n'est nécessaire, on teste donc bien la copie finale.
        File.Copy(env.Raditz.BgmFilePath, env.GameFile, overwrite: true);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        Assert.Equal(OperationStatus.VerificationFailed, result.Status);
        Assert.IsType<FileVerificationException>(result.Error);
        Assert.Equal(TestEnvironment.ContentOf(env.Raditz), env.GameFileContent);
        Assert.Empty(env.LeftoverTempFiles());
    }

    [Fact]
    public async Task Une_installation_annulee_laisse_le_fichier_du_jeu_intact()
    {
        using var env = new TestEnvironment();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationResult result = await env.Installer.InstallAsync(
            env.Settings, env.Namek, env.Pack, cancellationToken: cancellation.Token);

        Assert.Equal(OperationStatus.Cancelled, result.Status);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        Assert.Empty(env.LeftoverTempFiles());
    }

    // ---------------------------------------------------------------- restauration

    [Fact]
    public async Task Restaurer_remet_la_musique_originale_en_place()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        OperationResult result = await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        Assert.Equal(AppSettings.VanillaId, env.SettingsService.Load().CurrentSoundtrackId);
        Assert.True(env.Backups.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task Restaurer_sans_sauvegarde_est_refuse()
    {
        using var env = new TestEnvironment();

        OperationResult result = await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.VanillaBackupAvailable, expected: false);
    }

    [Fact]
    public async Task Restaurer_est_refuse_tant_que_le_jeu_tourne()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        env.StartGame();

        OperationResult result = await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        Assert.Equal(OperationStatus.PrecheckFailed, result.Status);
        AssertCheck(result, CheckId.GameNotRunning, expected: false);
        Assert.Equal(TestEnvironment.ContentOf(env.Namek), env.GameFileContent);
    }

    [Fact]
    public async Task Restaurer_met_de_cote_un_fichier_non_reconnu_avant_de_l_ecraser()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        env.ReplaceGameFileExternally(TestEnvironment.Content("fichier venu d'ailleurs"));

        OperationResult result = await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), env.GameFileContent);
        string archived = Assert.Single(Directory.GetFiles(env.ArchiveDirectory));
        Assert.Equal(TestEnvironment.Content("fichier venu d'ailleurs"), File.ReadAllText(archived));
    }

    [Fact]
    public async Task On_peut_reinstaller_une_bande_son_apres_une_restauration()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        OperationResult result = await env.Installer.InstallAsync(env.Settings, env.Cell, env.Pack);

        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Equal(TestEnvironment.ContentOf(env.Cell), env.GameFileContent);
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), File.ReadAllText(env.BackupFile));
        Assert.False(Directory.Exists(env.ArchiveDirectory));
    }

    // ---------------------------------------------------------------- identification

    [Fact]
    public async Task Avant_toute_installation_le_fichier_du_jeu_n_est_pas_reconnu()
    {
        using var env = new TestEnvironment();

        InstalledState state = await env.Installer.IdentifyInstalledAsync(env.Settings, env.Pack);

        Assert.Equal(InstalledKind.Unknown, state.Kind);
    }

    [Fact]
    public async Task Apres_une_installation_la_bande_son_en_place_est_reconnue()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);

        InstalledState state = await env.Installer.IdentifyInstalledAsync(env.Settings, env.Pack);

        Assert.Equal(InstalledKind.Soundtrack, state.Kind);
        Assert.Equal("03-namek", state.Soundtrack?.Id);
    }

    [Fact]
    public async Task Apres_une_restauration_la_musique_originale_est_reconnue()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        await env.Installer.RestoreVanillaAsync(env.Settings, env.Pack);

        InstalledState state = await env.Installer.IdentifyInstalledAsync(env.Settings, env.Pack);

        Assert.Equal(InstalledKind.Vanilla, state.Kind);
    }

    [Fact]
    public async Task Un_remplacement_fait_a_la_main_est_detecte_meme_si_les_parametres_disent_autre_chose()
    {
        using var env = new TestEnvironment();
        await env.Installer.InstallAsync(env.Settings, env.Namek, env.Pack);
        // L'utilisateur copie Cell à la main, sans passer par l'application.
        env.ReplaceGameFileExternally(TestEnvironment.ContentOf(env.Cell));

        InstalledState state = await env.Installer.IdentifyInstalledAsync(env.Settings, env.Pack);

        Assert.Equal("03-namek", env.Settings.CurrentSoundtrackId);
        Assert.Equal("05-cell", state.Soundtrack?.Id);
    }

    [Fact]
    public async Task Sans_fichier_dans_le_jeu_l_etat_est_NoGameFile()
    {
        using var env = new TestEnvironment();
        File.Delete(env.GameFile);

        InstalledState state = await env.Installer.IdentifyInstalledAsync(env.Settings, env.Pack);

        Assert.Equal(InstalledKind.NoGameFile, state.Kind);
    }

    [Fact]
    public async Task Sans_jeu_configure_l_etat_est_NoGameFile()
    {
        using var env = new TestEnvironment();

        InstalledState state = await env.Installer.IdentifyInstalledAsync(new AppSettings(), env.Pack);

        Assert.Equal(InstalledKind.NoGameFile, state.Kind);
    }

    /// <summary>
    /// Simule un disque défaillant : la relecture d'un fichier fraîchement
    /// copié (suffixe .tmp) renvoie une empreinte fausse.
    /// </summary>
    private sealed class CorruptedCopyHashService : HashService
    {
        public override Task<string> ComputeSha256Async(
            string filePath, IProgress<double>? progress = null, CancellationToken cancellationToken = default) =>
            filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                ? Task.FromResult(new string('0', 64))
                : base.ComputeSha256Async(filePath, progress, cancellationToken);
    }
}
