using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Settings.Handlers;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Settings;

public class SelectOutputColumnsPrompt : BasePrompt<FileReaderResult, Task>
{        
    public SelectOutputColumnsPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> AcceptInput(FileReaderResult fileReaderResult)
    {
        var columns = AnsiConsole.Prompt(new MultiSelectionPrompt<FileColumn>()
                .Title("Select output columns")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a column, [green]<enter>[/] to accept)[/]")
                .AddChoices(fileReaderResult.FileColumns));

        _consoleWriter.WriteTable(["Index", "Name"], [.. columns.Select(p => new[] { p.Index.ToString(), p.Name })]);

        foreach (var c in fileReaderResult.FileColumns)
        {
            if (columns.Contains(c))
            {
                c.SelectedForOutput = true;
            }
            else
            {
                c.SelectedForOutput = false;
            }
        }

        var storeSettingsCommand = new StoreFileSettingsHandler(_consoleWriter);
        await storeSettingsCommand.Handle(fileReaderResult);

        return Task.CompletedTask;
    }
}
