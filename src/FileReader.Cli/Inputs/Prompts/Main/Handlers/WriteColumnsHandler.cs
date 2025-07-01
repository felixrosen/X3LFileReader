using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Main.Handlers;

public class WriteColumnsHandler : BaseHandler<FileReaderResult, Task>
{
    public WriteColumnsHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    public override Task<Task> Handle(FileReaderResult fileReaderResult)
    {
        _consoleWriter.WriteTable(["Index", "Name"],
                                  [
                                    .. fileReaderResult.FileColumns
                                                       .Where(q => q.SelectedForOutput)
                                                       .Select(p => new[] { p.Index.ToString(), p.Name })
                                  ]);

        var orderColumn = fileReaderResult.FileColumns.FirstOrDefault(q => q.OrderBy);
        if (orderColumn is not null)
            _consoleWriter.WriteInfo($"Column to order by: ", orderColumn.Name, taskOutputColor: "yellow", addNewLine: true);
        else
            _consoleWriter.WriteInfo($"Column to order by: ", "", taskOutputColor: "yellow", addNewLine: true);

        return Task.FromResult(Task.CompletedTask);
    }
}
