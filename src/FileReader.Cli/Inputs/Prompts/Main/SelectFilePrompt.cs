using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.Inputs.Types;
using FileReader.Cli.Services;

namespace FileReader.Cli.Inputs.Prompts.Main;

public class SelectFilePrompt : BasePrompt<Task, Task<SelectFileResult>>
{
    SelectionItem _mainSelection;

    public SelectFilePrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new ReadNewFilePrompt(_consoleWriter)
                {
                    Name = "Read new file",
                    State = SelectionPromptState.ReadNewFile,
                },
                new ReadExistingFilePrompt(_consoleWriter)
                {
                    Name = "Read existing file",
                    State = SelectionPromptState.ReadExistingFile,
                },
                new InlineHandler<Task, Task>
                {
                    Name = "Clear existing files",
                    State = SelectionPromptState.ClearExistingFiles,
                    Handler = (c, t) => ClearExistingFiles(t, c),
                },
                new SelectionItem
                {
                    Name = "Exit",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }


    public override async Task<SelectFileResult> AcceptInput(Task input)
    {
        var ms = MainPrompt.ShowPrompt("What would you like to do?", string.Empty, _mainSelection.SubItems);

        while (ms.State != SelectionPromptState.Exit)
        {
            if (ms is ReadNewFilePrompt readNewFilePrompt)
            {
                var result = await readNewFilePrompt.AcceptInput(input);

                if (result.Success is true)
                    return result;
            }

            if (ms is ReadExistingFilePrompt readExistingFilePrompt)
            {
                var result = await readExistingFilePrompt.AcceptInput(input);

                if (result.Success is true)
                    return result;
            }

            if (ms is InlineHandler<Task, Task> handler)
            {
                await handler.Handler(_consoleWriter, Task.CompletedTask);
            }

            ms = MainPrompt.ShowPrompt("What would you like to do?", string.Empty, _mainSelection.SubItems);

            if (ms.State == SelectionPromptState.Exit)
                break;
        }

        return new SelectFileResult { Success = false, Message = "Exit" };
    }

    private static Task ClearExistingFiles(Task t, IConsoleWriter consoleWriter)
    {
        var existingFilesPath = FolderService.PrevReadFilesPath;

        if (Directory.Exists(existingFilesPath))
        {
            Directory.Delete(existingFilesPath, true);
        }

        consoleWriter.WriteInfo(" Cleared existing files", existingFilesPath, icon: ":information:", addNewLine: true);

        return Task.CompletedTask;
    }
}
