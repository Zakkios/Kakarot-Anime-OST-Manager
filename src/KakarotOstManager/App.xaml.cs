using System.IO;
using System.Text.Json;
using System.Windows;
using KakarotOstManager.Localization;
using KakarotOstManager.Models;
using KakarotOstManager.Services;
using KakarotOstManager.ViewModels;
using KakarotOstManager.Views;

namespace KakarotOstManager;

/// <summary>
/// Point d'entrée de l'application. C'est ici, et seulement ici, que les
/// services sont créés et reliés entre eux.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Option de ligne de commande <c>--data-dir &lt;dossier&gt;</c> : range les
    /// paramètres et les sauvegardes ailleurs que dans %LOCALAPPDATA%. Utile
    /// pour essayer l'application sans toucher à sa vraie configuration.
    /// </summary>
    private const string DataDirectoryOption = "--data-dir";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppPaths paths = GetAppPaths(e.Args);
        var hashService = new HashService();
        var copier = new FileCopyService(hashService);
        var gameService = new GameService();
        var backupService = new BackupService(paths, copier);
        var settingsService = new SettingsService(paths);
        var fingerprints = new FingerprintService(hashService, paths);
        var installService = new InstallService(gameService, backupService, copier, fingerprints, settingsService);

        // Un soundtracks.json illisible ne doit pas empêcher de démarrer : les
        // bandes-son restent utilisables, rangées dans « Autres ».
        Exception? catalogError = null;
        SoundtrackService soundtrackService;
        try
        {
            soundtrackService = new SoundtrackService(SoundtrackCatalogLoader.LoadDefault());
        }
        catch (Exception ex) when (ex is IOException or JsonException or ArgumentException)
        {
            catalogError = ex;
            soundtrackService = new SoundtrackService(new SoundtrackCatalog());
        }

        var viewModel = new MainViewModel(
            settingsService, gameService, soundtrackService, installService, backupService,
            new FolderPicker(), new SteamLocator(gameService), Localizer.Instance);

        var window = new MainWindow { DataContext = viewModel };
        window.Show();

        await viewModel.InitializeAsync(catalogError);
    }

    private static AppPaths GetAppPaths(string[] args)
    {
        int index = Array.IndexOf(args, DataDirectoryOption);
        return index >= 0 && index + 1 < args.Length
            ? new AppPaths(Path.GetFullPath(args[index + 1]))
            : AppPaths.Default;
    }
}
