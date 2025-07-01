using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Main;
using FileReader.Cli.Inputs.Prompts.Main.Handlers;
using FileReader.Cli.Inputs.Prompts.Settings.Handlers;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Settings;

public class FileSettingsPrompt : BasePrompt<FileReaderResult, Task>
{    
    public FileSettingsPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> AcceptInput(FileReaderResult fileReader)
    {
        SelectionItem _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [                
                new SelectOutputColumnsPrompt(_consoleWriter)
                {
                    Name = "Select output columns",
                    State = SelectionPromptState.SelectOutputColumns,
                },
                new SelectOrderColumnPrompt(_consoleWriter)
                {
                    Name = "Select column to order by",
                    State = SelectionPromptState.SelectColumnToOrderBy,
                },
                new WriteColumnsHandler(_consoleWriter)
                {
                    Name = "Print selected columns",
                    State = SelectionPromptState.PrintSelectedOutputColumns,
                },
                new ResetFileSettingsHandler(_consoleWriter)
                {
                    Name = "Reset file settings",
                    State = SelectionPromptState.ResetFileSettings,
                },
                new SelectionItem
                {
                    Name = "Exit Settings",
                    State = SelectionPromptState.Exit,
                }
            ]
        };

        while (true)
        {
            var ms = MainPrompt.ShowPrompt("What would you like to do?", fileReader.FileInfo.FileInfo.Name, _mainSelection.SubItems);


            if (ms.State == SelectionPromptState.Exit)
                break;

            if (ms is WriteColumnsHandler writeColumnsHandler)
            {
                await writeColumnsHandler.Handle(fileReader);
            }

            if (ms is ResetFileSettingsHandler resetSettingsHandler)
            {
                await resetSettingsHandler.Handle(fileReader);
            }

            if (ms is SelectOutputColumnsPrompt selectOutputColumn)
            {
                await selectOutputColumn.AcceptInput(fileReader);
            }

            if (ms is SelectOrderColumnPrompt selectOrderColumn)
            {
                await selectOrderColumn.AcceptInput(fileReader);
            }
        }

        return Task.CompletedTask;
    }
}
