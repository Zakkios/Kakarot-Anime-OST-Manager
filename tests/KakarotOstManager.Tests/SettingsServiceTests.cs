using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Sans_fichier_les_parametres_sont_vides()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(new AppPaths(temp.Root));

        AppSettings settings = service.Load();

        Assert.Null(settings.GamePath);
        Assert.Null(settings.SoundtrackPath);
        Assert.Null(settings.GameVersion);
        Assert.Null(settings.CurrentSoundtrackId);
    }

    [Fact]
    public void Les_parametres_enregistres_sont_relus_a_l_identique()
    {
        using var temp = new TempDirectory();
        var service = new SettingsService(new AppPaths(temp.Root));

        service.Save(new AppSettings
        {
            GamePath = @"D:\SteamLibrary\steamapps\common\DRAGON BALL Z KAKAROT",
            SoundtrackPath = @"D:\Mods\Bande-son animé",
            GameVersion = GameVersion.Remaster,
            CurrentSoundtrackId = "03-namek",
        });
        AppSettings reloaded = service.Load();

        Assert.Equal(@"D:\SteamLibrary\steamapps\common\DRAGON BALL Z KAKAROT", reloaded.GamePath);
        Assert.Equal(@"D:\Mods\Bande-son animé", reloaded.SoundtrackPath);
        Assert.Equal(GameVersion.Remaster, reloaded.GameVersion);
        Assert.Equal("03-namek", reloaded.CurrentSoundtrackId);
    }

    [Fact]
    public void Le_dossier_de_l_application_est_cree_a_l_enregistrement()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Combine("pas", "encore", "cree"));

        new SettingsService(paths).Save(new AppSettings());

        Assert.True(File.Exists(paths.SettingsFile));
    }

    [Fact]
    public void Le_fichier_est_lisible_par_un_humain()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Root);

        new SettingsService(paths).Save(new AppSettings
        {
            SoundtrackPath = @"D:\Mods\Bande-son animé",
            GameVersion = GameVersion.Remaster,
        });
        string json = File.ReadAllText(paths.SettingsFile);

        Assert.Contains("\"gameVersion\": \"remaster\"", json);
        Assert.Contains("Bande-son animé", json);
    }

    [Fact]
    public void Un_fichier_abime_donne_des_parametres_vides_au_lieu_d_une_erreur()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Root);
        File.WriteAllText(paths.SettingsFile, "{ ceci n'est pas du JSON");

        AppSettings settings = new SettingsService(paths).Load();

        Assert.Null(settings.GamePath);
    }

    [Fact]
    public void Enregistrer_ne_laisse_pas_de_fichier_provisoire()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Root);
        var service = new SettingsService(paths);

        service.Save(new AppSettings { CurrentSoundtrackId = "01-raditz" });
        service.Save(new AppSettings { CurrentSoundtrackId = "02-saiyans" });

        Assert.Equal(["settings.json"], Directory.GetFiles(temp.Root).Select(Path.GetFileName));
        Assert.Equal("02-saiyans", service.Load().CurrentSoundtrackId);
    }
}
