using System.IO;
using System.Security.Cryptography;

namespace KakarotOstManager.Services;

/// <summary>
/// Calcule l'empreinte SHA-256 d'un fichier. Deux Bgm.awb du mod ont la même
/// taille : seule l'empreinte permet de savoir lequel est installé.
/// </summary>
public sealed class HashService
{
    private const int BufferSize = 1024 * 1024;

    /// <param name="progress">Reçoit l'avancement, de 0 à 1.</param>
    /// <returns>L'empreinte en hexadécimal majuscule (64 caractères).</returns>
    public async Task<string> ComputeSha256Async(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[BufferSize];
        long totalBytes = stream.Length;
        long bytesDone = 0;

        // Le fichier est lu par blocs de 1 Mo : un Bgm.awb pèse environ 400 Mo,
        // il n'est pas question de le charger entièrement en mémoire.
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, bytesRead);
            bytesDone += bytesRead;
            progress?.Report((double)bytesDone / totalBytes);
        }

        progress?.Report(1);
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
