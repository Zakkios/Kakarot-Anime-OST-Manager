using System.Diagnostics;

namespace KakarotOstManager.Services;

/// <summary>
/// Indique si un programme est en cours d'exécution. Cette interface existe
/// pour que les tests puissent simuler un jeu lancé sans le lancer.
/// </summary>
public interface IProcessProbe
{
    /// <param name="processName">Nom du processus, sans « .exe ».</param>
    bool IsRunning(string processName);
}

/// <summary>Interroge la liste réelle des processus de Windows.</summary>
public sealed class SystemProcessProbe : IProcessProbe
{
    public bool IsRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }
}
