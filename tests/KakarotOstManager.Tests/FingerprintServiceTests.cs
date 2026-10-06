using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class FingerprintServiceTests
{
    private const string AbcSha256 = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

    [Fact]
    public async Task L_empreinte_d_un_fichier_inchange_n_est_calculee_qu_une_fois()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var hasher = new CountingHashService();
        var service = new FingerprintService(hasher, new AppPaths(temp.Combine("appdata")));

        string first = await service.GetSha256Async(file);
        string second = await service.GetSha256Async(file);

        Assert.Equal(AbcSha256, first);
        Assert.Equal(first, second);
        Assert.Equal(1, hasher.Calls);
    }

    [Fact]
    public async Task L_empreinte_est_recalculee_quand_le_fichier_change()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var hasher = new CountingHashService();
        var service = new FingerprintService(hasher, new AppPaths(temp.Combine("appdata")));
        await service.GetSha256Async(file);

        // Même taille, contenu différent, date de modification différente.
        File.WriteAllText(file, "xyz");
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(1));
        string after = await service.GetSha256Async(file);

        Assert.NotEqual(AbcSha256, after);
        Assert.Equal(2, hasher.Calls);
    }

    [Fact]
    public async Task La_memoire_survit_a_un_redemarrage_de_l_application()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var paths = new AppPaths(temp.Combine("appdata"));
        await new FingerprintService(new HashService(), paths).GetSha256Async(file);

        var hasher = new CountingHashService();
        string reloaded = await new FingerprintService(hasher, paths).GetSha256Async(file);

        Assert.Equal(AbcSha256, reloaded);
        Assert.Equal(0, hasher.Calls);
    }

    [Fact]
    public async Task Le_chemin_est_reconnu_quelle_que_soit_sa_casse()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var hasher = new CountingHashService();
        var service = new FingerprintService(hasher, new AppPaths(temp.Combine("appdata")));

        await service.GetSha256Async(file);
        await service.GetSha256Async(file.ToUpperInvariant());

        Assert.Equal(1, hasher.Calls);
    }

    [Fact]
    public async Task Une_empreinte_deja_connue_peut_etre_enregistree_sans_relire_le_fichier()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var hasher = new CountingHashService();
        var service = new FingerprintService(hasher, new AppPaths(temp.Combine("appdata")));

        service.Remember(file, AbcSha256);
        string hash = await service.GetSha256Async(file);

        Assert.Equal(AbcSha256, hash);
        Assert.Equal(0, hasher.Calls);
    }

    [Fact]
    public async Task Une_memoire_abimee_est_ignoree()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "Bgm.awb");
        var paths = new AppPaths(temp.Combine("appdata"));
        temp.CreateFile("{ pas du JSON", "appdata", "fingerprints.json");

        string hash = await new FingerprintService(new HashService(), paths).GetSha256Async(file);

        Assert.Equal(AbcSha256, hash);
    }

    [Fact]
    public async Task Un_fichier_absent_leve_une_erreur()
    {
        using var temp = new TempDirectory();
        var service = new FingerprintService(new HashService(), new AppPaths(temp.Combine("appdata")));

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.GetSha256Async(temp.Combine("absent.awb")));
    }

    private sealed class CountingHashService : HashService
    {
        public int Calls { get; private set; }

        public override Task<string> ComputeSha256Async(
            string filePath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            return base.ComputeSha256Async(filePath, progress, cancellationToken);
        }
    }
}
