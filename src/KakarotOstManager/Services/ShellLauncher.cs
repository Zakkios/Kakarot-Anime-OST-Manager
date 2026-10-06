using System.Diagnostics;

namespace KakarotOstManager.Services;

/// <summary>
/// Demande à Windows d'ouvrir une adresse ou un fichier avec le programme
/// qui lui est associé. Interface séparée pour que les tests puissent
/// vérifier ce qui serait lancé sans rien lancer.
/// </summary>
public interface IShellLauncher
{
    void Open(string target);
}

public sealed class SystemShellLauncher : IShellLauncher
{
    public void Open(string target)
    {
        // UseShellExecute confie l'adresse à Windows, comme un double-clic :
        // une adresse « steam:// » est alors transmise à Steam.
        using Process? process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }
}
