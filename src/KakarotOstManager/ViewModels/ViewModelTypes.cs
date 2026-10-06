using CommunityToolkit.Mvvm.ComponentModel;
using KakarotOstManager.Models;

namespace KakarotOstManager.ViewModels;

/// <summary>Nature du message affiché en bas de la fenêtre ; elle détermine sa couleur.</summary>
public enum StatusKind
{
    None,
    Info,
    Success,
    Error,
}

/// <summary>Une langue proposée dans la liste déroulante.</summary>
public sealed record LanguageOption(string Code, string DisplayName);

/// <summary>Une ligne de la liste des contrôles préalables.</summary>
public sealed record CheckItemViewModel(string Label, bool Passed);

/// <summary>
/// Ouvre la boîte de dialogue « choisir un dossier ». Le ViewModel passe par
/// cette interface pour ne pas dépendre de la fenêtre, et rester testable.
/// </summary>
public interface IFolderPicker
{
    /// <returns>Le dossier choisi, ou <c>null</c> si l'utilisateur a annulé.</returns>
    string? PickFolder(string title, string? initialDirectory);
}

/// <summary>Une ligne de la liste des bandes-son.</summary>
public sealed partial class SoundtrackItemViewModel(Soundtrack soundtrack, string title, string categoryTitle)
    : ObservableObject
{
    public Soundtrack Soundtrack { get; } = soundtrack;

    /// <summary>Texte affiché, par exemple « 03 — Namek ».</summary>
    public string Title { get; } = title;

    /// <summary>Titre de la rubrique, qui sert à regrouper les lignes.</summary>
    public string CategoryTitle { get; } = categoryTitle;

    /// <summary>Vrai si c'est la bande-son actuellement en place dans le jeu.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }
}
