using System.Diagnostics;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.View;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Export.Handlers;

public class ExportLinesToFileHandler : BaseHandler<(ViewFileResult ViewFileResult, FileReaderResult FileReaderResult), Task>
{
    public ExportLinesToFileHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> Handle((ViewFileResult ViewFileResult, FileReaderResult FileReaderResult) input)
    {
        var sw = Stopwatch.StartNew();

        var sb = ConstructExportResult.Construct(input.FileReaderResult.SelectedFileColumns, input.ViewFileResult.ResultPage);

        var filePath = await WriteToFile.Write(input.FileReaderResult.FileInfo.FileInfo,
                                               "Lines",
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
