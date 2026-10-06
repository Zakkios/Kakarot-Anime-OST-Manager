using System.IO;
using System.Text.Json;
using KakarotOstManager.Models;

namespace KakarotOstManager.Services;

/// <summary>Lit et écrit les paramètres de l'application.</summary>
public sealed class SettingsService(AppPaths paths)
{
    /// <summary>
    /// Renvoie les paramètres enregistrés, ou des paramètres vides si le
    /// fichier n'existe pas encore ou est illisible.
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(paths.SettingsFile))
        {
            return new AppSettings();
        }

        try
        {
            string json = File.ReadAllText(paths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonDefaults.Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Un fichier de paramètres abîmé ne doit pas empêcher l'application
            // de démarrer : on repart de zéro, les chemins seront redemandés.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings) =>
        AtomicFile.WriteAllText(paths.SettingsFile, JsonSerializer.Serialize(settings, JsonDefaults.Options));
}
