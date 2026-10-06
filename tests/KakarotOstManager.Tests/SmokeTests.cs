namespace KakarotOstManager.Tests;

/// <summary>
/// Test « fumigène » du jalon 0 : il ne vérifie aucune règle métier, seulement
/// que le projet de tests voit bien le projet de l'application.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void Le_projet_de_tests_reference_l_application()
    {
        var assembly = typeof(App).Assembly;

        Assert.Equal("KakarotOstManager", assembly.GetName().Name);
        Assert.Equal(new Version(0, 1, 0, 0), assembly.GetName().Version);
    }
}
