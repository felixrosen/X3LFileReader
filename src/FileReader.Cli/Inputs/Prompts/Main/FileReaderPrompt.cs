using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Main.Handlers;
using FileReader.Cli.Inputs.Prompts.Search;
using FileReader.Cli.Inputs.Prompts.Settings;
using FileReader.Cli.Inputs.Prompts.View;
using FileReader.Cli.Inputs.Types;
using FileReader.Cli.Services;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Main;

public class FileReaderPrompt : BasePrompt<SelectFileResult, Task>
{    
    private InMemoryFileReader? _fileReader;
    private SelectionItem _mainSelection;

    public FileReaderPrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new ViewFilePrompt(_consoleWriter)
                {
                    Name = "View",
                    State = SelectionPromptState.View,
                },
                new SearchFilePrompt(_consoleWriter)
                {
                    Name = "Search",
                    State = SelectionPromptState.SearchWithPaging,
                },
                new FileSettingsPrompt(_consoleWriter)
                {
                    Name = "Settings",
                    State = SelectionPromptState.Settings,
                },
                new SelectionItem
                {
                    Name = "Exit",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }

    public override async Task<Task> AcceptInput(SelectFileResult selectedFileResult)
    {

        if (selectedFileResult.Success is false || selectedFileResult.FullName is null || selectedFileResult.Delimiter is null || selectedFileResult.Encoding is null)
        {
            _consoleWriter.WriteError(selectedFileResult.Message);
            return Task.FromResult(Task.CompletedTask);
        }

        if (selectedFileResult.IsNewFile && selectedFileResult.AddLineNumbers)
        {
            var addLineNumbersHandler = new AddLineNumbersToFileHandler(_consoleWriter);

            await _consoleWriter.WriteStatus("Adding line numbers to file", async (ctx) =>
            {
                await addLineNumbersHandler.Handle(new AddLineNumbersToFileHandler.Input
                {
                    DelimiterString = selectedFileResult.Encoding.GetString([selectedFileResult.Delimiter.Value]),
                    Encoding = selectedFileResult.Encoding,
                    FileInfo = new FileInfo(selectedFileResult.FullName),
                });
            });
        }

        _fileReader = new InMemoryFileReader(new FileInfoExtended(selectedFileResult.FullName),
                                             _consoleWriter,
                                             new FileReaders.FileReaderSettings
                                             {
                                                 Delimiter = selectedFileResult.Delimiter.Value,
                                                 Encoding = selectedFileResult.Encoding,
                                                 WorkerCount = Environment.ProcessorCount / 2 - 2,
                                                 LinesBatchSize = 100_000,
                                                 MinimumBufferSizeInMb = 16 * 1024 * 1024 // 16 MB
                                             });

        var fileReaderResult = await _fileReader.Read();

        // We have successfully read the file, if it is new store so we can open it again
        if (selectedFileResult.IsNewFile)
        {
            var prevReadFilesPath = FolderService.PrevReadFilesPath;

            Directory.CreateDirectory(prevReadFilesPath); // safe even if it exists

            var filePath = Path.Combine(prevReadFilesPath, $"{fileReaderResult.FileHash}.json");
            var fileContent = JsonSerializer.Serialize(selectedFileResult, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(filePath, fileContent, encoding: Encoding.UTF8);
        }

        // Must apply settings here
        var settings = await ReadSettings(fileReaderResult);
        if (settings is not null)
            fileReaderResult.FileColumns = settings.FileColumns;

        var writeColumnsCommand = new WriteColumnsHandler(_consoleWriter);
        await writeColumnsCommand.Handle(fileReaderResult);

        var ms = MainPrompt.ShowPrompt("What would you like to do?", fileReaderResult.FileInfo.FileInfo.Name, _mainSelection.SubItems);

        SelectionItem? previousSelection = null;

        while (ms.State != SelectionPromptState.Exit)
        {
            OutputRule(ms);

            if (ms is ViewFilePrompt viewFilePrompt)
            {
                await viewFilePrompt.AcceptInput(fileReaderResult);
            }

            if (ms is SearchFilePrompt searchFilePrompt)
            {
                await searchFilePrompt.AcceptInput(fileReaderResult);
            }

            if (ms is FileSettingsPrompt settingsPrompt)
            {
                await settingsPrompt.AcceptInput(fileReaderResult);
            }

            AnsiConsole.WriteLine("");

            ms = ms switch
            {
                var prompt when prompt.State == SelectionPromptState.Exit => ms,

                var prompt when prompt.SubItems.Count > 0 => MainPrompt.ShowPrompt("What would you like to do?", fileReaderResult.FileInfo.FileInfo.Name, ms.SubItems),

                _ => MainPrompt.ShowPrompt("What would you like to do?", fileReaderResult.FileInfo.FileInfo.Name, _mainSelection.SubItems)
            };

            previousSelection = ms;
        }

        return Task.CompletedTask;
    }

    private static void OutputRule(SelectionItem ms)
    {
        var rule = new Rule($"[white]{ms.Name}[/]");
        rule.Justification = Justify.Left;
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine("");
    }

    private async Task<StoredFileSettings?> ReadSettings(FileReaderResult fileReaderResult)
    {
        var sw = Stopwatch.StartNew();

        if (fileReaderResult.FileHash is { Length: < 1 })
        {
            _consoleWriter.WriteError("Could not store settings, missing file hash");
            return null;
        }

        var folderSettingsPath = FolderService.FileSettingsPath;

        var fileName = $"{fileReaderResult.FileHash}.json";
        var filePath = Path.Combine(folderSettingsPath, fileName);

        if (File.Exists(filePath) is false)
        {
            _consoleWriter.WriteInfo(" No settings stored", elapsed: sw.Elapsed, icon: ":information:");
            return null;
        }

        var fileData = await File.ReadAllTextAsync(filePath);
        var settings = JsonSerializer.Deserialize<StoredFileSettings>(fileData);

        if (settings is null)
        {
            _consoleWriter.WriteInfo(" Could not read settings from file", elapsed: sw.Elapsed, icon: ":information:");
            return null;
        }

        sw.Stop();

        _consoleWriter.WriteInfo("Settings read from file: ", fileName, elapsed: sw.Elapsed, icon: ":check_mark_button:");

        return settings;
    }
}
