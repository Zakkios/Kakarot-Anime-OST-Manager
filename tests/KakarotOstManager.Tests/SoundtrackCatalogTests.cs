using System.Text.Json;
using System.Text.RegularExpressions;
using KakarotOstManager.Models;
using KakarotOstManager.Services;

namespace KakarotOstManager.Tests;

/// <summary>
/// Garde-fous sur le soundtracks.json livré : une faute de frappe dans ce
/// fichier doit faire échouer un test, pas l'application chez l'utilisateur.
/// </summary>
public class SoundtrackCatalogTests
{
    private static SoundtrackCatalog LoadShippedCatalog() =>
        SoundtrackCatalogLoader.Load(SoundtrackCatalogLoader.DefaultPath);

    [Fact]
    public void Le_catalogue_livre_contient_les_neuf_chapitres_de_l_histoire()
    {
        SoundtrackCatalog catalog = LoadShippedCatalog();

        Assert.Equal(9, catalog.Soundtracks.Count(metadata => metadata.Category == SoundtrackCategory.Main));
    }

    [Fact]
    public void Les_identifiants_sont_uniques()
    {
        var ids = LoadShippedCatalog().Soundtracks.Select(metadata => metadata.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Chaque_motif_est_une_expression_reguliere_valide()
    {
        foreach (SoundtrackMetadata metadata in LoadShippedCatalog().Soundtracks)
        {
            // Lève une exception, donc fait échouer le test, si le motif est invalide.
            _ = new Regex(metadata.Match);
        }
    }

    [Fact]
    public void Chaque_bande_son_a_un_nom_dans_les_deux_langues()
    {
        Assert.All(LoadShippedCatalog().Soundtracks, metadata =>
        {
            Assert.False(string.IsNullOrWhiteSpace(metadata.Name.Fr));
            Assert.False(string.IsNullOrWhiteSpace(metadata.Name.En));
        });
    }

    [Fact]
    public void Un_fichier_ecrit_a_la_main_avec_commentaires_est_accepte()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile(
            """
            {
              // un commentaire
              "soundtracks": [
                {
                  "id": "17-nouveau-dlc",
                  "match": "17\\s*-",
                  "name": { "fr": "Nouveau DLC", "en": "New DLC" },
                  "category": "dlc",
                  "order": 170,
                  "activationHint": { "fr": "Après la fin de l'histoire." },
                },
              ],
            }
            """,
            "soundtracks.json");

        SoundtrackMetadata dlc = Assert.Single(SoundtrackCatalogLoader.Load(file).Soundtracks);

        Assert.Equal(SoundtrackCategory.Dlc, dlc.Category);
        Assert.Equal(170, dlc.Order);
        Assert.Equal("Après la fin de l'histoire.", dlc.ActivationHint.Get("en"));
    }

    [Fact]
    public void Une_entree_sans_identifiant_est_refusee()
    {
        using var temp = new TempDirectory();
        string file = temp.CreateFile("""{ "soundtracks": [ { "match": "03" } ] }""", "soundtracks.json");

        Assert.Throws<JsonException>(() => SoundtrackCatalogLoader.Load(file));
    }
}
