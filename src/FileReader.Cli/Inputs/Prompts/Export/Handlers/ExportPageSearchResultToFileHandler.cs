using System.Diagnostics;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Search.Handlers;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Export.Handlers;

public class ExportPageSearchResultToFileHandler : BaseHandler<(SearchResult SearchResult, FileReaderResult FileReaderResult), Task>
{
    public ExportPageSearchResultToFileHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override async Task<Task> Handle((SearchResult SearchResult, FileReaderResult FileReaderResult) input)
    {
        var sw = Stopwatch.StartNew();
        
        var sb = ConstructExportResult.Construct(input.SearchResult.FileColumns, input.SearchResult.ResultPage);

        var filePath = await WriteToFile.Write(input.FileReaderResult.FileInfo.FileInfo,
                                               $"{input.SearchResult.SearchTerm}_Page_{input.SearchResult.Page}",
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
