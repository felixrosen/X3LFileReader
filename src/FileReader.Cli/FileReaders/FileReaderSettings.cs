using System.Text;

namespace FileReader.Cli.FileReaders;

public class FileReaderSettings
{
    public required int MinimumBufferSizeInMb { get; init; }
    public required int WorkerCount { get; init; }
    public required int LinesBatchSize { get; init; }
    public required byte Delimiter { get; init; }
    public required Encoding Encoding { get; set; }

    public string DelimiterString => Encoding.GetString([Delimiter]);
}
