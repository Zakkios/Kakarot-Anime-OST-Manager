using System.IO;
using System.Text.Json;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Charge les métadonnées des bandes-son depuis un fichier JSON.</summary>
public static class SoundtrackCatalogLoader
{
    /// <summary>Le soundtracks.json livré à côté de l'exécutable.</summary>
    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "Data", "soundtracks.json");

    /// <exception cref="JsonException">Le fichier n'est pas un JSON valide ou il manque un champ obligatoire.</exception>
    public static SoundtrackCatalog Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<SoundtrackCatalog>(json, JsonDefaults.Options) ?? new SoundtrackCatalog();
    }
}
