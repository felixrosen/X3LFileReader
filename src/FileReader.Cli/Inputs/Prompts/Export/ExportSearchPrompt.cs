using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Export.Handlers;
using FileReader.Cli.Inputs.Prompts.Main;
using FileReader.Cli.Inputs.Prompts.Search.Handlers;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Export;

public class ExportSearchPrompt : BasePrompt<(SearchResult SearchResult, FileReaderResult FileReaderResult), Task>
{
    SelectionItem _mainSelection;

    public ExportSearchPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new ExportPageSearchResultToFileHandler(_consoleWriter)
                {
                    Name = "Export page",
                    State = SelectionPromptState.ExportPage,
                },
                new ExportSearchResultToFileHandler(_consoleWriter)
                {
                    Name = "Export search result",
                    State = SelectionPromptState.ExportSearchResult,
                },
                new SelectionItem
                {
                    Name = "Exit",
                    State = SelectionPromptState.Exit,
                },
            ]
        };
    }

    public override async Task<Task> AcceptInput((SearchResult SearchResult, FileReaderResult FileReaderResult) input)
    {       
        while (true)
        {
            var ms = MainPrompt.ShowPrompt("What would you like to do?", string.Empty, _mainSelection.SubItems);

            if (ms.State == SelectionPromptState.Exit)
                break;

            if (ms is ExportPageSearchResultToFileHandler exportPageHandler)
            {
                await exportPageHandler.Handle(input);
            }

            if (ms is ExportSearchResultToFileHandler exportHandler)
            {
                await exportHandler.Handle(input);
            }
        }

        return Task.FromResult(true);
    }
}
