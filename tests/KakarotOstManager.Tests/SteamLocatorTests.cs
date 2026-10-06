using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

public class SteamLocatorTests
{
    /// <summary>Extrait fidèle d'un vrai fichier libraryfolders.vdf à deux bibliothèques.</summary>
    private const string LibraryFoldersVdf =
        """
        "libraryfolders"
        {
        	"0"
        	{
        		"path"		"C:\\Program Files (x86)\\Steam"
        		"label"		""
        		"apps"
        		{
        			"228980"		"1026731424"
        		}
        	}
        	"1"
        	{
        		"path"		"D:\\SteamLibrary"
        		"label"		""
        		"apps"
        		{
        			"851850"		"38654705664"
        		}
        	}
        }
        """;

    [Fact]
    public void Les_bibliotheques_sont_lues_dans_le_fichier_de_Steam()
    {
        IReadOnlyList<string> folders = SteamLocator.ParseLibraryFolders(LibraryFoldersVdf);

        Assert.Equal([@"C:\Program Files (x86)\Steam", @"D:\SteamLibrary"], folders);
    }

    [Fact]
    public void Un_fichier_sans_bibliotheque_donne_une_liste_vide()
    {
        Assert.Empty(SteamLocator.ParseLibraryFolders("\"libraryfolders\"\n{\n}"));
    }

    [Fact]
    public void Le_jeu_est_trouve_dans_la_bibliotheque_qui_le_contient()
    {
        using var temp = new TempDirectory();
        string emptyLibrary = temp.CreateDirectory("lib1");
        string libraryWithGame = temp.CreateDirectory("lib2");
        string game = temp.CreateDirectory("lib2", "steamapps", "common", "DRAGON BALL Z KAKAROT");
        temp.CreateDirectory("lib2", "steamapps", "common", "DRAGON BALL Z KAKAROT", "AT", "Content", "Sound", "Bgm");

        string? found = new SteamLocator(new GameService()).FindGameDirectory([emptyLibrary, libraryWithGame]);

        Assert.Equal(game, found);
    }

    [Fact]
    public void Un_dossier_du_jeu_sans_dossier_BGM_est_ignore()
    {
        using var temp = new TempDirectory();
        string library = temp.CreateDirectory("lib");
        temp.CreateDirectory("lib", "steamapps", "common", "DRAGON BALL Z KAKAROT");

        Assert.Null(new SteamLocator(new GameService()).FindGameDirectory([library]));
    }

    [Fact]
    public void Le_dossier_de_Steam_fait_lui_meme_partie_des_bibliotheques()
    {
        using var temp = new TempDirectory();
        string steam = temp.CreateDirectory("Steam");
        temp.CreateFile("\"libraryfolders\"\n{\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\\\SteamLibrary\"\n\t}\n}", "Steam", "steamapps", "libraryfolders.vdf");

        IReadOnlyList<string> folders = SteamLocator.GetLibraryFolders(steam);

        Assert.Equal([steam, @"D:\SteamLibrary"], folders);
    }
}
