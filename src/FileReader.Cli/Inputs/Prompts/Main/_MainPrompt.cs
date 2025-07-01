using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Main;

public class MainPrompt
{
    private readonly SelectionItem _mainSelection;
    
    private readonly IConsoleWriter _consoleWriter;

    public MainPrompt(IConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter;

        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new SelectFilePrompt(_consoleWriter)
                {
                    Name = "Select File",
                    State = SelectionPromptState.NextPage,
                },
                new SelectionItem
                {
                    Name = "Exit Select File",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }

    public async Task<Task> AcceptInput(Task _)
    {
        _consoleWriter.WriteFiglet("X3L File Reader");

        var ms = ShowPrompt($"What would you like to do?", string.Empty, _mainSelection.SubItems);
        
        while(ms.State != SelectionPromptState.Exit)
        {
            if(ms is SelectFilePrompt selectFilePrompt)
            {
                var selectedFileResult = await selectFilePrompt.AcceptInput(Task.CompletedTask);

                if(selectedFileResult.Success is true)                                    
                    _ = await new FileReaderPrompt(_consoleWriter).AcceptInput(selectedFileResult);
            }

            if (ms.State == SelectionPromptState.Exit)
                break;

            ms = ShowPrompt("What would you like to do?", string.Empty, _mainSelection.SubItems);
        }        

        return Task.CompletedTask;
    }

    public static SelectionItem ShowPrompt(string title, string info, List<SelectionItem> items)
    {
        var combinedTitle = title;

        if (info is { Length: > 0 })
            combinedTitle += $" [dim]({info})[/]";

        var selection = AnsiConsole.Prompt(new SelectionPrompt<SelectionItem>()
                                   .Title(combinedTitle)
                                   .AddChoices(items));

        return selection;
    }  
}
