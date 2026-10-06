namespace KakarotOstManager.Tests;

/// <summary>
/// Dossier temporaire propre à un test, supprimé à la fin. Les tests ne
/// touchent ainsi jamais aux vrais fichiers du jeu ou du pack.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("kaom-tests-").FullName;

    /// <summary>Chemin absolu d'un élément du dossier temporaire.</summary>
    public string Combine(params string[] parts) => Path.Combine([Root, .. parts]);

    public string CreateDirectory(params string[] parts)
    {
        string path = Combine(parts);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Crée un fichier, ainsi que les dossiers intermédiaires manquants.</summary>
    public string CreateFile(string content, params string[] parts)
    {
        string path = Combine(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Un fichier encore verrouillé ne doit pas faire échouer le test.
        }
    }
}
