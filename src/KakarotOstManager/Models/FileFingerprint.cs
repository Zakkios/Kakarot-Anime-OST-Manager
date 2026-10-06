namespace KakarotOstManager.Models;

/// <summary>
/// Empreinte d'un fichier, accompagnée de sa taille et de sa date de
/// modification au moment du calcul. Tant que ces deux dernières n'ont pas
/// changé, l'empreinte est réutilisée sans relire le fichier.
/// </summary>
public sealed record FileFingerprint(long Length, DateTime LastWriteTimeUtc, string Sha256);
