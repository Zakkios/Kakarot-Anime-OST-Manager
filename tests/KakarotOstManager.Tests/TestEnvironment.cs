using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

/// <summary>
/// Bac à sable complet pour les tests d'installation : un faux jeu, un faux
/// pack de trois bandes-son et un dossier de données, le tout dans un dossier
/// temporaire, avec les vrais services branchés dessus.
/// </summary>
internal sealed class TestEnvironment : IDisposable
{
    public const string OriginalMusic = "musique originale du jeu";

    public TestEnvironment(GameVersion version = GameVersion.Standard, HashService? hashService = null)
    {
        Game = new GameService(Processes, Shell);
        Paths = new AppPaths(Temp.Combine("appdata"));
        HashService = hashService ?? new HashService();
        Copier = new FileCopyService(HashService);
        Fingerprints = new FingerprintService(HashService, Paths);
        Backups = new BackupService(Paths, Copier);
        SettingsService = new SettingsService(Paths);
        Installer = new InstallService(Game, Backups, Copier, Fingerprints, SettingsService);

        string gamePath = Temp.CreateDirectory("game");
        string packPath = Temp.CreateDirectory("pack");
        Settings = new AppSettings { GamePath = gamePath, SoundtrackPath = packPath, GameVersion = version };

        Directory.CreateDirectory(Game.GetBgmDirectory(gamePath, version));
        File.WriteAllText(GameFile, Content(OriginalMusic));

        foreach (string name in new[] { "01 - Raditz", "03 - Namek", "05 - Cell" })
        {
            Temp.CreateFile(Content($"musique {name}"), "pack", name, "Bgm.awb");
        }

        var catalog = SoundtrackCatalogLoader.LoadEmbedded();
        Pack = new SoundtrackService(catalog).FindSoundtracks(packPath);
    }

    public TempDirectory Temp { get; } = new();

    public FakeProcessProbe Processes { get; } = new();

    public FakeShellLauncher Shell { get; } = new();

    public AppPaths Paths { get; }

    public GameService Game { get; }

    public HashService HashService { get; }

    public FileCopyService Copier { get; }

    public FingerprintService Fingerprints { get; }

    public BackupService Backups { get; }

    public SettingsService SettingsService { get; }

    public InstallService Installer { get; }

    public AppSettings Settings { get; }

    public IReadOnlyList<Soundtrack> Pack { get; }

    public Soundtrack Raditz => Pack.Single(soundtrack => soundtrack.Id == "01-raditz");

    public Soundtrack Namek => Pack.Single(soundtrack => soundtrack.Id == "03-namek");

    public Soundtrack Cell => Pack.Single(soundtrack => soundtrack.Id == "05-cell");

    /// <summary>Le Bgm.awb que le faux jeu « utilise ».</summary>
    public string GameFile => Game.GetBgmFilePath(Settings.GamePath!, Settings.GameVersion!.Value);

    public string BackupFile => Backups.GetBackupFile(Settings.GameVersion!.Value);

    public string ArchiveDirectory => Paths.GetArchiveDirectory(Settings.GameVersion!.Value);

    public string GameFileContent => File.ReadAllText(GameFile);

    /// <summary>
    /// Comme les vrais Bgm.awb, tous les faux fichiers ont la même taille :
    /// seul leur contenu les distingue.
    /// </summary>
    public static string Content(string text) => text.PadRight(64, '.');

    public static string ContentOf(Soundtrack soundtrack) => File.ReadAllText(soundtrack.BgmFilePath);

    /// <summary>
    /// Simule un remplacement du fichier du jeu fait en dehors de l'application
    /// (mise à jour Steam, copie à la main). La date de modification est
    /// avancée explicitement : dans la réalité ce changement arrive bien plus
    /// tard, alors qu'un test l'enchaîne en quelques microsecondes.
    /// </summary>
    public void ReplaceGameFileExternally(string content)
    {
        File.WriteAllText(GameFile, content);
        File.SetLastWriteTimeUtc(GameFile, DateTime.UtcNow.AddMinutes(1));
    }

    /// <summary>Simule le jeu en cours d'exécution.</summary>
    public void StartGame() => Processes.Running.Add(GameService.GameProcessName);

    /// <summary>Fichiers provisoires oubliés dans le dossier BGM du jeu.</summary>
    public string[] LeftoverTempFiles() => Directory.GetFiles(Path.GetDirectoryName(GameFile)!, "*.tmp");

    public void Dispose() => Temp.Dispose();
}

internal sealed class FakeProcessProbe : IProcessProbe
{
    public HashSet<string> Running { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning(string processName) => Running.Contains(processName);
}

/// <summary>Note ce que l'application aurait demandé à Windows d'ouvrir, sans rien ouvrir.</summary>
internal sealed class FakeShellLauncher : IShellLauncher
{
    public List<string> Opened { get; } = [];

    /// <summary>Erreur à simuler, par exemple quand Steam n'est pas installé.</summary>
    public Exception? Failure { get; set; }

    public void Open(string target)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Opened.Add(target);
    }
}

internal sealed class FakeSteamLocator : ISteamLocator
{
    /// <summary>Dossier que « Steam » indiquera ; <c>null</c> si le jeu est introuvable.</summary>
    public string? GameDirectory { get; set; }

    public string? FindGameDirectory() => GameDirectory;
}

/// <summary>Enregistre chaque valeur d'avancement reçue, immédiatement et dans l'ordre.</summary>
internal sealed class RecordingProgress<T> : IProgress<T>
{
    public List<T> Values { get; } = [];

    public void Report(T value) => Values.Add(value);
}
