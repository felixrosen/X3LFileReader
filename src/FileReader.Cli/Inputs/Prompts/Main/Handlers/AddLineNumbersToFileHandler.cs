using System.Diagnostics;
using System.Text;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Main.Handlers;

public class AddLineNumbersToFileHandler : BaseHandler<AddLineNumbersToFileHandler.Input, Task>
{
    public class Input
    {
        public required FileInfo FileInfo { get; init; }
        public required string DelimiterString { get; init; }
        public required  Encoding Encoding { get; init; }
    }

    public AddLineNumbersToFileHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> Handle(Input input)
    {
        ArgumentNullException.ThrowIfNull(input.FileInfo.DirectoryName, "DirectoryName");

        var inputPath = input.FileInfo.FullName;
        var outputPath = Path.Combine(input.FileInfo.DirectoryName, $"LN_{input.FileInfo.Name}");

        var readBufferSize = 1024 * 1024 * 4; // 4 MB read buffer
        var writeBufferSize = 1024 * 1024 * 4; // 4 MB write buffer

        var sw = Stopwatch.StartNew();

        using (var reader = new StreamReader(inputPath, input.Encoding, detectEncodingFromByteOrderMarks: false, bufferSize: readBufferSize))
        using (var writer = new StreamWriter(outputPath, append: false, input.Encoding, bufferSize: writeBufferSize))
        {
            string? line;
            long lineNumber = 0;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if(lineNumber == 0)
                    writer.WriteLine($"Line #{input.DelimiterString}{line}");
                else
                    writer.WriteLine($"{lineNumber}{input.DelimiterString}{line}");

                lineNumber++;
            }
        }

        File.Delete(inputPath);
        File.Move(outputPath, inputPath);

        sw.Stop();

        _consoleWriter.WriteInfo("Line numbers appended to: ",
                                 input.FileInfo.Name,
                                 taskOutputColor: "yellow",
                                 addNewLine: true,
                                 icon: ":check_mark_button:",
                                 elapsed: sw.Elapsed);

        return Task.CompletedTask;
    }
}
