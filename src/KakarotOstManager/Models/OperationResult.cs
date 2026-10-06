namespace KakarotOstManager.Models;

public enum OperationStatus
{
    Success,

    /// <summary>Un contrôle préalable a échoué : rien n'a été modifié.</summary>
    PrecheckFailed,

    /// <summary>La musique originale n'a pas pu être mise à l'abri : rien n'a été écrasé.</summary>
    BackupFailed,

    /// <summary>Le fichier du jeu n'a pas pu être remplacé : l'ancien est toujours en place.</summary>
    CopyFailed,

    /// <summary>La copie ne correspond pas à l'original : l'ancien fichier est toujours en place.</summary>
    VerificationFailed,

    Cancelled,
}

/// <summary>Compte rendu d'une installation ou d'une restauration.</summary>
/// <param name="Checks">Contrôles effectués, dans l'ordre, réussis ou non.</param>
/// <param name="Error">Erreur technique à l'origine de l'échec, s'il y en a une.</param>
public sealed record OperationResult(
    OperationStatus Status,
    IReadOnlyList<CheckResult> Checks,
    Exception? Error = null)
{
    public bool Succeeded => Status == OperationStatus.Success;
}
