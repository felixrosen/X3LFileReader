using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Settings.Handlers;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Settings;

public class SelectOrderColumnPrompt : BasePrompt<FileReaderResult, Task>
{    
    public SelectOrderColumnPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> AcceptInput(FileReaderResult fileReaderResult)
    {
        var column = AnsiConsole.Prompt(new SelectionPrompt<FileColumn>()
                .Title("Select column to order by")
                .AddChoices(fileReaderResult.SelectedFileColumns));

        _consoleWriter.WriteInfo($"Column to order by:", column.Name, taskOutputColor: "yellow", addNewLine: true);

        foreach (var c in fileReaderResult.FileColumns)
        {
            if (column.Index == c.Index)
            {
                c.OrderBy = true;
            }
            else
            {
                c.OrderBy = false;
            }
        }

        var storeSettingsCommand = new StoreFileSettingsHandler(_consoleWriter);
        await storeSettingsCommand.Handle(fileReaderResult);

        return Task.CompletedTask;
    }
}
