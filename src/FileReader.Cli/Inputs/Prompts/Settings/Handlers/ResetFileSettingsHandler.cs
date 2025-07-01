using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Types;
using FileReader.Cli.Services;

namespace FileReader.Cli.Inputs.Prompts.Settings.Handlers;

public class ResetFileSettingsHandler : BaseHandler<FileReaderResult, Task>
{
    public ResetFileSettingsHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
    }

    public override Task<Task> Handle(FileReaderResult input)
    {
        var fileSettingsPath = FolderService.FileSettingsPath;
        var filePath = Path.Combine(fileSettingsPath, $"{input.FileHash}.json");

        if(File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        input.ResetFileColumns();

        _consoleWriter.WriteInfo(" Reset file settings completed ", input.FileInfo.FileInfo.Name, icon: ":information:", addNewLine: true);

        return Task.FromResult(Task.CompletedTask);
    }
}
