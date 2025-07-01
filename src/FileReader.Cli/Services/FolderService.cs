namespace FileReader.Cli.Services;

public class FolderService
{
    public static string HomePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".x3lfilereader");

    public static string FileSettingsPath => Path.Combine(HomePath, "file-settings");
    public static string PrevReadFilesPath => Path.Combine(HomePath, "prev-read-files");
    public static string ExportsPath => Path.Combine(HomePath, "exports");
}
