using System.IO;

namespace KakarotOstManager.Services;

/// <summary>Une copie ne contient pas exactement les mêmes octets que son original.</summary>
public sealed class FileVerificationException(string sourceFile, string copiedFile)
    : IOException($"La copie « {copiedFile} » ne correspond pas à « {sourceFile} ».")
{
    public string SourceFile { get; } = sourceFile;

    public string CopiedFile { get; } = copiedFile;
}
