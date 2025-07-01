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

public class ViewFileLinesPrompt : BasePrompt<FileReaderResult, Task>
{
    private readonly SelectionItem _mainSelection;

    public ViewFileLinesPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _mainSelection = new()
        {
            Name = "Main selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new SelectionItem
                {
                    Name = "View Lines",
                    State = SelectionPromptState.ViewLines,
                },
                new ExportLinesToFileHandler(_consoleWriter)
                {
                    Name = "Export",
                    State = SelectionPromptState.Export,
                },
                new SelectionItem
                {
                    Name = "Exit View Lines",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }

    public override async Task<Task> AcceptInput(FileReaderResult input)
    {
        var ms = new SelectionItem { Name = "Main selection", State = SelectionPromptState.ViewLines, };

        while (true)
        {
            ViewFileResult? result = null;

            if (ms.State == SelectionPromptState.ViewLines)
            {
                _consoleWriter.WriteInfo(string.Empty);
                var selectedPageInput = AnsiConsole.Prompt(new TextPrompt<string>("Select lines (ex. 1, 2, 3, 5, 8 - 13): "));
                var selectedPages = ParseSelectedPages(selectedPageInput);

                if (selectedPages.Count > 0)
                {
                    result = GetLines(selectedPages, input);

                    _consoleWriter.WriteTable([.. input.SelectedFileColumns.Select(p => p.Name)], result.ResultPage);

                    _consoleWriter.WriteInfo($"Records {result.TotalRecords:N0}", addNewLine: true);
                }
                else
                {
                    _consoleWriter.WriteInfo(" Could not parse lines input", icon: ":information:", addNewLine: true);
                }
            }

            ms = MainPrompt.ShowPrompt("What would you like to do?", string.Empty, _mainSelection.SubItems);

            if (ms is ExportLinesToFileHandler exportLinesHandler)
            {
                if (result is null)
                {
                    _consoleWriter.WriteInfo(" No lines to export", icon: ":information:", addNewLine: true);
                }
                else
                    await exportLinesHandler.Handle((result, input));
            }

            if (ms.State == SelectionPromptState.Exit)
                break;
        }

        return Task.FromResult(Task.CompletedTask);
    }

    private List<int> ParseSelectedPages(string selectedPageInput)
    {
        var selectedPages = new List<int>();

        var selectPageParts = selectedPageInput.Split(',', StringSplitOptions.RemoveEmptyEntries |
                                                           StringSplitOptions.TrimEntries);
        foreach (var s in selectPageParts)
        {
            var stringValues = s.Trim().Replace(" ", string.Empty);

            if (stringValues.Contains('-'))
            {
                var subStringValues = stringValues.Split('-', StringSplitOptions.RemoveEmptyEntries |
                                                              StringSplitOptions.TrimEntries);
                // Assume two values
                if (int.TryParse(subStringValues[0], out var first))
                {
                    if (int.TryParse(subStringValues[1], out var last))
                    {
                        if (first >= last)
                        {
                            _consoleWriter.WriteInfo(string.Empty);
                            _consoleWriter.WriteInfo(" Invalid range input: ", $"{first}-{last}", icon: ":warning:");
                            return [];
                        }

                        var pagesInRange = Enumerable.Range(first, (last - first) + 1).ToList();
                        selectedPages.AddRange(pagesInRange);
                    }
                }
            }

            if (int.TryParse(stringValues, out var v))
                selectedPages.Add(v);
        }

        if (selectedPages.Count > 10_000)
        {
            _consoleWriter.WriteInfo(string.Empty);
            _consoleWriter.WriteInfo(" Invalid input, would render more than 10 000 lines: ", $"{selectedPageInput}", icon: ":warning:");
            return [];
        }

        return selectedPages;
    }

    public static ViewFileResult GetLines(List<int> lineIndexes, FileReaderResult input)
    {
        var rows = new List<List<string>>();
        var selectedColumnIndexesArray = input.FileColumns.Where(q => q.SelectedForOutput).Select(p => p.Index).ToArray();
        var selectedColumnIndexes = new ReadOnlySpan<int>(selectedColumnIndexesArray);

        foreach (var lineIndex in lineIndexes)
        {
            var index = lineIndex - 1;

            if (index > input.FileLines.Length - 1 ||
                index < 0)
                continue;

            var rawLine = input.FileLines[index].RawLine;

            if (rawLine is null) continue;

            var columns = ParseColumns(rawLine, input.FileColumns.Count, input.Delimiter);

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
            FileColumns = input.SelectedFileColumns,
            Page = 1,
            PageSize = rows.Count,
            ResultPage = rows,
            TotalPages = 1,
            TotalRecords = rows.Count,
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
