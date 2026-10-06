namespace KakarotOstManager.Models;

public enum InstallStep
{
    /// <summary>Identification du fichier actuellement en place dans le jeu.</summary>
    Analyzing,

    /// <summary>Mise à l'abri du fichier en place avant de l'écraser.</summary>
    BackingUp,

    Copying,

    Verifying,

    Done,
}

/// <summary>Avancement d'une opération : l'étape en cours et sa part accomplie, de 0 à 1.</summary>
public readonly record struct InstallProgress(InstallStep Step, double Fraction);
