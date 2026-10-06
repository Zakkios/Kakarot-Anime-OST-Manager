using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class HashServiceTests
{
    private readonly HashService _service = new();

    [Fact]
    public async Task L_empreinte_correspond_a_la_valeur_SHA256_de_reference()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "fichier.bin");

        string hash = await _service.ComputeSha256Async(file);

        // Valeur publiée dans la norme SHA-256 pour le texte « abc ».
        Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", hash);
    }

    [Fact]
    public async Task Deux_fichiers_de_meme_taille_mais_de_contenu_different_ont_des_empreintes_differentes()
    {
        using var temp = new TempDirectory();
        string namek = temp.CreateFile("musique de Namek", "namek.awb");
        string cell = temp.CreateFile("musique de Cell.", "cell.awb");
        Assert.Equal(new FileInfo(namek).Length, new FileInfo(cell).Length);

        Assert.NotEqual(await _service.ComputeSha256Async(namek), await _service.ComputeSha256Async(cell));
    }

    [Fact]
    public async Task Un_fichier_vide_a_une_empreinte()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("", "vide.bin");

        string hash = await _service.ComputeSha256Async(file);

        Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", hash);
    }

    [Fact]
    public async Task L_avancement_progresse_jusqu_a_1()
    {
        using var temp = new TempDirectory();
        // 2,5 Mo : le fichier est lu en trois blocs de 1 Mo au plus.
        string file = temp.Combine("gros.bin");
        File.WriteAllBytes(file, new byte[2_500_000]);
        var progress = new RecordingProgress<double>();

        await _service.ComputeSha256Async(file, progress);

        Assert.True(progress.Values.Count >= 3);
        Assert.Equal(progress.Values.Order(), progress.Values);
        Assert.Equal(1, progress.Values[^1]);
    }

    [Fact]
    public async Task Le_calcul_peut_etre_annule()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("abc", "fichier.bin");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ComputeSha256Async(file, cancellationToken: cancellation.Token));
    }
}
