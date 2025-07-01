using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.Services;
using FileReader.Cli.Services;
using FileReader.Cli.Workflow;

namespace FileReader.Cli.FileReaders.InMemory;

public class FileReaderResult
{
    public required byte Delimiter { get; init; }
    public required FileInfoExtended FileInfo { get; init; }
    public required List<FileColumn> FileColumns { get; set; } = [];

    public List<FileColumn> SelectedFileColumns => [.. FileColumns.Where(q => q.SelectedForOutput)];

    public required DataLine[] FileLines { get; set; } = [];

    public required string FileHash { get; init; }

    public void ResetFileColumns()
    {
        foreach(var c in FileColumns)
        {
            c.SelectedForOutput = true;
            c.OrderBy = false;
        }
    }
}

public class InMemoryFileReader
{
    private int _totalNumberOfLinesProcessedInChannel;

    private readonly byte _return = Encoding.ASCII.GetBytes("\n")[0];
    private readonly IConsoleWriter _consoleWriter;
    private readonly FileReaderSettings _fileReaderSettings;
    private readonly FileInfoExtended _fileInfo;
    private readonly Dictionary<string, WorkflowStep> _fileReaderWorkflow;
    private readonly Dictionary<string, WorkflowStep> _fileContentWorkflow;
    private readonly Channel<List<byte[]>> _channel;

    private Workflow.Workflow Workflow => new(_consoleWriter)
    {
        Stages =
        [
            new WorkflowStages
            {
                Steps = _fileReaderWorkflow,
            },
            new WorkflowStages
            {
                Steps = _fileContentWorkflow
            }

        ]
    };

    public InMemoryFileReader(FileInfoExtended fileInfo,
                              IConsoleWriter consoleWriter,
                              FileReaderSettings fileReaderSettings)
    {
        _consoleWriter = consoleWriter;
        _fileReaderSettings = fileReaderSettings;
        _fileInfo = fileInfo;

        _channel = Channel.CreateUnbounded<List<byte[]>>(new UnboundedChannelOptions
        {
            SingleReader = _fileReaderSettings.WorkerCount == 1 ? true : false,
            SingleWriter = true,
        });

        _fileReaderWorkflow = new()
        {
            ["InfoMessages"] = new WorkflowStep
            {
                StartMessage = "Printing info messages",
                CompletedMessage = new WorkflowOutput("Info messages printed"),
                Action = PrintReaderInfo,
                ProgressType = WorkflowProgressType.Message,
                StepType = WorkflowStepType.Sequential,
            },
            ["ReadLinesFromChannel"] = new WorkflowStep
            {
                StartMessage = "Starting workers",
                CompletedMessage = new WorkflowOutput(string.Empty),
                Action = ReadLinesFromChannel,
                ProgressType = WorkflowProgressType.Status,
                StepType = WorkflowStepType.Parallel,
            },
            ["CountLinesInFile"] = new WorkflowStep
            {
                StartMessage = "Counting number of lines in file",
                CompletedMessage = new WorkflowOutput(string.Empty),
                Action = CountNumberOfLinesInFile,
                ProgressType = WorkflowProgressType.Status,
                StepType = WorkflowStepType.Sequential,
            },
            ["InitializeDataStructures"] = new WorkflowStep
            {
                StartMessage = "Initializing data structures",
                CompletedMessage = new WorkflowOutput(string.Empty),
                Action = InitializeDataStructures,
                ProgressType = WorkflowProgressType.Status,
                StepType = WorkflowStepType.Sequential,
            },
            ["ReadLinesFromFileToMemory"] = new WorkflowStep
            {
                StartMessage = "Reading lines from file to memory",
                CompletedMessage = new WorkflowOutput(string.Empty),
                Action = ReadLinesFromFileToMemory,
                ProgressType = WorkflowProgressType.Status,
                StepType = WorkflowStepType.Sequential,
            },
        };

        _fileContentWorkflow = new()
        {
            ["CalcFileHash"] = new WorkflowStep
            {
                StartMessage = "Calculating file hash",
                CompletedMessage = new WorkflowOutput(string.Empty),
                Action = CalculateFileHash,
                ProgressType = WorkflowProgressType.Status,
                StepType = WorkflowStepType.Sequential,
            },
        };
    }

