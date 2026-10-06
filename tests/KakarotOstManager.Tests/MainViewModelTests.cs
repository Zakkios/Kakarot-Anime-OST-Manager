using KakarotOstManager.Localization;
using KakarotOstManager.Models;
using KakarotOstManager.Services;
using KakarotOstManager.ViewModels;

namespace KakarotOstManager.Tests;

public class MainViewModelTests
{
    /// <summary>Le ViewModel branché sur le bac à sable, avec un sélecteur de dossier simulé.</summary>
    private sealed class Screen : IDisposable
    {
        public Screen(bool saveSettings = true, string language = "fr")
        {
            if (saveSettings)
            {
                Env.Settings.Language = language;
                Env.SettingsService.Save(Env.Settings);
            }

            Localizer.Language = language;
            ViewModel = CreateViewModel();
        }

        public TestEnvironment Env { get; } = new();

        public FakeFolderPicker Picker { get; } = new();

        public FakeSteamLocator Steam { get; } = new();

        public Localizer Localizer { get; } = new();

        public MainViewModel ViewModel { get; }

        /// <summary>Simule un redémarrage de l'application sur les mêmes fichiers.</summary>
        public MainViewModel CreateViewModel()
        {
            var catalog = SoundtrackCatalogLoader.Load(SoundtrackCatalogLoader.DefaultPath);
            return new MainViewModel(
                Env.SettingsService, Env.Game, new SoundtrackService(catalog), Env.Installer, Env.Backups,
                Picker, Steam, Localizer);
        }

        public SoundtrackItemViewModel Item(string id) =>
            ViewModel.Soundtracks.Single(item => item.Soundtrack.Id == id);

        public void Dispose() => Env.Dispose();
    }

    private sealed class FakeFolderPicker : IFolderPicker
    {
        /// <summary>Dossier que « l'utilisateur » choisira ; <c>null</c> pour simuler une annulation.</summary>
        public string? NextFolder { get; set; }

        public string? PickFolder(string title, string? initialDirectory) => NextFolder;
    }

    // ---------------------------------------------------------------- démarrage

    [Fact]
    public async Task Au_demarrage_les_parametres_enregistres_sont_affiches()
    {
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync();

        Assert.Equal(screen.Env.Settings.GamePath, screen.ViewModel.GamePath);
        Assert.Equal(screen.Env.Settings.SoundtrackPath, screen.ViewModel.SoundtrackPath);
        Assert.True(screen.ViewModel.IsStandard);
        Assert.False(screen.ViewModel.IsRemaster);
        Assert.Equal("", screen.ViewModel.GamePathError);
    }

    [Fact]
    public async Task Creer_puis_initialiser_le_ViewModel_n_ecrase_pas_les_parametres_enregistres()
    {
        // Non-régression : construire le ViewModel enregistrait des paramètres
        // vides par-dessus settings.json avant même de l'avoir lu.
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync();

        AppSettings onDisk = screen.Env.SettingsService.Load();
        Assert.Equal(screen.Env.Settings.GamePath, onDisk.GamePath);
        Assert.Equal(screen.Env.Settings.SoundtrackPath, onDisk.SoundtrackPath);
        Assert.Equal(GameVersion.Standard, onDisk.GameVersion);
    }

    [Fact]
    public async Task Au_demarrage_les_bandes_son_sont_listees_par_rubrique_et_la_premiere_est_selectionnee()
    {
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync();

        Assert.Equal(["01 — Raditz", "03 — Namek", "05 — Cell"], screen.ViewModel.Soundtracks.Select(item => item.Title));
        Assert.All(screen.ViewModel.Soundtracks, item => Assert.Equal("Histoire principale", item.CategoryTitle));
        Assert.Equal("01 — Raditz", screen.ViewModel.SelectedTitle);
        Assert.True(screen.ViewModel.HasSelection);
        Assert.False(screen.ViewModel.IsBusy);
    }

    [Fact]
    public async Task Sans_aucun_parametre_l_ecran_est_vide_mais_utilisable()
    {
        using var screen = new Screen(saveSettings: false);

        await screen.ViewModel.InitializeAsync();

        Assert.Equal("", screen.ViewModel.GamePath);
        Assert.Empty(screen.ViewModel.Soundtracks);
        Assert.False(screen.ViewModel.HasSelection);
        Assert.Equal("—", screen.ViewModel.InstalledTitle);
        Assert.False(screen.ViewModel.ApplyCommand.CanExecute(null));
        Assert.False(screen.ViewModel.RestoreCommand.CanExecute(null));
        Assert.True(screen.ViewModel.BrowseGameFolderCommand.CanExecute(null));
    }

