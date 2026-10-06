using System.IO;
using KakarotOstManager.ViewModels;
using Microsoft.Win32;

namespace KakarotOstManager.Views;

/// <summary>Ouvre la boîte de dialogue Windows de sélection de dossier.</summary>
public sealed class FolderPicker : IFolderPicker
{
    public string? PickFolder(string title, string? initialDirectory)
    {
        var dialog = new OpenFolderDialog { Title = title };
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
