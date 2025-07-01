namespace FileReader.Cli.ConsoleWriters;

public interface IConsoleWriter
{
    void WriteInfo(string message,
                string? taskOutput = null,
                bool addNewLine = false,
                TimeSpan? elapsed = null,
                string? taskOutputColor = null,
                string? icon = null);

    void WriteWarning(string message);
    void WriteError(string message);
    void WriteTable(string[] headers, IEnumerable<string[]> rows);
    void WriteTable(List<string> headers, List<List<string>> rows);

    Task WriteStatus(string initialMessage, Func<IStatusScope, Task> work);
    void WriteFiglet(string text);
}

public interface IStatusScope
{
    void Update(string message);

    Task UpdateAsync(string message);
}

public interface IStatusService
{
    void Run(string initialMessage, Action<IStatusScope> work);
    Task RunAsync(string initialMessage, Func<IStatusScope, Task> work);
}