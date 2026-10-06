using System.Windows.Controls;
using System.Windows.Markup;
using KakarotOstManager.Localization;

namespace KakarotOstManager.Tests;

public class LocExtensionTests
{
    [Fact]
    public void Un_texte_declare_en_XAML_change_tout_seul_avec_la_langue()
    {
        const string xaml =
            """
            <TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                       xmlns:loc="clr-namespace:KakarotOstManager.Localization;assembly=KakarotOstManager"
                       Text="{loc:Loc Apply}" />
            """;

        RunOnStaThread(() =>
        {
            Localizer.Instance.Language = "en";
            var textBlock = (TextBlock)XamlReader.Parse(xaml);
            Assert.Equal("Apply", textBlock.Text);

            Localizer.Instance.Language = "fr";
            Assert.Equal("Appliquer", textBlock.Text);

            Localizer.Instance.Language = "en";
            Assert.Equal("Apply", textBlock.Text);
        });
    }

    /// <summary>
    /// Les contrôles WPF exigent un fil d'exécution de type « STA », ce que
    /// les fils utilisés par xUnit ne sont pas : on en crée un pour l'occasion.
    /// </summary>
    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new Xunit.Sdk.XunitException($"Échec sur le fil STA : {failure}");
        }
    }
}
