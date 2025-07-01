using FileReader.Cli.ConsoleWriters.Spectre;
using FileReader.Cli.Inputs.Prompts.Main;

namespace FileReader.Cli;

internal class Program
{    
    private static async Task Main(string[] args)
    {
        var prompt = new MainPrompt(new SpectreConsoleWriter(new SpectreStatusService()));
        await prompt.AcceptInput(Task.CompletedTask);

        return;

    }
}