    [Fact]
    public async Task Avant_toute_installation_le_fichier_en_place_est_presente_comme_probablement_original()
    {
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync();

        Assert.Equal("Fichier non reconnu (sans doute la bande-son originale)", screen.ViewModel.InstalledTitle);
        Assert.False(screen.ViewModel.RestoreCommand.CanExecute(null));
    }

    [Fact]
    public async Task Une_erreur_de_catalogue_est_affichee_sans_bloquer_le_demarrage()
    {
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync(new InvalidOperationException("ligne 12"));

        Assert.Equal(StatusKind.Error, screen.ViewModel.Status);
        Assert.Contains("ligne 12", screen.ViewModel.StatusMessage);
        Assert.NotEmpty(screen.ViewModel.Soundtracks);
    }

    // ---------------------------------------------------------------- choix des dossiers

    [Fact]
    public async Task Choisir_le_dossier_du_jeu_detecte_la_version_et_enregistre_le_tout()
    {
        using var screen = new Screen(saveSettings: false);
        await screen.ViewModel.InitializeAsync();
        screen.Picker.NextFolder = screen.Env.Settings.GamePath;

        await screen.ViewModel.BrowseGameFolderCommand.ExecuteAsync(null);

        Assert.Equal(screen.Env.Settings.GamePath, screen.ViewModel.GamePath);
        Assert.True(screen.ViewModel.IsStandard);
        Assert.Equal("", screen.ViewModel.GamePathError);
        AppSettings onDisk = screen.Env.SettingsService.Load();
        Assert.Equal(screen.Env.Settings.GamePath, onDisk.GamePath);
        Assert.Equal(GameVersion.Standard, onDisk.GameVersion);
    }

    [Fact]
    public async Task Un_dossier_qui_n_est_pas_le_jeu_affiche_une_erreur()
    {
        using var screen = new Screen(saveSettings: false);
        await screen.ViewModel.InitializeAsync();
        screen.Picker.NextFolder = screen.Env.Temp.CreateDirectory("pas-le-jeu");

        await screen.ViewModel.BrowseGameFolderCommand.ExecuteAsync(null);

        Assert.Equal("Le dossier sélectionné ne semble pas contenir Dragon Ball Z: Kakarot.", screen.ViewModel.GamePathError);
    }

    [Fact]
    public async Task Annuler_la_selection_de_dossier_ne_change_rien()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.Picker.NextFolder = null;

        await screen.ViewModel.BrowseGameFolderCommand.ExecuteAsync(null);

