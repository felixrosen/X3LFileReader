using System.Buffers;
using System.Text;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.FileReaders.Services;
using FileReader.Cli.Inputs.Prompts.Export.Handlers;
using FileReader.Cli.Inputs.Prompts.Main;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.View;

public record ViewFileQuery
{
    public required int Page { get; set; } = 1;
    public required int PageSize { get; set; } = 50;

    public required FileReaderResult FileReaderResult { get; set; }
}

public class ViewFileResult
{
    public required int Page { get; set; } = 1;
    public required int PageSize { get; set; } = 50;
    public required int TotalPages { get; set; }
    public required int TotalRecords { get; set; }

    public required List<FileColumn> FileColumns { get; set; }
    public required List<List<string>> ResultPage { get; set; }
}

public class ViewFilePrompt : BasePrompt<FileReaderResult, Task>
{
    private readonly SelectionItem _mainSelection;

    public ViewFilePrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new InlineHandler<ViewFileQuery, ViewFileResult>
                {
                    Name = "Next Page",
                    State = SelectionPromptState.NextPage,
                    Handler = GetPage,
                },
                new InlineHandler<ViewFileQuery, ViewFileResult>
                {
                    Name = "Previous Page",
                    State = SelectionPromptState.PreviousPage,
                    Handler = GetPage,
                },
                new ExportPageToFileHandler(consoleWriter)
                {
                    Name = "Export Page",
                    State = SelectionPromptState.ExportPage,
                },
                new InlineHandler<(ViewFileQuery Query, int Page), (ViewFileResult Result, ViewFileQuery Query)>
                {
                    Name = "Go to Page",
                    State = SelectionPromptState.GoToPage,
                    Handler = (c, vq) =>
                    {
                        var query = vq.Query with {Page = vq. Page };
                        return (GetPage(c, query), query);
                    }
                },
                new ViewFileLinesPrompt(consoleWriter)
                {
                    Name = "View Lines",
                    State = SelectionPromptState.ViewLines,
                },
                new SelectionItem
                {
                    Name = "Exit View",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }

    public override async Task<Task> AcceptInput(FileReaderResult input)
    {
        var query = new ViewFileQuery
        {
            Page = 1,
            PageSize = 50,
            FileReaderResult = input,
        };

        var result = GetPage(_consoleWriter, query);

        // Display first page

        SelectionItem ms = new SelectionItem { Name = "Main selection", State = SelectionPromptState.NextPage };

        do
        {
            if (ms is { State: SelectionPromptState.NextPage } or 
                      { State: SelectionPromptState.PreviousPage } or
                      { State: SelectionPromptState.GoToPage })
            {
                _consoleWriter.WriteTable([.. input.SelectedFileColumns.Select(p => p.Name)], result.ResultPage);

                _consoleWriter.WriteInfo($"Page " +
                                         $"{result.Page} ({result.TotalPages:N0}) " +
                                         $"- " +
                                         $"Records " +
                                         $"{(result.Page - 1) * result.PageSize + 1} - " +
                                         $"{result.Page * result.PageSize - (result.PageSize - result.ResultPage.Count)} ({result.TotalRecords:N0})",
                                         taskOutputColor: "yellow", addNewLine: true);
            }

            // Ask for next step
            ms = MainPrompt.ShowPrompt("What would you like to do?", input.FileInfo.FileInfo.Name, _mainSelection.SubItems);

            if (ms is InlineHandler<ViewFileQuery, ViewFileResult> changePageHandler)
            {
                var page = ms.State switch
                {
                    SelectionPromptState.NextPage => query.Page++,
                    SelectionPromptState.PreviousPage => query.Page--,
                    _ => query.Page++,
                };

                result = changePageHandler.Handler(_consoleWriter, query);
            }

            if (ms is InlineHandler<(ViewFileQuery Query, int Page), (ViewFileResult Result, ViewFileQuery Query)> goToPageHandler)
            {
                var selectedPage = AnsiConsole.Prompt(new TextPrompt<int>("Select page: "));
                var commandResult = goToPageHandler.Handler(_consoleWriter, (query, selectedPage));

                result = commandResult.Result;
                query = commandResult.Query;
            }

            if (ms is ViewFileLinesPrompt viewFileLinesItem)
            {
                await viewFileLinesItem.AcceptInput(input);
            }

            if (ms is ExportPageToFileHandler exportPageToFileHandler)
            {
                await exportPageToFileHandler.Handle((result, result.Page, input));
            }

            if (ms.State == SelectionPromptState.Exit)
                break;

        } while (true);

        return Task.FromResult(Task.CompletedTask);
    }

    public static ViewFileResult GetPage(IConsoleWriter consoleWriter, ViewFileQuery input)
    {
        if (input.Page < 1)
            input.Page = 1;

        var rawLines = input.FileReaderResult.FileLines.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize).ToList();
        var selectedColumnIndexesArray = input.FileReaderResult.FileColumns.Where(q => q.SelectedForOutput).Select(p => p.Index).ToArray();
        var selectedColumnIndexes = new ReadOnlySpan<int>(selectedColumnIndexesArray);

        var rows = new List<List<string>>();

        foreach (var l in rawLines)
        {
            if (l.RawLine is null)
            { continue; }

            if (l.RawLine.Length == 0)
            { continue; }

            var columns = ParseColumns(l.RawLine, input.FileReaderResult.FileColumns.Count, input.FileReaderResult.Delimiter);

            var row = new List<string>();

            for (var i = 0; i < columns.Length; i++)
            {
                if (!selectedColumnIndexes.Contains(i))
                    continue;

                row.Add(Encoding.UTF8.GetString(columns[i]));
            }

            rows.Add(row);
        }

        return new ViewFileResult
        {
            FileColumns = input.FileReaderResult.SelectedFileColumns,
            Page = input.Page,
            PageSize = input.PageSize,
            ResultPage = rows,
            TotalPages = (int)Math.Ceiling((double)input.FileReaderResult.FileLines.Length / input.PageSize),
            TotalRecords = input.FileReaderResult.FileLines.Length,
        };
    }

    private static byte[][] ParseColumns(byte[] rawLine, int numberOfColumns, byte delimiter)
    {
        var readOnlySequence = new ReadOnlySequence<byte>(rawLine);
        var reader = new SequenceReader<byte>(readOnlySequence);
        var parts = new byte[numberOfColumns][];

        while (!reader.End)
        {
            for (var i = 0; i < numberOfColumns; i++)
            {
                var linePart = reader.Read(delimiter);

                if (linePart.Length == 0)
                {
                    parts[i] = [];
                    continue;
                }

                parts[i] = linePart;
            }

            if (!reader.End) // If we reached the end of the line, we can break.                
                break;
        }

        return parts;
    }
}
