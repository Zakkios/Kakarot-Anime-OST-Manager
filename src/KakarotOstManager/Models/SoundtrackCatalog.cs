namespace KakarotOstManager.Models;

/// <summary>Contenu du fichier Data/soundtracks.json.</summary>
public sealed record SoundtrackCatalog
{
    public IReadOnlyList<SoundtrackMetadata> Soundtracks { get; init; } = [];
}

/// <summary>
/// Informations connues à l'avance sur une bande-son du mod. Elles sont
/// rattachées à un dossier du disque grâce à <see cref="Match"/>.
/// </summary>
public sealed record SoundtrackMetadata
{
    public required string Id { get; init; }

    /// <summary>
    /// Expression régulière, insensible à la casse, testée sur le nom du
    /// dossier qui contient Bgm.awb.
    /// </summary>
    public required string Match { get; init; }

    /// <summary>Numéro affiché devant le nom, par exemple « 02.5 ».</summary>
    public string? Number { get; init; }

    public LocalizedText Name { get; init; } = LocalizedText.Empty;

    public SoundtrackCategory Category { get; init; } = SoundtrackCategory.Main;

    /// <summary>Position dans sa rubrique, par ordre croissant.</summary>
    public double Order { get; init; }

    /// <summary>Moment de l'histoire où installer cette bande-son.</summary>
    public LocalizedText ActivationHint { get; init; } = LocalizedText.Empty;
}
