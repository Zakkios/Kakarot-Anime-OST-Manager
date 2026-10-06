using System.IO;

namespace KakarotOstManager.Models;

/// <summary>
/// Bande-son réellement trouvée sur le disque : un dossier du pack qui
/// contient un Bgm.awb, enrichi des métadonnées quand elles existent.
/// </summary>
public sealed record Soundtrack
{
    public required string Id { get; init; }

    public required string FolderPath { get; init; }

    public required string BgmFilePath { get; init; }

    public string? Number { get; init; }

    public required LocalizedText Name { get; init; }

    public required SoundtrackCategory Category { get; init; }

    public double Order { get; init; }

    public LocalizedText ActivationHint { get; init; } = LocalizedText.Empty;

    public string FolderName => Path.GetFileName(FolderPath);
}
