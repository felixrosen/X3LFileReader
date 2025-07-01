using System.Text;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;

namespace FileReader.Cli.Inputs.Prompts.Main;

public class ReadNewFilePrompt : BasePrompt<Task, Task<SelectFileResult>>
{

    public ReadNewFilePrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<SelectFileResult> AcceptInput(Task input)
    {
        await Task.Delay(0);

        var filePath = AnsiConsole.Prompt(new TextPrompt<string>("Enter file path: "));

        if (!File.Exists(filePath))
        {
            _consoleWriter.WriteInfo(" Could not find file", filePath, icon: ":information:", addNewLine: true);
            return new SelectFileResult { Success = false, Message = "File does not exist" };
        }

        var encodingString = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                  .Title("Select encoding")
                                  .AddChoices([
                                      "UTF8",
                                      "ASCII",
                                  ]));

        AnsiConsole.WriteLine($"Selected encoding: {encodingString}");

        var delimiter = AnsiConsole.Prompt(new TextPrompt<string>("Enter delimiter (one char): ").Validate((s) =>
        {
            if (s.Length != 1)
                return ValidationResult.Error("Delimiter must be one char");

            return ValidationResult.Success();
        }));

        var encoding = encodingString switch
        {
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8,
        };

        var addLineNumbers = AnsiConsole.Prompt(new TextPrompt<bool>("Add line numbers to file?")
                                        .AddChoice(true)
                                        .AddChoice(false)
                                        .DefaultValue(false)
                                        .WithConverter(choice => choice ? "y" : "n"));

        var delimiterByte = encoding.GetBytes(delimiter)[0];

        return new SelectFileResult
        {
            Success = true,
            Message = string.Empty,

            FullName = filePath,
            Delimiter = delimiterByte,
            EncodingString = encodingString,
            IsNewFile = true,
            AddLineNumbers = addLineNumbers,
        };
    }
}
