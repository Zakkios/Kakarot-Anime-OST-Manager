namespace KakarotOstManager.Models;

/// <summary>
/// Rubrique dans laquelle une bande-son est affichée. L'ordre des valeurs
/// est aussi l'ordre d'affichage des rubriques.
/// </summary>
public enum SoundtrackCategory
{
    Main,
    Dlc,

    /// <summary>Dossier contenant un Bgm.awb mais absent de soundtracks.json.</summary>
    Other,
}