    public async Task<FileReaderResult> Read()
    {
        var workflowRunner = new WorkflowRunner(_consoleWriter);
        var workflowResult = await workflowRunner.Execute(Workflow);              

        return new FileReaderResult
        {
            Delimiter = _fileReaderSettings.Delimiter,
            FileInfo = _fileInfo,

            FileLines = workflowResult?.FileLines ?? throw new NullReferenceException("File lines not set"),
            FileColumns = workflowResult.FileColumns ?? throw new NullReferenceException("File columns not set"),
            FileHash = workflowResult?.FileHash ?? throw new NullReferenceException("File hash not set"),
        };
    }    

    private Task ReadLinesFromChannel(WorkflowStep w, WorkflowResults workflowResults)
    {
        var indexPointer = 0;
        w.CompletedMessage = new WorkflowOutput("Workers completed processing");

        var workers = new List<Task>();
        for (var i = 0; i < _fileReaderSettings.WorkerCount; i++)
        {
            var id = i;
            workers.Add(Task.Run(async () =>
            {
                var workersLinesProcessed = 0;
                Stopwatch? sw = null;

                while (await _channel.Reader.WaitToReadAsync())
                {
                    await foreach (var lines in _channel.Reader.ReadAllAsync())
                    {
                        sw ??= Stopwatch.StartNew();

                        Interlocked.Add(ref workersLinesProcessed, lines.Count);
                        Interlocked.Add(ref _totalNumberOfLinesProcessedInChannel, lines.Count);

                        for (var i1 = 0; i1 < lines.Count; i1++)
                        {
                            var l = lines[i1];
                            workflowResults.FileLines![indexPointer].RawLine = l;

                            Interlocked.Increment(ref indexPointer);
                        }
                    }
                }

                sw?.Stop();
            }));
        }

        return Task.WhenAll(workers);
    }

