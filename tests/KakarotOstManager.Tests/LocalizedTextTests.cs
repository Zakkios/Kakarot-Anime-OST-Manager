using KakarotOstManager.Models;

namespace KakarotOstManager.Tests;

public class LocalizedTextTests
{
    [Theory]
    [InlineData("fr", "Juste avant le départ")]
    [InlineData("fr-FR", "Juste avant le départ")]
    [InlineData("FR", "Juste avant le départ")]
    [InlineData("en", "Right before leaving")]
    [InlineData("en-US", "Right before leaving")]
    [InlineData("de", "Right before leaving")]
    public void La_langue_demandee_est_renvoyee_l_anglais_servant_de_langue_par_defaut(string language, string expected)
    {
        var text = new LocalizedText(Fr: "Juste avant le départ", En: "Right before leaving");

        Assert.Equal(expected, text.Get(language));
    }

    [Fact]
    public void L_autre_langue_sert_de_repli_quand_la_traduction_manque()
    {
        Assert.Equal("Right before leaving", new LocalizedText(Fr: "", En: "Right before leaving").Get("fr"));
        Assert.Equal("Juste avant le départ", new LocalizedText(Fr: "Juste avant le départ").Get("en"));
    }

    [Fact]
    public void Un_texte_vide_donne_une_chaine_vide()
    {
        Assert.Equal("", LocalizedText.Empty.Get("fr"));
    }
}
