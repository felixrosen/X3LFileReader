using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.Inputs.Types;
using FileReader.Cli.Services;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Main;

public class ReadExistingFilePrompt : BasePrompt<Task, Task<SelectFileResult>>
{
    public ReadExistingFilePrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<SelectFileResult> AcceptInput(Task input)
    {
        await Task.Delay(0);

        var prevReadFilesPath = FolderService.PrevReadFilesPath;

        if (Directory.Exists(prevReadFilesPath) is false)
        {
            _consoleWriter.WriteInfo(" No existing files found", prevReadFilesPath, icon: ":information:", addNewLine: true);
            return new SelectFileResult { Success = false, Message = "No existing files found" };
        }

        var files = Directory.GetFiles(prevReadFilesPath).Select(p => new FileInfo(p)).ToList();

        if (files.Count == 0)
        {
            _consoleWriter.WriteInfo(" No existing files found", prevReadFilesPath, icon: ":information:", addNewLine: true);
            return new SelectFileResult { Success = false, Message = "No existing files found" };
        }

        var metaData = new List<SelectFileResult>();
        foreach (var f in files)
        {
            var content = await File.ReadAllTextAsync(f.FullName);
            var fr = System.Text.Json.JsonSerializer.Deserialize<SelectFileResult>(content);

            if (fr is null)
                continue;

            metaData.Add(fr);
        }

        var selectedFile = AnsiConsole.Prompt(new SelectionPrompt<SelectFileResult>()
                                      .Title("Select file to read")
                                      .AddChoices(metaData)
                                      .UseConverter((f) => new FileInfo(f.FullName!).Name));

        selectedFile.IsNewFile = false;

        return selectedFile;
    }
}