        Assert.Equal(screen.Env.Settings.GamePath, screen.ViewModel.GamePath);
    }

    [Fact]
    public async Task Choisir_le_dossier_du_pack_liste_ses_bandes_son()
    {
        using var screen = new Screen(saveSettings: false);
        await screen.ViewModel.InitializeAsync();
        screen.Picker.NextFolder = screen.Env.Settings.SoundtrackPath;

        await screen.ViewModel.BrowseSoundtrackFolderCommand.ExecuteAsync(null);

        Assert.Equal(3, screen.ViewModel.Soundtracks.Count);
        Assert.Equal("", screen.ViewModel.SoundtrackPathError);
        Assert.NotNull(screen.ViewModel.SelectedSoundtrack);
        Assert.Equal(screen.Env.Settings.SoundtrackPath, screen.Env.SettingsService.Load().SoundtrackPath);
    }

    [Fact]
    public async Task Un_dossier_sans_Bgm_awb_affiche_une_erreur()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.Picker.NextFolder = screen.Env.Temp.CreateDirectory("dossier-vide");

        await screen.ViewModel.BrowseSoundtrackFolderCommand.ExecuteAsync(null);

        Assert.Empty(screen.ViewModel.Soundtracks);
        Assert.Equal("Aucun fichier Bgm.awb n'a été trouvé.", screen.ViewModel.SoundtrackPathError);
        Assert.False(screen.ViewModel.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task Choisir_une_version_sans_dossier_BGM_affiche_une_erreur()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();

        screen.ViewModel.IsRemaster = true;

        Assert.True(screen.ViewModel.IsRemaster);
        Assert.False(screen.ViewModel.IsStandard);
        Assert.Equal("Le dossier BGM de cette version du jeu est introuvable dans le dossier du jeu.", screen.ViewModel.GamePathError);
        Assert.Equal(GameVersion.Remaster, screen.Env.SettingsService.Load().GameVersion);
    }

    // ---------------------------------------------------------------- appliquer

    [Fact]
    public async Task Appliquer_installe_la_bande_son_selectionnee_et_l_annonce()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");

        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(TestEnvironment.ContentOf(screen.Env.Namek), screen.Env.GameFileContent);
        Assert.Equal(StatusKind.Success, screen.ViewModel.Status);
        Assert.Equal(
            "Bande-son 03 — Namek installée avec succès. Vous pouvez lancer Dragon Ball Z: Kakarot.",
            screen.ViewModel.StatusMessage);
        Assert.Equal("03 — Namek", screen.ViewModel.InstalledTitle);
        Assert.True(screen.Item("03-namek").IsActive);
        Assert.False(screen.Item("01-raditz").IsActive);
        Assert.False(screen.ViewModel.IsBusy);
    }

    [Fact]
    public async Task Apres_une_installation_les_controles_sont_listes_et_la_restauration_devient_possible()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");

        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(7, screen.ViewModel.Checks.Count);
        Assert.All(screen.ViewModel.Checks, check => Assert.True(check.Passed));
        Assert.Contains(screen.ViewModel.Checks, check => check.Label == "Kakarot n'est pas lancé");
        Assert.True(screen.ViewModel.RestoreCommand.CanExecute(null));
    }

    [Fact]
    public async Task Appliquer_pendant_que_le_jeu_tourne_affiche_l_erreur_et_ne_modifie_rien()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");
        screen.Env.StartGame();

        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(StatusKind.Error, screen.ViewModel.Status);
        Assert.StartsWith("Dragon Ball Z: Kakarot est actuellement lancé.", screen.ViewModel.StatusMessage);
        Assert.Contains(screen.ViewModel.Checks, check => !check.Passed && check.Label == "Kakarot n'est pas lancé");
        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), screen.Env.GameFileContent);
        Assert.False(screen.Item("03-namek").IsActive);
    }

    [Fact]
    public async Task Un_fichier_source_disparu_est_signale_avec_le_nom_de_la_bande_son()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("05-cell");
        File.Delete(screen.Env.Cell.BgmFilePath);

        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(StatusKind.Error, screen.ViewModel.Status);
        Assert.Equal("Le fichier Bgm.awb de 05 — Cell est introuvable.", screen.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task Sans_selection_le_bouton_Appliquer_est_desactive()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();

        screen.ViewModel.SelectedSoundtrack = null;

        Assert.False(screen.ViewModel.ApplyCommand.CanExecute(null));
        Assert.False(screen.ViewModel.HasSelection);
    }

    // ---------------------------------------------------------------- restaurer

    [Fact]
    public async Task Restaurer_remet_l_original_et_l_annonce()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");
        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        await screen.ViewModel.RestoreCommand.ExecuteAsync(null);

        Assert.Equal(TestEnvironment.Content(TestEnvironment.OriginalMusic), screen.Env.GameFileContent);
        Assert.Equal(StatusKind.Success, screen.ViewModel.Status);
        Assert.Equal("Bande-son originale restaurée.", screen.ViewModel.StatusMessage);
        Assert.Equal("Bande-son originale", screen.ViewModel.InstalledTitle);
        Assert.DoesNotContain(screen.ViewModel.Soundtracks, item => item.IsActive);
    }

    // ---------------------------------------------------------------- redémarrage et détection

    [Fact]
    public async Task Au_redemarrage_la_bande_son_installee_est_retrouvee_et_selectionnee()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("05-cell");
        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        MainViewModel restarted = screen.CreateViewModel();
        await restarted.InitializeAsync();

        Assert.Equal("05 — Cell", restarted.InstalledTitle);
        Assert.Equal("05 — Cell", restarted.SelectedTitle);
        Assert.True(restarted.RestoreCommand.CanExecute(null));
    }

    [Fact]
    public async Task Un_remplacement_fait_a_la_main_est_affiche_et_corrige_les_parametres()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");
        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);
        screen.Env.ReplaceGameFileExternally(TestEnvironment.ContentOf(screen.Env.Cell));

        MainViewModel restarted = screen.CreateViewModel();
        await restarted.InitializeAsync();

        Assert.Equal("05 — Cell", restarted.InstalledTitle);
        Assert.Equal("05-cell", screen.Env.SettingsService.Load().CurrentSoundtrackId);
    }

    // ---------------------------------------------------------------- détection automatique

    [Fact]
    public async Task Au_premier_lancement_le_dossier_du_jeu_est_trouve_grace_a_Steam()
    {
        using var screen = new Screen(saveSettings: false);
        screen.Steam.GameDirectory = screen.Env.Settings.GamePath;

        await screen.ViewModel.InitializeAsync();

        Assert.Equal(screen.Env.Settings.GamePath, screen.ViewModel.GamePath);
        Assert.True(screen.ViewModel.IsStandard);
        Assert.Equal(StatusKind.Info, screen.ViewModel.Status);
        Assert.Equal("Dossiers détectés automatiquement. Vérifiez-les avant de continuer.", screen.ViewModel.StatusMessage);
        Assert.Equal(screen.Env.Settings.GamePath, screen.Env.SettingsService.Load().GamePath);
    }

    [Fact]
    public async Task Un_pack_range_dans_le_dossier_BGM_du_jeu_est_detecte()
    {
        using var screen = new Screen(saveSettings: false);
        screen.Steam.GameDirectory = screen.Env.Settings.GamePath;
        string bgmDirectory = Path.GetDirectoryName(screen.Env.GameFile)!;
        File.WriteAllText(
            Path.Combine(Directory.CreateDirectory(Path.Combine(bgmDirectory, "03 - Namek")).FullName, "Bgm.awb"),
            TestEnvironment.Content("musique 03 - Namek"));

        await screen.ViewModel.InitializeAsync();

        Assert.Equal(bgmDirectory, screen.ViewModel.SoundtrackPath);
        Assert.Equal("03 — Namek", Assert.Single(screen.ViewModel.Soundtracks).Title);
    }

    [Fact]
    public async Task Un_dossier_deja_enregistre_n_est_pas_remplace_par_la_detection()
    {
        using var screen = new Screen();
        screen.Steam.GameDirectory = screen.Env.Temp.CreateDirectory("autre-installation");

        await screen.ViewModel.InitializeAsync();

        Assert.Equal(screen.Env.Settings.GamePath, screen.ViewModel.GamePath);
    }

    // ---------------------------------------------------------------- lancer le jeu

    [Fact]
    public async Task Lancer_le_jeu_passe_par_Steam()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();

        screen.ViewModel.LaunchGameCommand.Execute(null);

        Assert.Equal(["steam://rungameid/851850"], screen.Env.Shell.Opened);
        Assert.Equal(StatusKind.Info, screen.ViewModel.Status);
        Assert.Equal("Lancement du jeu via Steam…", screen.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task Un_echec_du_lancement_est_affiche()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.Env.Shell.Failure = new InvalidOperationException("Steam est introuvable");

        screen.ViewModel.LaunchGameCommand.Execute(null);

        Assert.Equal(StatusKind.Error, screen.ViewModel.Status);
        Assert.Equal("Impossible de lancer le jeu via Steam : Steam est introuvable", screen.ViewModel.StatusMessage);
    }

    // ---------------------------------------------------------------- précédente, suivante

    [Fact]
    public async Task La_precedente_et_la_suivante_de_la_selection_sont_affichees()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();

        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");

        Assert.True(screen.ViewModel.HasSelectedNeighbors);
        Assert.Equal("01 — Raditz", screen.ViewModel.SelectedPrevious);
        Assert.Equal("05 — Cell", screen.ViewModel.SelectedNext);
    }

    [Fact]
    public async Task Le_premier_chapitre_n_a_pas_de_precedente()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();

        screen.ViewModel.SelectedSoundtrack = screen.Item("01-raditz");

        Assert.Equal("—", screen.ViewModel.SelectedPrevious);
        Assert.Equal("03 — Namek", screen.ViewModel.SelectedNext);
    }

    [Fact]
    public async Task Tant_que_la_musique_d_origine_est_en_place_le_premier_chapitre_est_propose()
    {
        using var screen = new Screen();

        await screen.ViewModel.InitializeAsync();

        Assert.True(screen.ViewModel.HasNextUp);
        Assert.Equal("01 — Raditz", screen.ViewModel.NextUpTitle);
        Assert.True(screen.ViewModel.InstallNextCommand.CanExecute(null));
    }

    [Fact]
    public async Task Installer_l_OST_suivante_installe_le_chapitre_qui_suit_celui_en_place()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("01-raditz");
        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);
        Assert.Equal("03 — Namek", screen.ViewModel.NextUpTitle);

        await screen.ViewModel.InstallNextCommand.ExecuteAsync(null);

        Assert.Equal(TestEnvironment.ContentOf(screen.Env.Namek), screen.Env.GameFileContent);
        Assert.Equal("03 — Namek", screen.ViewModel.InstalledTitle);
        Assert.Equal("03 — Namek", screen.ViewModel.SelectedTitle);
        Assert.Equal("05 — Cell", screen.ViewModel.NextUpTitle);
    }

    [Fact]
    public async Task Au_dernier_chapitre_plus_rien_n_est_propose()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("05-cell");

        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        Assert.False(screen.ViewModel.HasNextUp);
        Assert.Equal("", screen.ViewModel.NextUpTitle);
        Assert.False(screen.ViewModel.InstallNextCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------- langue

    [Fact]
    public async Task Changer_de_langue_retraduit_tout_l_ecran_et_s_enregistre()
    {
        using var screen = new Screen();
        await screen.ViewModel.InitializeAsync();
        screen.ViewModel.SelectedSoundtrack = screen.Item("03-namek");
        await screen.ViewModel.ApplyCommand.ExecuteAsync(null);

        screen.ViewModel.SelectedLanguage = screen.ViewModel.Languages.Single(language => language.Code == "en");

        Assert.Equal(
            "03 — Namek soundtrack installed successfully. You can now launch Dragon Ball Z: Kakarot.",
            screen.ViewModel.StatusMessage);
        Assert.All(screen.ViewModel.Soundtracks, item => Assert.Equal("Main story", item.CategoryTitle));
        Assert.Contains(screen.ViewModel.Checks, check => check.Label == "Kakarot is not running");
        Assert.Equal("No guidance available yet for this soundtrack.", screen.ViewModel.SelectedHint);
        Assert.Equal("03 — Namek", screen.ViewModel.SelectedTitle);
        Assert.True(screen.Item("03-namek").IsActive);
        Assert.Equal("en", screen.Env.SettingsService.Load().Language);
    }

    [Fact]
    public async Task La_langue_enregistree_est_reprise_au_demarrage()
    {
        using var screen = new Screen(language: "en");

        await screen.ViewModel.InitializeAsync();

        Assert.Equal("en", screen.ViewModel.SelectedLanguage.Code);
        Assert.Equal("Unrecognized file (most likely the original soundtrack)", screen.ViewModel.InstalledTitle);
    }
}

