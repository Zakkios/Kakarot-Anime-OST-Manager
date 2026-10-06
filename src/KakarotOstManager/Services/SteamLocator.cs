using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace KakarotOstManager.Services;

/// <summary>Retrouve le dossier du jeu sans le demander à l'utilisateur.</summary>
public interface ISteamLocator
{
    /// <returns>Le dossier d'installation du jeu, ou <c>null</c> s'il n'a pas été trouvé.</returns>
    string? FindGameDirectory();
}

/// <summary>
/// Cherche le jeu dans les bibliothèques Steam. Steam note son dossier dans
/// le registre de Windows, et la liste de ses bibliothèques (une par disque,
/// en général) dans le fichier steamapps\libraryfolders.vdf.
/// </summary>
public sealed class SteamLocator(GameService gameService) : ISteamLocator
{
    public const string GameFolderName = "DRAGON BALL Z KAKAROT";

    public string? FindGameDirectory()
    {
        foreach (string steamPath in GetSteamInstallPaths())
        {
            string? gameDirectory = FindGameDirectory(GetLibraryFolders(steamPath));
            if (gameDirectory is not null)
            {
                return gameDirectory;
            }
        }

        return null;
    }

    /// <summary>Premier dossier du jeu trouvé dans ces bibliothèques, et qui contient bien un dossier BGM.</summary>
    public string? FindGameDirectory(IEnumerable<string> libraryFolders) =>
        libraryFolders
            .Select(library => Path.Combine(library, "steamapps", "common", GameFolderName))
            .FirstOrDefault(candidate => gameService.DetectVersion(candidate) is not null);

    /// <summary>Le dossier de Steam lui-même, suivi des bibliothèques déclarées dans libraryfolders.vdf.</summary>
    public static IReadOnlyList<string> GetLibraryFolders(string steamPath)
    {
        var folders = new List<string> { steamPath };
        string vdfFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        try
        {
            if (File.Exists(vdfFile))
            {
                folders.AddRange(ParseLibraryFolders(File.ReadAllText(vdfFile)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fichier illisible : on se contente du dossier de Steam.
        }

        return folders;
    }

    /// <summary>
    /// Extrait les lignes <c>"path"  "D:\\SteamLibrary"</c> du fichier. Les
    /// barres obliques inverses y sont doublées.
    /// </summary>
    public static IReadOnlyList<string> ParseLibraryFolders(string vdfContent) =>
        Regex.Matches(vdfContent, "\"path\"\\s+\"([^\"]*)\"", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.Replace(@"\\", @"\"))
            .Where(path => path.Length > 0)
            .ToList();

    private static IEnumerable<string> GetSteamInstallPaths()
    {
        string?[] candidates =
        [
            ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
            ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
            ReadRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"),
        ];

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? ReadRegistry(RegistryKey root, string keyPath, string valueName)
    {
        try
        {
            using RegistryKey? key = root.OpenSubKey(keyPath);
            return key?.GetValue(valueName) as string;
        }
        catch (Exception ex) when (ex is SecurityException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
