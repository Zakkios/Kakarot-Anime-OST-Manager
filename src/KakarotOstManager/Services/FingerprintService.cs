using System.IO;
using System.Text.Json;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>
/// Fournit l'empreinte SHA-256 des fichiers en mémorisant les résultats.
/// Relire dix fichiers de 400 Mo à chaque démarrage serait trop lent : une
/// empreinte n'est recalculée que si la taille ou la date du fichier a changé.
/// </summary>
public sealed class FingerprintService(HashService hashService, AppPaths paths)
{
    private Dictionary<string, FileFingerprint>? _cache;

    /// <exception cref="FileNotFoundException">Le fichier n'existe pas.</exception>
    public async Task<string> GetSha256Async(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Fichier introuvable.", filePath);
        }

        Dictionary<string, FileFingerprint> cache = GetCache();
        if (cache.TryGetValue(info.FullName, out FileFingerprint? known)
            && known.Length == info.Length
            && known.LastWriteTimeUtc == info.LastWriteTimeUtc)
        {
            progress?.Report(1);
            return known.Sha256;
        }

        string sha256 = await hashService.ComputeSha256Async(info.FullName, progress, cancellationToken);
        Store(info, sha256);
        return sha256;
    }

    /// <summary>
    /// Enregistre une empreinte déjà connue, par exemple celle d'un fichier
    /// qu'on vient de copier et de vérifier, pour éviter de le relire.
    /// </summary>
    public void Remember(string filePath, string sha256)
    {
        var info = new FileInfo(filePath);
        if (info.Exists)
        {
            Store(info, sha256);
        }
    }

    private void Store(FileInfo info, string sha256)
    {
        Dictionary<string, FileFingerprint> cache = GetCache();
        cache[info.FullName] = new FileFingerprint(info.Length, info.LastWriteTimeUtc, sha256);

        try
        {
            AtomicFile.WriteAllText(paths.FingerprintCacheFile, JsonSerializer.Serialize(cache, JsonDefaults.Options));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ce fichier n'est qu'un accélérateur : s'il ne peut pas être
            // écrit, l'empreinte sera simplement recalculée la prochaine fois.
        }
    }

    private Dictionary<string, FileFingerprint> GetCache()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        Dictionary<string, FileFingerprint>? loaded = null;
        try
        {
            if (File.Exists(paths.FingerprintCacheFile))
            {
                string json = File.ReadAllText(paths.FingerprintCacheFile);
                loaded = JsonSerializer.Deserialize<Dictionary<string, FileFingerprint>>(json, JsonDefaults.Options);
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Fichier illisible : on repart d'une mémoire vide.
        }

        // Sous Windows, « D:\Pack » et « d:\pack » désignent le même fichier.
        _cache = new Dictionary<string, FileFingerprint>(loaded ?? [], StringComparer.OrdinalIgnoreCase);
        return _cache;
    }
}
