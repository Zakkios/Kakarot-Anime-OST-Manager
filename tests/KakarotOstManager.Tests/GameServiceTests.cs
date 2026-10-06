using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class GameServiceTests
{
    private readonly GameService _service = new();

    [Fact]
    public void Le_dossier_BGM_standard_est_sous_AT()
    {
        string bgm = _service.GetBgmDirectory(@"C:\Jeux\KAKAROT", GameVersion.Standard);

        Assert.Equal(@"C:\Jeux\KAKAROT\AT\Content\Sound\Bgm", bgm);
    }

    [Fact]
    public void Le_dossier_BGM_remaster_est_sous_dlc_Remaster()
    {
        string bgm = _service.GetBgmDirectory(@"C:\Jeux\KAKAROT", GameVersion.Remaster);

        Assert.Equal(@"C:\Jeux\KAKAROT\dlc\Remaster\AT\Content\Sound\Bgm", bgm);
    }

    [Fact]
    public void Le_chemin_du_fichier_se_termine_par_Bgm_awb()
    {
        string file = _service.GetBgmFilePath(@"C:\Jeux\KAKAROT", GameVersion.Standard);

        Assert.Equal(@"C:\Jeux\KAKAROT\AT\Content\Sound\Bgm\Bgm.awb", file);
    }

    [Fact]
    public void La_version_detectee_est_Standard_quand_seul_le_dossier_standard_existe()
    {
        using var game = new TempDirectory();
        game.CreateDirectory("AT", "Content", "Sound", "Bgm");

        Assert.Equal(GameVersion.Standard, _service.DetectVersion(game.Root));
    }

    [Fact]
    public void La_version_detectee_est_Remaster_des_que_son_dossier_existe()
    {
        using var game = new TempDirectory();
        game.CreateDirectory("AT", "Content", "Sound", "Bgm");
        game.CreateDirectory("dlc", "Remaster", "AT", "Content", "Sound", "Bgm");

        Assert.Equal(GameVersion.Remaster, _service.DetectVersion(game.Root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Aucune_version_n_est_detectee_sans_chemin(string? gamePath)
    {
        Assert.Null(_service.DetectVersion(gamePath));
    }

    [Fact]
    public void Aucune_version_n_est_detectee_dans_un_dossier_quelconque()
    {
        using var notAGame = new TempDirectory();

        Assert.Null(_service.DetectVersion(notAGame.Root));
    }

    [Fact]
    public void Un_dossier_de_jeu_complet_est_valide()
    {
        using var game = new TempDirectory();
        game.CreateDirectory("AT", "Content", "Sound", "Bgm");

        Assert.Equal(GameDirectoryStatus.Valid, _service.ValidateGameDirectory(game.Root, GameVersion.Standard));
    }

    [Fact]
    public void Un_chemin_vide_est_signale_comme_non_renseigne()
    {
        Assert.Equal(GameDirectoryStatus.NotSet, _service.ValidateGameDirectory(null, GameVersion.Standard));
    }

    [Fact]
    public void Un_dossier_inexistant_est_signale()
    {
        using var temp = new TempDirectory();
        string missing = temp.Combine("n-existe-pas");

        Assert.Equal(GameDirectoryStatus.DirectoryNotFound, _service.ValidateGameDirectory(missing, GameVersion.Standard));
    }

    [Fact]
    public void Un_dossier_sans_BGM_pour_la_version_choisie_est_refuse()
    {
        using var game = new TempDirectory();
        game.CreateDirectory("AT", "Content", "Sound", "Bgm");

        // Le dossier standard existe, mais on demande la version Remaster.
        Assert.Equal(GameDirectoryStatus.BgmDirectoryNotFound, _service.ValidateGameDirectory(game.Root, GameVersion.Remaster));
    }
}
