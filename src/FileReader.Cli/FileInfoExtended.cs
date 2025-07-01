using System.Diagnostics;

namespace FileReader.Cli;

public class FileInfoExtended
{
    private readonly string _filePath;

    public FileInfo FileInfo { get; private set; }

    private int? _numberOfRows;

    public void InitializeNumberOfLines()
    {
        _ = NumberOfLines;
    }


    public int NumberOfLines
    {
        get
        {
            if (_numberOfRows is not null)
                return _numberOfRows.Value;

            var sw = Stopwatch.StartNew();

            var rowCount = CountLinesInFile();

            _numberOfRows = rowCount;

            sw.Stop();

            //Console.WriteLine($"Counted {_numberOfRows:N0} rows in file in {sw.Elapsed}");

            return _numberOfRows ?? 0;
        }
    }

    private int CountLinesInFile()
    {
        var rowCount = 0;
        var newLine = (byte)'\n';
        const int bufferSize = 2_000 * 1024 * 1024; // 1 MB buffer

        using (var fs = File.OpenRead(_filePath))
        {
            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == newLine)
                        rowCount++;
                }
            }
        }

        return rowCount;
    }

    public FileInfoExtended(string filePath)
    {
        _filePath = filePath;

        FileInfo = new FileInfo(filePath);
    }
}
