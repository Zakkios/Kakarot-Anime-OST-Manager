using System.Diagnostics;
using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class FileCopyServiceTests
{
    private readonly FileCopyService _copier = new(new HashService());

    [Fact]
    public async Task La_copie_remplace_la_destination_et_renvoie_l_empreinte()
    {
        using var temp = new TempDirectory();
        string source = temp.CreateFile("abc", "source.awb");
        string destination = temp.CreateFile("ancien contenu", "destination.awb");

        string hash = await _copier.CopyVerifiedAsync(source, destination);

        Assert.Equal("abc", File.ReadAllText(destination));
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
        Assert.False(File.Exists(destination + ".tmp"));
    }

    [Fact]
    public async Task Un_gros_fichier_est_copie_a_l_identique()
    {
        using var temp = new TempDirectory();
        // 2,5 Mo de données variées : la copie se fait en plusieurs blocs.
        byte[] data = new byte[2_500_000];
        new Random(1234).NextBytes(data);
        string source = temp.Combine("source.awb");
        File.WriteAllBytes(source, data);
        string destination = temp.Combine("destination.awb");
        var progress = new RecordingProgress<double>();

        await _copier.CopyVerifiedAsync(source, destination, progress);

        Assert.Equal(data, File.ReadAllBytes(destination));
        Assert.Equal(progress.Values.Order(), progress.Values);
        Assert.Equal(1, progress.Values[^1]);
    }

    [Fact]
    public async Task Une_source_absente_laisse_la_destination_intacte()
    {
        using var temp = new TempDirectory();
        string destination = temp.CreateFile("ancien contenu", "destination.awb");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _copier.CopyVerifiedAsync(temp.Combine("absente.awb"), destination));

        Assert.Equal("ancien contenu", File.ReadAllText(destination));
        Assert.False(File.Exists(destination + ".tmp"));
    }
}

public class BackupServiceTests
{
    [Fact]
    public async Task Une_nouvelle_sauvegarde_archive_la_precedente()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Combine("appdata"));
        var service = new BackupService(paths, new FileCopyService(new HashService()));
        string gameFile = temp.CreateFile("original v1", "Bgm.awb");
        await service.BackupVanillaAsync(gameFile, GameVersion.Standard);

        File.WriteAllText(gameFile, "original v2");
        await service.BackupVanillaAsync(gameFile, GameVersion.Standard);

        Assert.Equal("original v2", File.ReadAllText(service.GetBackupFile(GameVersion.Standard)));
        string archived = Assert.Single(Directory.GetFiles(paths.GetArchiveDirectory(GameVersion.Standard)));
        Assert.Equal("original v1", File.ReadAllText(archived));
    }

    [Fact]
    public async Task Si_la_nouvelle_sauvegarde_echoue_la_precedente_est_remise_en_place()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Combine("appdata"));
        var service = new BackupService(paths, new FileCopyService(new HashService()));
        string gameFile = temp.CreateFile("original v1", "Bgm.awb");
        await service.BackupVanillaAsync(gameFile, GameVersion.Standard);

        File.Delete(gameFile);
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.BackupVanillaAsync(gameFile, GameVersion.Standard));

        Assert.Equal("original v1", File.ReadAllText(service.GetBackupFile(GameVersion.Standard)));
        Assert.Empty(Directory.GetFiles(paths.GetArchiveDirectory(GameVersion.Standard)));
    }

    [Fact]
    public async Task Chaque_version_du_jeu_a_sa_propre_sauvegarde()
    {
        using var temp = new TempDirectory();
        var service = new BackupService(new AppPaths(temp.Combine("appdata")), new FileCopyService(new HashService()));
        string gameFile = temp.CreateFile("original", "Bgm.awb");

        await service.BackupVanillaAsync(gameFile, GameVersion.Remaster);

        Assert.True(service.BackupExists(GameVersion.Remaster));
        Assert.False(service.BackupExists(GameVersion.Standard));
    }

    [Fact]
    public async Task Deux_archives_creees_dans_la_meme_seconde_ne_s_ecrasent_pas()
    {
        using var temp = new TempDirectory();
        var paths = new AppPaths(temp.Combine("appdata"));
        var service = new BackupService(paths, new FileCopyService(new HashService()));
        string first = temp.CreateFile("premier", "a.awb");
        string second = temp.CreateFile("second", "b.awb");

        string firstArchive = await service.ArchiveAsync(first, GameVersion.Standard);
        string secondArchive = await service.ArchiveAsync(second, GameVersion.Standard);

        Assert.NotEqual(firstArchive, secondArchive);
        Assert.Equal("premier", File.ReadAllText(firstArchive));
        Assert.Equal("second", File.ReadAllText(secondArchive));
    }
}

public class GameProcessTests
{
    [Fact]
    public void Le_jeu_est_vu_comme_lance_quand_son_processus_existe()
    {
        var processes = new FakeProcessProbe();
        var service = new GameService(processes);
        Assert.False(service.IsKakarotRunning());

        processes.Running.Add("AT-Win64-Shipping");

        Assert.True(service.IsKakarotRunning());
    }

    [Fact]
    public void La_sonde_reelle_voit_le_processus_des_tests_et_pas_un_processus_imaginaire()
    {
        var probe = new SystemProcessProbe();
        using Process current = Process.GetCurrentProcess();

        Assert.True(probe.IsRunning(current.ProcessName));
        Assert.False(probe.IsRunning("un-processus-qui-n-existe-pas-" + Guid.NewGuid().ToString("N")));
    }
}
