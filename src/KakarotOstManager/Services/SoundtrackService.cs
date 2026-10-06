using System.IO;
using System.Text.RegularExpressions;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Trouve les bandes-son présentes dans le dossier du pack.</summary>
public sealed class SoundtrackService
{
    // Profondeur suffisante pour une archive extraite dans un dossier du même
    // nom (« 03 - Namek\03 - Namek\Bgm.awb »), sans parcourir tout un disque.
    private const int MaxScanDepth = 3;

    private readonly IReadOnlyList<(SoundtrackMetadata Metadata, Regex Pattern)> _rules;

    /// <exception cref="ArgumentException">Un champ « match » du catalogue n'est pas une expression régulière valide.</exception>
    public SoundtrackService(SoundtrackCatalog catalog)
    {
        _rules = catalog.Soundtracks
            .Select(metadata => (metadata, new Regex(metadata.Match, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
            .ToList();
    }

    /// <summary>
    /// Parcourt le dossier du pack et renvoie une bande-son par sous-dossier
    /// contenant un Bgm.awb, triées par rubrique puis par ordre.
    /// </summary>
    public IReadOnlyList<Soundtrack> FindSoundtracks(string? packPath)
    {
        if (string.IsNullOrWhiteSpace(packPath) || !Directory.Exists(packPath))
        {
            return [];
        }

        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packPath));
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = MaxScanDepth,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
        };

        // Un Bgm.awb posé directement à la racine n'est pas une bande-son du
        // pack : quand le pack est rangé dans le dossier BGM du jeu, c'est le
        // fichier que le jeu utilise.
        var folders = Directory.EnumerateFiles(root, GameService.BgmFileName, options)
            .Select(file => Path.GetDirectoryName(file)!)
            .Where(folder => !string.Equals(folder, root, StringComparison.OrdinalIgnoreCase))
            .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase);

        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var soundtracks = new List<Soundtrack>();

        foreach (string folder in folders)
        {
            string folderName = Path.GetFileName(folder);
            string bgmFile = Path.Combine(folder, GameService.BgmFileName);
            SoundtrackMetadata? metadata = FindMetadata(folderName, usedIds);

            if (metadata is not null)
            {
                usedIds.Add(metadata.Id);
                soundtracks.Add(new Soundtrack
                {
                    Id = metadata.Id,
                    FolderPath = folder,
                    BgmFilePath = bgmFile,
                    Number = metadata.Number,
                    Name = metadata.Name,
                    Category = metadata.Category,
                    Order = metadata.Order,
                    ActivationHint = metadata.ActivationHint,
                });
            }
            else
            {
                string id = MakeUniqueId(folderName, usedIds);
                usedIds.Add(id);
                soundtracks.Add(new Soundtrack
                {
                    Id = id,
                    FolderPath = folder,
                    BgmFilePath = bgmFile,
                    Name = LocalizedText.Same(folderName),
                    Category = SoundtrackCategory.Other,
                });
            }
        }

        return soundtracks
            .OrderBy(soundtrack => soundtrack.Category)
            .ThenBy(soundtrack => soundtrack.Order)
            .ThenBy(soundtrack => soundtrack.FolderName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Bande-son qui précède <paramref name="current"/> dans l'histoire
    /// principale, parmi celles réellement présentes. <c>null</c> s'il n'y en
    /// a pas, ou si <paramref name="current"/> n'appartient pas à l'histoire.
    /// </summary>
    public static Soundtrack? GetPrevious(IReadOnlyList<Soundtrack> soundtracks, Soundtrack current) =>
        GetNeighbor(soundtracks, current, offset: -1);

    /// <summary>Bande-son qui suit <paramref name="current"/> dans l'histoire principale.</summary>
    public static Soundtrack? GetNext(IReadOnlyList<Soundtrack> soundtracks, Soundtrack current) =>
        GetNeighbor(soundtracks, current, offset: +1);

    private static Soundtrack? GetNeighbor(IReadOnlyList<Soundtrack> soundtracks, Soundtrack current, int offset)
    {
        if (current.Category != SoundtrackCategory.Main)
        {
            return null;
        }

        var story = soundtracks
            .Where(soundtrack => soundtrack.Category == SoundtrackCategory.Main)
            .OrderBy(soundtrack => soundtrack.Order)
            .ToList();

        int index = story.FindIndex(soundtrack => soundtrack.Id == current.Id);
        int neighbor = index + offset;

        return index >= 0 && neighbor >= 0 && neighbor < story.Count ? story[neighbor] : null;
    }

    private SoundtrackMetadata? FindMetadata(string folderName, HashSet<string> usedIds)
    {
        foreach (var (metadata, pattern) in _rules)
        {
            // Une métadonnée ne sert qu'une fois : un second dossier du même
            // nom (copie, doublon) est rangé dans « Autres ».
            if (!usedIds.Contains(metadata.Id) && pattern.IsMatch(folderName))
            {
                return metadata;
            }
        }

        return null;
    }

    private static string MakeUniqueId(string folderName, HashSet<string> usedIds)
    {
        string slug = Regex.Replace(folderName.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0)
        {
            slug = "ost";
        }

        string id = slug;
        for (int suffix = 2; usedIds.Contains(id); suffix++)
        {
            id = $"{slug}-{suffix}";
        }

        return id;
    }
}