public class LocalizationTests
{
    [Fact]
    public void Chaque_texte_existe_en_francais_et_en_anglais()
    {
        var resources = new System.Resources.ResourceManager(
            "KakarotOstManager.Localization.Strings", typeof(Localizer).Assembly);

        HashSet<string> Keys(System.Globalization.CultureInfo culture) => resources
            .GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<System.Collections.DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .ToHashSet();

        // L'anglais est la langue par défaut : ses textes sont ceux de la culture « invariante ».
        HashSet<string> english = Keys(System.Globalization.CultureInfo.InvariantCulture);
        HashSet<string> french = Keys(System.Globalization.CultureInfo.GetCultureInfo("fr"));

        Assert.NotEmpty(english);
        Assert.Empty(english.Except(french));
        Assert.Empty(french.Except(english));
    }

    [Fact]
    public void Chaque_valeur_des_enums_affiches_a_son_texte()
    {
        var localizer = new Localizer { Language = "fr" };
        var keys = Enum.GetNames<CheckId>().Select(name => $"Check{name}")
            .Concat(Enum.GetNames<InstallStep>().Select(name => $"Step{name}"))
            .Concat(Enum.GetNames<SoundtrackCategory>().Select(name => $"Category{name}"));

        // Une clé absente est rendue entre crochets, par exemple « [CheckFoo] ».
        Assert.All(keys, key => Assert.DoesNotContain("[", localizer[key]));
    }

    [Fact]
    public void Une_langue_inconnue_retombe_sur_l_anglais()
    {
        var localizer = new Localizer { Language = "de" };

        Assert.Equal("en", localizer.Language);
        Assert.Equal("Apply", localizer["Apply"]);
    }

    [Fact]
    public void Changer_de_langue_previent_l_interface()
    {
        var localizer = new Localizer { Language = "en" };
        var changed = new List<string?>();
        localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localizer.Language = "fr";

        Assert.Equal("Appliquer", localizer["Apply"]);
        Assert.Contains("Item[]", changed);
    }
}
