using System.Diagnostics;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.View;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Export.Handlers;

public class ExportPageToFileHandler : BaseHandler<(ViewFileResult ViewFileResult, int Page, FileReaderResult FileReaderResult), Task>
{
    public ExportPageToFileHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> Handle((ViewFileResult ViewFileResult, int Page, FileReaderResult FileReaderResult) input)
    {
        var sw = Stopwatch.StartNew();

        var sb = ConstructExportResult.Construct(input.FileReaderResult.SelectedFileColumns, input.ViewFileResult.ResultPage);

        var filePath = await WriteToFile.Write(input.FileReaderResult.FileInfo.FileInfo,
                                               $"Page_{input.Page}",
                                               sb);

        sw.Stop();

        _consoleWriter.WriteInfo(" Result exported to: ",
                                 filePath,
                                 taskOutputColor: "yellow",
                                 addNewLine: true,
                                 icon: ":information:",
                                 elapsed: sw.Elapsed);

        return Task.CompletedTask;
    }
}
