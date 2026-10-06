using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace KakarotOstManager.Services;

/// <summary>Réglages JSON communs à settings.json et soundtracks.json.</summary>
internal static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,

        // Tolérance utile pour un fichier modifié à la main.
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,

        // Écrit « é » tel quel plutôt que « é ».
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),

        // Écrit les enums sous forme de texte (« remaster ») plutôt que de nombre.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
