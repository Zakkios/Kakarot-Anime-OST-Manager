using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;

namespace KakarotOstManager.Localization;

/// <summary>
/// Donne accès aux textes de l'interface dans la langue courante, et prévient
/// l'interface quand cette langue change pour qu'elle se redessine.
/// </summary>
public sealed class Localizer : INotifyPropertyChanged
{
    // Strings.resx (anglais) est compilé dans l'application ; Strings.fr.resx
    // devient un fichier séparé, fr\KakarotOstManager.resources.dll.
    private static readonly ResourceManager Resources =
        new("KakarotOstManager.Localization.Strings", typeof(Localizer).Assembly);

    private CultureInfo _culture = ToSupportedCulture(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    /// <summary>Instance utilisée par l'interface (voir <see cref="LocExtension"/>).</summary>
    public static Localizer Instance { get; } = new();

    public static IReadOnlyList<string> SupportedLanguages { get; } = ["en", "fr"];

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Code de la langue courante : « fr » ou « en ».</summary>
    public string Language
    {
        get => _culture.TwoLetterISOLanguageName;
        set
        {
            CultureInfo culture = ToSupportedCulture(value);
            if (culture.Equals(_culture))
            {
                return;
            }

            _culture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));

            // « Item[] » désigne l'indexeur : toutes les liaisons du type
            // {loc:Loc Cle} se rafraîchissent.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        }
    }

    /// <summary>Texte associé à <paramref name="key"/> dans la langue courante.</summary>
    public string this[string key] => Resources.GetString(key, _culture) ?? $"[{key}]";

    /// <summary>Texte contenant des emplacements {0}, {1}… remplis par <paramref name="args"/>.</summary>
    public string Format(string key, params object?[] args) => string.Format(_culture, this[key], args);

    private static CultureInfo ToSupportedCulture(string? languageCode) =>
        CultureInfo.GetCultureInfo(
            languageCode is not null && languageCode.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "fr" : "en");
}
