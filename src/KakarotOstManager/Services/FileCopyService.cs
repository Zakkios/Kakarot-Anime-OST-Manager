using System.IO;
using System.Security.Cryptography;

namespace KakarotOstManager.Services;

/// <summary>
/// Remplace un fichier par la copie d'un autre, sans jamais laisser la
/// destination dans un état intermédiaire.
/// </summary>
public sealed class FileCopyService(HashService hashService)
{
    private const int BufferSize = 1024 * 1024;

    /// <summary>
    /// Copie <paramref name="sourceFile"/> vers <paramref name="destinationFile"/>
    /// en trois temps : copie dans un fichier provisoire voisin, relecture de
    /// ce fichier pour comparer son empreinte à celle de la source, puis
    /// renommage qui remplace la destination d'un seul coup. En cas d'échec à
    /// n'importe quel moment, la destination d'origine est intacte.
    /// </summary>
    /// <returns>L'empreinte SHA-256 du contenu copié.</returns>
    /// <exception cref="FileVerificationException">La copie diffère de la source.</exception>
    public async Task<string> CopyVerifiedAsync(
        string sourceFile,
        string destinationFile,
        IProgress<double>? copyProgress = null,
        IProgress<double>? verifyProgress = null,
        CancellationToken cancellationToken = default)
    {
        string tempFile = destinationFile + ".tmp";
        try
        {
            string sourceHash = await CopyAndHashAsync(sourceFile, tempFile, copyProgress, cancellationToken);
            string copyHash = await hashService.ComputeSha256Async(tempFile, verifyProgress, cancellationToken);
            if (copyHash != sourceHash)
            {
                throw new FileVerificationException(sourceFile, tempFile);
            }

            // Sur un même disque, ce renommage est atomique : le jeu voit
            // soit l'ancien fichier, soit le nouveau, jamais un fichier partiel.
            File.Move(tempFile, destinationFile, overwrite: true);
            return sourceHash;
        }
        catch
        {
            TryDelete(tempFile);
            throw;
        }
    }

    /// <summary>Copie le fichier et calcule l'empreinte de ce qui a été lu, en une seule lecture.</summary>
    private static async Task<string> CopyAndHashAsync(
        string sourceFile, string tempFile, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read,
            BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(
            tempFile, FileMode.Create, FileAccess.Write, FileShare.None,
            BufferSize, FileOptions.Asynchronous);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[BufferSize];
        long totalBytes = source.Length;
        long bytesDone = 0;

        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            hash.AppendData(buffer, 0, bytesRead);
            bytesDone += bytesRead;
            progress?.Report((double)bytesDone / totalBytes);
        }

        // Force l'écriture physique sur le disque avant le renommage.
        await destination.FlushAsync(cancellationToken);
        destination.Flush(flushToDisk: true);

        progress?.Report(1);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Le fichier provisoire sera écrasé à la prochaine tentative.
        }
    }
}
