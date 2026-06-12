namespace CodeEnforcer;

internal static class JustificationEntryTypes
{
    public static JustificationEntryType Parse(string value)
    {
        string normalized = value.Trim().Replace("_", "-", StringComparison.Ordinal).ToLowerInvariant();
        return normalized switch
        {
            "file" or "files" => JustificationEntryType.File,
            "folder" or "folders" => JustificationEntryType.Folder,
            "root-folder" or "root-folders" or "root" or "rootfolder" or "rootfolders" =>
                JustificationEntryType.RootFolder,
            _ => throw new CodeEnforcerException(
                "--type must be one of: file, folder, root-folder.",
                ExitCodes.InputError)
        };
    }

    public static string Format(JustificationEntryType type) =>
        type switch
        {
            JustificationEntryType.File => "file",
            JustificationEntryType.Folder => "folder",
            JustificationEntryType.RootFolder => "root-folder",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}
