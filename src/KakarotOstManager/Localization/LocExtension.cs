using System.Windows.Data;
using System.Windows.Markup;

namespace KakarotOstManager.Localization;

/// <summary>
/// Permet d'écrire <c>Text="{loc:Loc Apply}"</c> dans le XAML. Le texte est
/// lié au <see cref="Localizer"/> : il change tout seul avec la langue.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
