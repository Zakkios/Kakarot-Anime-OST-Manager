using System.IO;
using System.Text.Json;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Charge les métadonnées des bandes-son.</summary>
public static class SoundtrackCatalogLoader
{
    // Nom donné à Data/soundtracks.json dans le .csproj (LogicalName).
    private const string EmbeddedResourceName = "KakarotOstManager.Data.soundtracks.json";

    /// <summary>
    /// Fichier facultatif, à côté de l'exécutable. S'il existe, il remplace le
    /// catalogue intégré : on peut ainsi corriger ou compléter les
    /// métadonnées sans recompiler l'application.
    /// </summary>
    public static string OverridePath => Path.Combine(AppContext.BaseDirectory, "Data", "soundtracks.json");

    /// <summary>Catalogue à utiliser au démarrage de l'application.</summary>
    public static SoundtrackCatalog LoadDefault() => LoadWithOverride(OverridePath);

    /// <summary>Catalogue de <paramref name="overrideFile"/> s'il existe, sinon celui intégré à l'application.</summary>
    /// <exception cref="JsonException">Le fichier de remplacement n'est pas un catalogue valide.</exception>
    public static SoundtrackCatalog LoadWithOverride(string overrideFile) =>
        File.Exists(overrideFile) ? Load(overrideFile) : LoadEmbedded();

    /// <summary>Catalogue livré à l'intérieur de l'exécutable.</summary>
    public static SoundtrackCatalog LoadEmbedded()
    {
        using Stream stream = typeof(SoundtrackCatalogLoader).Assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Ressource intégrée introuvable : {EmbeddedResourceName}");

        return JsonSerializer.Deserialize<SoundtrackCatalog>(stream, JsonDefaults.Options) ?? new SoundtrackCatalog();
    }

    /// <exception cref="JsonException">Le fichier n'est pas un JSON valide ou il manque un champ obligatoire.</exception>
    public static SoundtrackCatalog Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SoundtrackCatalog>(json, JsonDefaults.Options) ?? new SoundtrackCatalog();
    }
}
