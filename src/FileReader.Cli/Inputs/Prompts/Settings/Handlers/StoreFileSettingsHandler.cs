using System.Diagnostics;
using System.Text.Json;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Types;
using FileReader.Cli.Services;

namespace FileReader.Cli.Inputs.Prompts.Settings.Handlers;

public class StoreFileSettingsHandler : BaseHandler<FileReaderResult, Task>
{
    private static JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true, };

    public StoreFileSettingsHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
    }

    public override async Task<Task> Handle(FileReaderResult fileReaderResult)
    {
        var sw = Stopwatch.StartNew();

        if (fileReaderResult.FileHash is null or { Length: < 1 })
        {
            _consoleWriter.WriteError("Could not store settings, missing file hash");
            return Task.CompletedTask;
        }

        var fileSettingsPath = FolderService.FileSettingsPath;

        Directory.CreateDirectory(fileSettingsPath); // safe even if it exists

        var filePath = Path.Combine(fileSettingsPath, $"{fileReaderResult.FileHash}.json");

        _consoleWriter.WriteInfo(" Path to settings:", filePath, taskOutputColor: "yellow", icon: ":information:");

        // Check if file exists
        var settings = new StoredFileSettings
        {
            FileName = fileReaderResult.FileInfo.FileInfo.Name,
            FileColumns = fileReaderResult.FileColumns
        };
        
        var fileData = JsonSerializer.Serialize(settings, options: _options);

        await File.WriteAllTextAsync(filePath, fileData);

        sw.Stop();

        _consoleWriter.WriteInfo("Settings stored", icon: ":check_mark_button:", elapsed: sw.Elapsed, addNewLine: true);

        return Task.CompletedTask;
    }
}
