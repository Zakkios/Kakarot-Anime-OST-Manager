using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakarotOstManager.Localization;
using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.ViewModels;

/// <summary>
/// État et actions de la fenêtre principale. La fenêtre (XAML) se contente
/// d'afficher ces propriétés et de déclencher ces commandes : aucune règle
/// du logiciel n'est écrite dans la vue.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly GameService _gameService;
    private readonly SoundtrackService _soundtrackService;
    private readonly InstallService _installService;
    private readonly BackupService _backupService;
    private readonly IFolderPicker _folderPicker;
    private readonly ISteamLocator _steamLocator;
    private readonly Localizer _loc;

    private AppSettings _settings = new();
    private IReadOnlyList<Soundtrack> _pack = [];
    private InstalledState _installed = InstalledState.NoGameFile;
    private IReadOnlyList<CheckResult> _lastChecks = [];
    private InstallStep? _currentStep;
    private Soundtrack? _nextUp;
    private bool _applyingLanguageFromSettings;

    // Le message d'état est mémorisé sous forme de fonction : elle est
    // rappelée quand la langue change, pour le retraduire.
    private Func<string>? _statusText;

    public MainViewModel(
        SettingsService settingsService,
        GameService gameService,
        SoundtrackService soundtrackService,
        InstallService installService,
        BackupService backupService,
        IFolderPicker folderPicker,
        ISteamLocator steamLocator,
        Localizer localizer)
    {
        _settingsService = settingsService;
        _gameService = gameService;
        _soundtrackService = soundtrackService;
        _installService = installService;
        _backupService = backupService;
        _folderPicker = folderPicker;
        _steamLocator = steamLocator;
        _loc = localizer;

        GamePath = "";
        GamePathError = "";
        SoundtrackPath = "";
        SoundtrackPathError = "";
        SelectedTitle = "";
        SelectedHint = "";
        SelectedFolder = "";
        SelectedPrevious = "";
        SelectedNext = "";
        InstalledTitle = "";
        NextUpTitle = "";
        StatusMessage = "";
        ProgressText = "";

        // Affecter la langue affichée ne doit pas compter comme un choix de
        // l'utilisateur : rien ne doit être enregistré tant que les
        // paramètres n'ont pas été lus.
        _applyingLanguageFromSettings = true;
        SelectedLanguage = Languages.First(language => language.Code == _loc.Language);
        _applyingLanguageFromSettings = false;
    }

    // ------------------------------------------------------------ état affiché

    public ObservableCollection<SoundtrackItemViewModel> Soundtracks { get; } = [];

    public ObservableCollection<CheckItemViewModel> Checks { get; } = [];

    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new LanguageOption("fr", "Français"),
        new LanguageOption("en", "English"),
    ];

    [ObservableProperty]
    public partial LanguageOption SelectedLanguage { get; set; }

    [ObservableProperty]
    public partial string GamePath { get; set; }

    [ObservableProperty]
    public partial string GamePathError { get; set; }

    [ObservableProperty]
    public partial string SoundtrackPath { get; set; }

    [ObservableProperty]
    public partial string SoundtrackPathError { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial SoundtrackItemViewModel? SelectedSoundtrack { get; set; }

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial string SelectedTitle { get; set; }

    [ObservableProperty]
    public partial string SelectedHint { get; set; }

    [ObservableProperty]
    public partial string SelectedFolder { get; set; }

    /// <summary>Vrai pour un chapitre de l'histoire : lui seul a une précédente et une suivante.</summary>
    [ObservableProperty]
    public partial bool HasSelectedNeighbors { get; set; }

    [ObservableProperty]
    public partial string SelectedPrevious { get; set; }

    [ObservableProperty]
    public partial string SelectedNext { get; set; }

    /// <summary>Titre de la bande-son qui suit celle actuellement installée ; vide s'il n'y en a pas.</summary>
    [ObservableProperty]
    public partial string NextUpTitle { get; set; }

    [ObservableProperty]
    public partial bool HasNextUp { get; set; }

    /// <summary>Ce que contient réellement le jeu, par exemple « 03 — Namek ».</summary>
    [ObservableProperty]
    public partial string InstalledTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    public partial bool CanRestoreBackup { get; set; }

    [ObservableProperty]
    public partial StatusKind Status { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    /// <summary>Vrai pendant une analyse ou une copie : les boutons sont alors désactivés.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseGameFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(BrowseSoundtrackFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(LaunchGameCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; }

    /// <summary>Avancement de l'étape en cours, de 0 à 1.</summary>
    [ObservableProperty]
    public partial double ProgressFraction { get; set; }

    /// <summary>Vrai quand la durée de l'étape n'est pas mesurable : la barre défile en continu.</summary>
    [ObservableProperty]
    public partial bool IsProgressIndeterminate { get; set; }

    public bool IsIdle => !IsBusy;

    public bool IsStandard
    {
        get => _settings.GameVersion == GameVersion.Standard;
        set
        {
            if (value)
            {
                _ = ChangeGameVersionAsync(GameVersion.Standard);
            }
        }
    }

    public bool IsRemaster
    {
        get => _settings.GameVersion == GameVersion.Remaster;
        set
        {
            if (value)
            {
                _ = ChangeGameVersionAsync(GameVersion.Remaster);
            }
        }
    }

    // ------------------------------------------------------------ démarrage

    /// <summary>Charge les paramètres, liste les bandes-son et identifie celle qui est en place.</summary>
    /// <param name="catalogError">Erreur rencontrée en lisant soundtracks.json, s'il y en a eu une.</param>
    public async Task InitializeAsync(Exception? catalogError = null)
    {
        _settings = _settingsService.Load();

        _applyingLanguageFromSettings = true;
        if (_settings.Language is not null)
        {
            _loc.Language = _settings.Language;
        }

        SelectedLanguage = Languages.First(language => language.Code == _loc.Language);
        _applyingLanguageFromSettings = false;

        bool autoDetected = false;
        if (string.IsNullOrWhiteSpace(_settings.GamePath) && _steamLocator.FindGameDirectory() is { } gameDirectory)
        {
            _settings.GamePath = gameDirectory;
            autoDetected = true;
        }

        _settings.GameVersion ??= _gameService.DetectVersion(_settings.GamePath);
        autoDetected |= TryDetectPackInGameFolder();
        if (autoDetected)
        {
            _settingsService.Save(_settings);
        }

        RefreshGameFields();
        ReloadSoundtracks();
        await RefreshInstalledAsync(showAnalyzing: true);

        SelectedSoundtrack ??= Soundtracks.FirstOrDefault(item => item.IsActive) ?? Soundtracks.FirstOrDefault();

        if (catalogError is not null)
        {
            SetStatus(StatusKind.Error, () => _loc.Format("ErrorCatalog", catalogError.Message));
        }
        else if (autoDetected)
        {
            SetStatus(StatusKind.Info, () => _loc["StatusAutoDetected"]);
        }
    }

    /// <summary>
    /// Beaucoup de joueurs extraient le pack directement dans le dossier BGM
    /// du jeu. Si aucun dossier de pack n'est encore choisi et que des
    /// bandes-son s'y trouvent, on le propose d'office.
    /// </summary>
    private bool TryDetectPackInGameFolder()
    {
        if (!string.IsNullOrWhiteSpace(_settings.SoundtrackPath)
            || string.IsNullOrWhiteSpace(_settings.GamePath)
            || _settings.GameVersion is null)
        {
            return false;
        }

        string bgmDirectory = _gameService.GetBgmDirectory(_settings.GamePath, _settings.GameVersion.Value);
        if (_soundtrackService.FindSoundtracks(bgmDirectory).Count == 0)
        {
            return false;
        }

        _settings.SoundtrackPath = bgmDirectory;
        return true;
    }

    // ------------------------------------------------------------ commandes

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task BrowseGameFolderAsync()
    {
        string? folder = _folderPicker.PickFolder(_loc["PickGameFolderTitle"], _settings.GamePath);
        if (folder is null)
        {
            return;
        }

        _settings.GamePath = folder;
        // Nouveau dossier : la version est redétectée plutôt que conservée.
        _settings.GameVersion = _gameService.DetectVersion(folder) ?? _settings.GameVersion;
        bool packDetected = TryDetectPackInGameFolder();
        _settingsService.Save(_settings);

        RefreshGameFields();
        if (packDetected)
        {
            ReloadSoundtracks();
        }

        await RefreshInstalledAsync(showAnalyzing: true);
        SelectedSoundtrack ??= Soundtracks.FirstOrDefault(item => item.IsActive) ?? Soundtracks.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task BrowseSoundtrackFolderAsync()
    {
        string? folder = _folderPicker.PickFolder(_loc["PickPackFolderTitle"], _settings.SoundtrackPath);
        if (folder is null)
        {
            return;
        }

        _settings.SoundtrackPath = folder;
        _settingsService.Save(_settings);

        ReloadSoundtracks();
        await RefreshInstalledAsync(showAnalyzing: true);
        SelectedSoundtrack ??= Soundtracks.FirstOrDefault(item => item.IsActive) ?? Soundtracks.FirstOrDefault();
    }

    private bool CanApply() => IsIdle && SelectedSoundtrack is not null;

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        Soundtrack soundtrack = SelectedSoundtrack!.Soundtrack;

        await RunOperationAsync(
            title: () => _loc.Format("InstallingTitle", TitleOf(soundtrack)),
            operation: progress => _installService.InstallAsync(_settings, soundtrack, _pack, progress),
            successText: () => _loc.Format("SuccessInstalled", TitleOf(soundtrack)),
            soundtrack);
    }

    private bool CanRestore() => IsIdle && CanRestoreBackup;

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync()
    {
        await RunOperationAsync(
            title: () => _loc["RestoringTitle"],
            operation: progress => _installService.RestoreVanillaAsync(_settings, _pack, progress),
            successText: () => _loc["SuccessRestored"],
            soundtrack: null);
    }

    private bool CanInstallNext() => IsIdle && _nextUp is not null;

    /// <summary>Installe la bande-son qui suit, dans l'histoire, celle actuellement en place.</summary>
    [RelayCommand(CanExecute = nameof(CanInstallNext))]
    private async Task InstallNextAsync()
    {
        string nextId = _nextUp!.Id;
        SelectedSoundtrack = Soundtracks.FirstOrDefault(item => item.Soundtrack.Id == nextId);
        if (SelectedSoundtrack is not null)
        {
            await ApplyAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void LaunchGame()
    {
        try
        {
            _gameService.LaunchKakarot();
            SetStatus(StatusKind.Info, () => _loc["StatusLaunching"]);
        }
        catch (Exception ex)
        {
            SetStatus(StatusKind.Error, () => _loc.Format("ErrorLaunchFailed", ex.Message));
        }
    }

    // ------------------------------------------------------------ réactions

    partial void OnSelectedSoundtrackChanged(SoundtrackItemViewModel? value) => RefreshSelection();

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (_applyingLanguageFromSettings)
        {
            return;
        }

        _loc.Language = value.Code;
        _settings.Language = value.Code;
        _settingsService.Save(_settings);
        RefreshTexts();
    }

    private async Task ChangeGameVersionAsync(GameVersion version)
    {
        if (_settings.GameVersion == version || IsBusy)
        {
            return;
        }

        _settings.GameVersion = version;
        _settingsService.Save(_settings);

        RefreshGameFields();
        await RefreshInstalledAsync(showAnalyzing: true);
    }

    // ------------------------------------------------------------ opérations

    private async Task RunOperationAsync(
        Func<string> title,
        Func<IProgress<InstallProgress>, Task<OperationResult>> operation,
        Func<string> successText,
        Soundtrack? soundtrack)
    {
        IsBusy = true;
        _lastChecks = [];
        RebuildChecks();
        SetStatus(StatusKind.Info, title);
        ShowStep(new InstallProgress(InstallStep.Analyzing, 0));

        try
        {
            var progress = new UiProgress(ShowStep);

            // Task.Run exécute le travail hors du fil de l'interface : la
            // fenêtre reste fluide pendant la copie des 400 Mo.
            OperationResult result = await Task.Run(() => operation(progress));

            _lastChecks = result.Checks;
            RebuildChecks();
            SetStatus(
                result.Succeeded ? StatusKind.Success : StatusKind.Error,
                result.Succeeded ? successText : DescribeFailure(result, soundtrack));
        }
        catch (Exception ex)
        {
            // Erreur imprévue : on l'affiche plutôt que de fermer l'application.
            SetStatus(StatusKind.Error, () => _loc.Format("ErrorUnexpected", ex.Message));
        }

        await RefreshInstalledAsync(showAnalyzing: false);
        IsBusy = false;
    }

    /// <summary>Compare le fichier du jeu à la sauvegarde et au pack, puis met l'affichage à jour.</summary>
    private async Task RefreshInstalledAsync(bool showAnalyzing)
    {
        bool wasBusy = IsBusy;
        IsBusy = true;
        if (showAnalyzing)
        {
            SetStatus(StatusKind.Info, () => _loc["StatusAnalyzing"]);
            IsProgressIndeterminate = true;
            ProgressText = "";
        }

        try
        {
            _installed = await Task.Run(() => _installService.IdentifyInstalledAsync(_settings, _pack));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _installed = InstalledState.Unknown;
        }

        // Les paramètres suivent ce qui est réellement sur le disque, par
        // exemple après un remplacement fait à la main.
        string? actualId = _installed.Kind switch
        {
            InstalledKind.Soundtrack => _installed.Soundtrack!.Id,
            InstalledKind.Vanilla => AppSettings.VanillaId,
            _ => _settings.CurrentSoundtrackId,
        };
        if (actualId != _settings.CurrentSoundtrackId)
        {
            _settings.CurrentSoundtrackId = actualId;
            _settingsService.Save(_settings);
        }

        CanRestoreBackup = _settings.GameVersion is not null && _backupService.BackupExists(_settings.GameVersion.Value);
        InstalledTitle = DescribeInstalled();
        foreach (SoundtrackItemViewModel item in Soundtracks)
        {
            item.IsActive = _installed.Soundtrack?.Id == item.Soundtrack.Id;
        }

        RefreshNextUp();

        if (showAnalyzing)
        {
            SetStatus(StatusKind.None, null);
        }

        IsProgressIndeterminate = false;
        IsBusy = wasBusy;
    }

    private void ReloadSoundtracks()
    {
        _pack = _soundtrackService.FindSoundtracks(_settings.SoundtrackPath);
        SoundtrackPath = _settings.SoundtrackPath ?? "";
        RebuildSoundtrackItems();
        RefreshSoundtrackPathError();
    }

    private void RebuildSoundtrackItems()
    {
        string? selectedId = SelectedSoundtrack?.Soundtrack.Id;

        Soundtracks.Clear();
        foreach (Soundtrack soundtrack in _pack)
        {
            Soundtracks.Add(new SoundtrackItemViewModel(soundtrack, TitleOf(soundtrack), _loc[$"Category{soundtrack.Category}"])
            {
                IsActive = _installed.Soundtrack?.Id == soundtrack.Id,
            });
        }

        SelectedSoundtrack = Soundtracks.FirstOrDefault(item => item.Soundtrack.Id == selectedId);
    }

    private void ShowStep(InstallProgress progress)
    {
        _currentStep = progress.Step;
        ProgressText = _loc[$"Step{progress.Step}"];
        ProgressFraction = progress.Fraction;
        IsProgressIndeterminate = progress.Step == InstallStep.Analyzing;
    }

    // ------------------------------------------------------------ textes

    /// <summary>Recalcule tous les textes qui dépendent de la langue.</summary>
    private void RefreshTexts()
    {
        RefreshGameFields();
        RefreshSoundtrackPathError();
        RebuildSoundtrackItems();
        RefreshSelection();
        InstalledTitle = DescribeInstalled();
        RefreshNextUp();
        StatusMessage = _statusText?.Invoke() ?? "";
        RebuildChecks();
        if (_currentStep is not null)
        {
            ProgressText = _loc[$"Step{_currentStep}"];
        }
    }

    private void RefreshGameFields()
    {
        GamePath = _settings.GamePath ?? "";
        OnPropertyChanged(nameof(IsStandard));
        OnPropertyChanged(nameof(IsRemaster));

        if (string.IsNullOrWhiteSpace(_settings.GamePath))
        {
            GamePathError = "";
        }
        else if (_gameService.DetectVersion(_settings.GamePath) is null)
        {
            GamePathError = _loc["ErrorGameFolderInvalid"];
        }
        else if (_settings.GameVersion is not null
            && _gameService.ValidateGameDirectory(_settings.GamePath, _settings.GameVersion.Value) != GameDirectoryStatus.Valid)
        {
            GamePathError = _loc["ErrorBgmFolderMissing"];
        }
        else
        {
            GamePathError = "";
        }
    }

    private void RefreshSoundtrackPathError() =>
        SoundtrackPathError = !string.IsNullOrWhiteSpace(_settings.SoundtrackPath) && _pack.Count == 0
            ? _loc["ErrorNoSoundtrackFound"]
            : "";

    private void RefreshSelection()
    {
        Soundtrack? soundtrack = SelectedSoundtrack?.Soundtrack;
        HasSelection = soundtrack is not null;
        SelectedTitle = soundtrack is null ? "" : TitleOf(soundtrack);
        SelectedFolder = soundtrack?.FolderName ?? "";

        string hint = soundtrack?.ActivationHint.Get(_loc.Language) ?? "";
        SelectedHint = hint.Length > 0 ? hint : _loc["NoHintYet"];

        HasSelectedNeighbors = soundtrack?.Category == SoundtrackCategory.Main;
        SelectedPrevious = TitleOrNone(soundtrack is null ? null : SoundtrackService.GetPrevious(_pack, soundtrack));
        SelectedNext = TitleOrNone(soundtrack is null ? null : SoundtrackService.GetNext(_pack, soundtrack));
    }

    /// <summary>
    /// Détermine la bande-son à proposer ensuite : celle qui suit la bande-son
    /// en place, ou le premier chapitre tant que la musique d'origine est là.
    /// </summary>
    private void RefreshNextUp()
    {
        _nextUp = _installed.Kind switch
        {
            InstalledKind.Soundtrack => SoundtrackService.GetNext(_pack, _installed.Soundtrack!),
            InstalledKind.Vanilla or InstalledKind.Unknown =>
                _pack.FirstOrDefault(soundtrack => soundtrack.Category == SoundtrackCategory.Main),
            _ => null,
        };

        HasNextUp = _nextUp is not null;
        NextUpTitle = _nextUp is null ? "" : TitleOf(_nextUp);
        InstallNextCommand.NotifyCanExecuteChanged();
    }

    private string TitleOrNone(Soundtrack? soundtrack) => soundtrack is null ? _loc["NoneValue"] : TitleOf(soundtrack);

    private void RebuildChecks()
    {
        Checks.Clear();
        foreach (CheckResult check in _lastChecks)
        {
            Checks.Add(new CheckItemViewModel(_loc[$"Check{check.Id}"], check.Passed));
        }
    }

    private void SetStatus(StatusKind kind, Func<string>? text)
    {
        _statusText = text;
        Status = kind;
        StatusMessage = text?.Invoke() ?? "";
    }

    private string TitleOf(Soundtrack soundtrack)
    {
        string name = soundtrack.Name.Get(_loc.Language);
        return soundtrack.Number is null ? name : $"{soundtrack.Number} — {name}";
    }

    private string DescribeInstalled()
    {
        if (string.IsNullOrWhiteSpace(_settings.GamePath) || _settings.GameVersion is null)
        {
            return _loc["InstalledNotConfigured"];
        }

        return _installed.Kind switch
        {
            InstalledKind.Soundtrack => TitleOf(_installed.Soundtrack!),
            InstalledKind.Vanilla => _loc["InstalledVanilla"],
            InstalledKind.Unknown when !CanRestoreBackup => _loc["InstalledUnknownNoBackup"],
            InstalledKind.Unknown => _loc["InstalledUnknown"],
            _ => _loc["InstalledNoFile"],
        };
    }

    private Func<string> DescribeFailure(OperationResult result, Soundtrack? soundtrack) => result.Status switch
    {
        OperationStatus.PrecheckFailed => DescribeFailedCheck(result.Checks.First(check => !check.Passed).Id, soundtrack),
        OperationStatus.BackupFailed => () => _loc["ErrorBackupFailed"],
        OperationStatus.VerificationFailed => () => _loc["ErrorVerificationFailed"],
        OperationStatus.Cancelled => () => _loc["StatusCancelled"],
        _ => () => _loc["ErrorCopyFailed"],
    };

    private Func<string> DescribeFailedCheck(CheckId check, Soundtrack? soundtrack) => check switch
    {
        CheckId.GameVersion => () => _loc["ErrorGameVersionMissing"],
        CheckId.BgmDirectory => () => _loc["ErrorBgmFolderMissing"],
        CheckId.SoundtrackDirectory => () => _loc["ErrorNoSoundtrackFound"],
        CheckId.SourceFile => () => _loc.Format("ErrorSourceMissing", soundtrack is null ? "" : TitleOf(soundtrack)),
        CheckId.GameNotRunning => () => _loc["ErrorGameRunning"],
        CheckId.OriginalFileSafe => () => _loc["ErrorBackupFailed"],
        CheckId.VanillaBackupAvailable => () => _loc["ErrorNoBackup"],
        _ => () => _loc["ErrorGameFolderInvalid"],
    };

    /// <summary>
    /// Transmet l'avancement au fil de l'interface. Seul ce fil a le droit de
    /// modifier ce qui est affiché, alors que la copie tourne sur un autre.
    /// </summary>
    private sealed class UiProgress(Action<InstallProgress> handler) : IProgress<InstallProgress>
    {
        private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

        public void Report(InstallProgress value)
        {
            if (_uiContext is null)
            {
                handler(value);
            }
            else
            {
                _uiContext.Post(_ => handler(value), null);
            }
        }
    }
}
