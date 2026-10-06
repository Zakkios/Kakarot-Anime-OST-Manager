using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class SoundtrackServiceTests
{
    /// <summary>Noms de dossiers tels que le mod les fournit réellement.</summary>
    private static readonly string[] RealFolderNames =
    [
        "DBZ Kakarot Soundtrack JPN Mod 01 - Raditz",
        "DBZ Kakarot Soundtrack JPN Mod 02 - Saiyans",
        "DBZ Kakarot Soundtrack JPN Mod 02.5 - Vegeta",
        "DBZ Kakarot Soundtrack JPN Mod 03 - Namek",
        "DBZ Kakarot Soundtrack JPN Mod 04 - Androids",
        "DBZ Kakarot Soundtrack JPN Mod 05 - Cell",
        "DBZ Kakarot Soundtrack JPN Mod 06 - Majin Buu",
        "DBZ Kakarot Soundtrack JPN Mod 07 - Kid Buu",
        "DBZ Kakarot Soundtrack JPN Mod 07.5 - Post Game",
    ];

    /// <summary>
    /// Noms des dix-huit dossiers du pack actuel, tels que la page Nexus du
    /// mod les donne, avec l'identifiant attendu pour chacun.
    /// </summary>
    public static TheoryData<string, string, SoundtrackCategory> CurrentPackFolders => new()
    {
        { "DBZ - Kakarot Soundtrack Mod 01 - Raditz", "01-raditz", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 02 - Saiyans", "02-saiyans", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 02.5 - Vegeta", "02.5-vegeta", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 03 - Namek", "03-namek", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 04 - Androids", "04-androids", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 05 - Cell", "05-cell", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 06 - Majin Buu", "06-majin-buu", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 07 - Kid Buu", "07-kid-buu", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 07.5 - Post Game", "07.5-post-game", SoundtrackCategory.Main },
        { "DBZ - Kakarot Soundtrack Mod 08 - A New Power Awakens Part 1", "08-a-new-power-awakens-1", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 09 - A New Power Awakens Part 2", "09-a-new-power-awakens-2", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 10 - Trunks - The Warrior of Hope", "10-trunks", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 11 - Trunks - The Warrior of Hope - Epilogue", "11-trunks-epilogue", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 12 - Bardock - Alone Against Fate", "12-bardock", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 13 - 23rd Tenkaichi Budokai", "13-23rd-tenkaichi-budokai", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 14 - End of Z", "14-end-of-z", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 15 - Daima Part 1", "15-daima-1", SoundtrackCategory.Dlc },
        { "DBZ - Kakarot Soundtrack Mod 16 - Daima Part 2", "16-daima-2", SoundtrackCategory.Dlc },
    };

    [Theory]
    [MemberData(nameof(CurrentPackFolders))]
    public void Chaque_dossier_du_pack_actuel_est_reconnu(string folderName, string expectedId, SoundtrackCategory expectedCategory)
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, folderName);

        Soundtrack found = Assert.Single(CreateService().FindSoundtracks(pack.Root));

        Assert.Equal(expectedId, found.Id);
        Assert.Equal(expectedCategory, found.Category);
        Assert.NotEqual("", found.ActivationHint.Get("fr"));
    }

    [Fact]
    public void Le_pack_complet_est_liste_histoire_d_abord_puis_DLC_sans_rien_dans_Autres()
    {
        using var pack = new TempDirectory();
        foreach (var row in CurrentPackFolders.Reverse())
        {
            AddSoundtrackFolder(pack, (string)row[0]);
        }

        IReadOnlyList<Soundtrack> found = CreateService().FindSoundtracks(pack.Root);

        Assert.Equal(CurrentPackFolders.Select(row => (string)row[1]), found.Select(soundtrack => soundtrack.Id));
        Assert.DoesNotContain(found, soundtrack => soundtrack.Category == SoundtrackCategory.Other);
    }

    [Fact]
    public void Le_dernier_chapitre_de_l_histoire_n_a_pas_de_DLC_pour_suivante()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "DBZ - Kakarot Soundtrack Mod 07.5 - Post Game");
        AddSoundtrackFolder(pack, "DBZ - Kakarot Soundtrack Mod 08 - A New Power Awakens Part 1");
        IReadOnlyList<Soundtrack> all = CreateService().FindSoundtracks(pack.Root);

        Assert.Null(SoundtrackService.GetNext(all, all.Single(soundtrack => soundtrack.Id == "07.5-post-game")));
        Assert.Null(SoundtrackService.GetPrevious(all, all.Single(soundtrack => soundtrack.Id == "08-a-new-power-awakens-1")));
    }

    /// <summary>Service branché sur le vrai soundtracks.json livré avec l'application.</summary>
    private static SoundtrackService CreateService() =>
        new(SoundtrackCatalogLoader.LoadEmbedded());

    private static void AddSoundtrackFolder(TempDirectory pack, params string[] folderParts) =>
        pack.CreateFile("faux contenu audio", [.. folderParts, "Bgm.awb"]);

    [Fact]
    public void Les_neuf_dossiers_reels_du_mod_sont_reconnus_dans_l_ordre_de_l_histoire()
    {
        using var pack = new TempDirectory();
        // Créés dans le désordre : le tri ne doit pas dépendre de l'ordre du disque.
        foreach (string folder in RealFolderNames.Reverse())
        {
            AddSoundtrackFolder(pack, folder);
        }

        IReadOnlyList<Soundtrack> found = CreateService().FindSoundtracks(pack.Root);

        Assert.Equal(
            ["01-raditz", "02-saiyans", "02.5-vegeta", "03-namek", "04-androids", "05-cell", "06-majin-buu", "07-kid-buu", "07.5-post-game"],
            found.Select(soundtrack => soundtrack.Id));
        Assert.All(found, soundtrack => Assert.Equal(SoundtrackCategory.Main, soundtrack.Category));
    }

    [Fact]
    public void Une_bande_son_reconnue_porte_ses_metadonnees_et_ses_chemins()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "DBZ Kakarot Soundtrack JPN Mod 02.5 - Vegeta");

        Soundtrack vegeta = Assert.Single(CreateService().FindSoundtracks(pack.Root));

        Assert.Equal("02.5-vegeta", vegeta.Id);
        Assert.Equal("02.5", vegeta.Number);
        Assert.Equal("Vegeta", vegeta.Name.Get("fr"));
        Assert.Equal("DBZ Kakarot Soundtrack JPN Mod 02.5 - Vegeta", vegeta.FolderName);
        Assert.Equal(pack.Combine("DBZ Kakarot Soundtrack JPN Mod 02.5 - Vegeta", "Bgm.awb"), vegeta.BgmFilePath);
    }

    [Fact]
    public void Les_noms_courts_du_cahier_des_charges_sont_reconnus_aussi()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "02 - Saiyans");
        AddSoundtrackFolder(pack, "02.5 - Vegeta");
        AddSoundtrackFolder(pack, "03 - namek");

        var ids = CreateService().FindSoundtracks(pack.Root).Select(soundtrack => soundtrack.Id);

        Assert.Equal(["02-saiyans", "02.5-vegeta", "03-namek"], ids);
    }

    [Fact]
    public void Un_dossier_inconnu_est_range_dans_Autres_sous_son_propre_nom()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "DBZ Kakarot Soundtrack JPN Mod 03 - Namek");
        AddSoundtrackFolder(pack, "backup");
        AddSoundtrackFolder(pack, "17 - Nouveau DLC");

        IReadOnlyList<Soundtrack> found = CreateService().FindSoundtracks(pack.Root);

        Assert.Equal(["03-namek", "17-nouveau-dlc", "backup"], found.Select(soundtrack => soundtrack.Id));
        Soundtrack unknown = found[1];
        Assert.Equal(SoundtrackCategory.Other, unknown.Category);
        Assert.Equal("17 - Nouveau DLC", unknown.Name.Get("fr"));
        Assert.Null(unknown.Number);
    }

    [Fact]
    public void Un_dossier_sans_Bgm_awb_est_ignore()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "03 - Namek");
        pack.CreateDirectory("04 - Androids");
        pack.CreateFile("lisez-moi", "05 - Cell", "readme.txt");

        Soundtrack only = Assert.Single(CreateService().FindSoundtracks(pack.Root));

        Assert.Equal("03-namek", only.Id);
    }

    [Fact]
    public void Le_Bgm_awb_pose_a_la_racine_n_est_pas_une_bande_son()
    {
        // Cas réel : le pack est rangé dans le dossier BGM du jeu, où se
        // trouve aussi le Bgm.awb que le jeu utilise.
        using var pack = new TempDirectory();
        pack.CreateFile("fichier actif du jeu", "Bgm.awb");
        AddSoundtrackFolder(pack, "03 - Namek");

        Soundtrack only = Assert.Single(CreateService().FindSoundtracks(pack.Root));

        Assert.Equal("03-namek", only.Id);
    }

    [Fact]
    public void Une_archive_extraite_dans_un_dossier_du_meme_nom_est_trouvee()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "DBZ Kakarot Soundtrack JPN Mod 05 - Cell", "DBZ Kakarot Soundtrack JPN Mod 05 - Cell");

        Soundtrack cell = Assert.Single(CreateService().FindSoundtracks(pack.Root));

        Assert.Equal("05-cell", cell.Id);
        Assert.Equal(
            pack.Combine("DBZ Kakarot Soundtrack JPN Mod 05 - Cell", "DBZ Kakarot Soundtrack JPN Mod 05 - Cell"),
            cell.FolderPath);
    }

    [Fact]
    public void Deux_dossiers_pour_la_meme_bande_son_gardent_des_identifiants_distincts()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "03 - Namek");
        AddSoundtrackFolder(pack, "Copie de 03 - Namek");

        IReadOnlyList<Soundtrack> found = CreateService().FindSoundtracks(pack.Root);

        Assert.Equal(2, found.Count);
        Assert.Equal(2, found.Select(soundtrack => soundtrack.Id).Distinct().Count());
        Assert.Single(found, soundtrack => soundtrack.Category == SoundtrackCategory.Main);
        Assert.Single(found, soundtrack => soundtrack.Category == SoundtrackCategory.Other);
    }

    [Fact]
    public void Un_chemin_avec_separateur_final_donne_le_meme_resultat()
    {
        using var pack = new TempDirectory();
        pack.CreateFile("fichier actif du jeu", "Bgm.awb");
        AddSoundtrackFolder(pack, "03 - Namek");

        var found = CreateService().FindSoundtracks(pack.Root + Path.DirectorySeparatorChar);

        Assert.Single(found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"Z:\ce\dossier\n-existe\pas")]
    public void Un_chemin_absent_ou_inexistant_donne_une_liste_vide(string? packPath)
    {
        Assert.Empty(CreateService().FindSoundtracks(packPath));
    }

    [Fact]
    public void Precedente_et_suivante_suivent_l_ordre_de_l_histoire()
    {
        using var pack = new TempDirectory();
        foreach (string folder in RealFolderNames)
        {
            AddSoundtrackFolder(pack, folder);
        }
        IReadOnlyList<Soundtrack> all = CreateService().FindSoundtracks(pack.Root);
        Soundtrack namek = all.Single(soundtrack => soundtrack.Id == "03-namek");

        Assert.Equal("02.5-vegeta", SoundtrackService.GetPrevious(all, namek)?.Id);
        Assert.Equal("04-androids", SoundtrackService.GetNext(all, namek)?.Id);
    }

    [Fact]
    public void Precedente_et_suivante_sautent_les_bandes_son_absentes_du_disque()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "01 - Raditz");
        AddSoundtrackFolder(pack, "03 - Namek");
        AddSoundtrackFolder(pack, "05 - Cell");
        IReadOnlyList<Soundtrack> all = CreateService().FindSoundtracks(pack.Root);
        Soundtrack namek = all.Single(soundtrack => soundtrack.Id == "03-namek");

        Assert.Equal("01-raditz", SoundtrackService.GetPrevious(all, namek)?.Id);
        Assert.Equal("05-cell", SoundtrackService.GetNext(all, namek)?.Id);
    }

    [Fact]
    public void La_premiere_n_a_pas_de_precedente_et_la_derniere_pas_de_suivante()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "01 - Raditz");
        AddSoundtrackFolder(pack, "07.5 - Post Game");
        IReadOnlyList<Soundtrack> all = CreateService().FindSoundtracks(pack.Root);

        Assert.Null(SoundtrackService.GetPrevious(all, all[0]));
        Assert.Null(SoundtrackService.GetNext(all, all[^1]));
    }

    [Fact]
    public void Une_bande_son_hors_histoire_n_a_ni_precedente_ni_suivante()
    {
        using var pack = new TempDirectory();
        AddSoundtrackFolder(pack, "03 - Namek");
        AddSoundtrackFolder(pack, "backup");
        IReadOnlyList<Soundtrack> all = CreateService().FindSoundtracks(pack.Root);
        Soundtrack backup = all.Single(soundtrack => soundtrack.Id == "backup");

        Assert.Null(SoundtrackService.GetPrevious(all, backup));
        Assert.Null(SoundtrackService.GetNext(all, backup));
    }
}
