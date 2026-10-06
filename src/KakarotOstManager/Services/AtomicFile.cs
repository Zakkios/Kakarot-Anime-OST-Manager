using System.IO;

namespace KakarotOstManager.Services;

internal static class AtomicFile
{
    /// <summary>
    /// Écrit dans un fichier provisoire puis le renomme : si l'écriture est
    /// interrompue, l'ancien fichier reste intact.
    /// </summary>
    public static void WriteAllText(string filePath, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        string tempFile = filePath + ".tmp";
        File.WriteAllText(tempFile, contents);
        File.Move(tempFile, filePath, overwrite: true);
    }
}
