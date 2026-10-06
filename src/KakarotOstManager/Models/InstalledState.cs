namespace KakarotOstManager.Models;

public enum InstalledKind
{
    /// <summary>Il n'y a pas de Bgm.awb dans le jeu, ou le jeu n'est pas configuré.</summary>
    NoGameFile,

    /// <summary>Le fichier en place est identique à la sauvegarde de la musique originale.</summary>
    Vanilla,

    /// <summary>Le fichier en place est identique à une bande-son du pack.</summary>
    Soundtrack,

    /// <summary>
    /// Le fichier en place n'est ni la sauvegarde ni une bande-son du pack.
    /// Tant qu'aucune sauvegarde n'existe, c'est le cas de la musique originale.
    /// </summary>
    Unknown,
}

/// <summary>Ce que contient réellement le dossier BGM du jeu.</summary>
public sealed record InstalledState(InstalledKind Kind, Soundtrack? Soundtrack = null)
{
    public static InstalledState NoGameFile { get; } = new(InstalledKind.NoGameFile);

    public static InstalledState Vanilla { get; } = new(InstalledKind.Vanilla);

    public static InstalledState Unknown { get; } = new(InstalledKind.Unknown);

    public static InstalledState Of(Soundtrack soundtrack) => new(InstalledKind.Soundtrack, soundtrack);
}