    private async Task ReadLinesFromFileToMemory(WorkflowStep w, WorkflowResults workflowResults)
    {
        var numberOfLinesParsedFromFile = 0;

        var lineParserSw = Stopwatch.StartNew();
        using var stream = new FileStream(_fileInfo.FileInfo.FullName,
                                          FileMode.Open,
                                          FileAccess.Read,
                                          FileShare.Read,
                                          bufferSize: _fileReaderSettings.MinimumBufferSizeInMb,
                                          useAsync: true);

        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: _fileReaderSettings.MinimumBufferSizeInMb,
                                                                           minimumReadSize: _fileReaderSettings.MinimumBufferSizeInMb,
                                                                           leaveOpen: false));
        var temp = new List<byte[]>();


        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            while (TryReadLine(ref buffer, out var rosLine))
            {
                if(numberOfLinesParsedFromFile == 0)
                {
                    // Header
                    numberOfLinesParsedFromFile++;
                    ReadColumnsFromFile(rosLine.ToArray(), workflowResults);
                    continue;
                }
                
                numberOfLinesParsedFromFile++;                                

                temp.Add(rosLine.ToArray());

                if (temp.Count > 0 && temp.Count % _fileReaderSettings.LinesBatchSize == 0)
                {
                    // Write to channel in batches
                    var copy = new List<byte[]>(temp);
                    await _channel.Writer.WriteAsync(copy);
                    temp.Clear();
                }
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                if (temp.Count > 0)
                {
                    // Write to channel in batches
                    var copy = new List<byte[]>(temp);
                    await _channel.Writer.WriteAsync(copy);
                    temp.Clear();
                }

                break;
            }
        }

        lineParserSw.Stop();

        w.CompletedMessage = new WorkflowOutput("Lines read from file to memory:",
                                                $"{numberOfLinesParsedFromFile:N0}",
                                                lineParserSw.Elapsed);

        await reader.CompleteAsync();
        _channel.Writer.Complete();
    }

    private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        // Look for a EOL in the buffer.        
        var returnPosition = buffer.PositionOf(_return);

        if (returnPosition is null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, returnPosition.Value);

        if (line.Length > 0)
        {
            line = line.Slice(0, line.Length - 1); // Remove the \r at the end.
        }

        // Skip the line + the \n. If we don't we will just loop on the same line.
        buffer = buffer.Slice(buffer.GetPosition(1, returnPosition.Value));

        return true;
    }

    private Task PrintReaderInfo(WorkflowStep w, WorkflowResults workflowResults)
    {
        _consoleWriter.WriteInfo("Filename: ", _fileInfo.FileInfo.Name, addNewLine: false);
        _consoleWriter.WriteInfo("Encoding: ", _fileReaderSettings.Encoding.EncodingName, addNewLine: false);
        _consoleWriter.WriteInfo("Delimiter: ", _fileReaderSettings.Encoding.GetString([_fileReaderSettings.Delimiter]), addNewLine: false);
        _consoleWriter.WriteInfo($"Number for workers: ", $"{_fileReaderSettings.WorkerCount:N0}", addNewLine: false);
        _consoleWriter.WriteInfo($"Lines batch seize: ", $"{_fileReaderSettings.LinesBatchSize:N0}", addNewLine: true);

        return Task.CompletedTask;
    }

    private Task CountNumberOfLinesInFile(WorkflowStep w, WorkflowResults workflowResults)
    {
        var sw = Stopwatch.StartNew();

        var numberOfLines = $"{_fileInfo.NumberOfLines:N0}";

        sw.Stop();

        w.CompletedMessage = new WorkflowOutput("Number of lines in file:", $"{numberOfLines:N0}", sw.Elapsed);

        return Task.CompletedTask;
    }

    private Task InitializeDataStructures(WorkflowStep w, WorkflowResults workflowResults)
    {
        var sw = Stopwatch.StartNew();

        workflowResults.FileLines = new DataLine[_fileInfo.NumberOfLines];
        for (var i = 0; i < _fileInfo.NumberOfLines; i++)
        {
            workflowResults.FileLines[i] = new DataLine();
        }

        sw.Stop();

        w.CompletedMessage = new WorkflowOutput("Data structures initialized", elapsed: sw.Elapsed);
        return Task.CompletedTask;
    }

    private void ReadColumnsFromFile(byte[] columnRowBytes, WorkflowResults workflowResults)
    {
        var sw = Stopwatch.StartNew();        

        var readOnlySequence = new ReadOnlySequence<byte>(columnRowBytes);
        var reader = new SequenceReader<byte>(readOnlySequence);
        var columns = new List<byte[]>();

        while (!reader.End)
        {
            var linePart = reader.Read(_fileReaderSettings.Delimiter);

            if (linePart.Length == 0)
            {
                columns.Add([]);
                continue;
            }

            columns.Add(linePart);
        }

        workflowResults.FileColumns = [];

        for (var i = 0; i < columns.Count; i++)
        {
            var columnName = Encoding.UTF8.GetString(columns[i]);
            workflowResults.FileColumns.Add(new FileColumn
            {
                Index = i,
                Name = columnName.Replace("\uFEFF", string.Empty),
            });
        }

        sw.Stop();

        _consoleWriter.WriteInfo("Columns read from file, number of columns",
                                 $"{workflowResults.FileColumns.Count}",
                                 icon: ":check_mark_button:",
                                 elapsed: sw.Elapsed);
    }    

    private Task CalculateFileHash(WorkflowStep w, WorkflowResults workflowResults)
    {
        var sw = Stopwatch.StartNew();

        using var sha256 = SHA256.Create();
        using var stream = _fileInfo.FileInfo.OpenRead();

        var hashBytes = sha256.ComputeHash(stream);

        var fullNameBytes = sha256.ComputeHash(_fileReaderSettings.Encoding.GetBytes(_fileInfo.FileInfo.FullName));

        var fileBytes = BuffersService.Combine(hashBytes, fullNameBytes);

        workflowResults.FileHash = Convert.ToHexString(fileBytes);

        sw.Stop();

        w.CompletedMessage = new WorkflowOutput("Calculated file hash", elapsed: sw.Elapsed);

        return Task.CompletedTask;
    }
}