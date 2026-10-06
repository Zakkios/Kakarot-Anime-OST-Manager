namespace KakarotOstManager.Models;

/// <summary>
/// Texte disponible en français et en anglais. Si la langue demandée est
/// vide, l'autre langue sert de repli.
/// </summary>
public sealed record LocalizedText(string? Fr = null, string? En = null)
{
    public static LocalizedText Empty { get; } = new();

    /// <summary>Même texte dans les deux langues (un nom de dossier, par exemple).</summary>
    public static LocalizedText Same(string text) => new(text, text);

    /// <param name="languageCode">Code de langue tel que « fr », « fr-FR » ou « en ».</param>
    public string Get(string languageCode)
    {
        bool french = languageCode.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
        string? preferred = french ? Fr : En;
        string? fallback = french ? En : Fr;

        return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback ?? "";
    }
}
