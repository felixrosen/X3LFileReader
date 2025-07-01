using System.Text;
using Spectre.Console;

namespace FileReader.Cli.ConsoleWriters.Spectre;

public class SpectreConsoleWriter : IConsoleWriter
{
    private readonly IStatusService _statusService;

    public SpectreConsoleWriter(IStatusService statusService)
    {
        // Configure the console to use UTF-8 encoding
        Console.OutputEncoding = Encoding.UTF8;
        _statusService = statusService;
    }

    public void WriteFiglet(string text)
    {
        AnsiConsole.Write(new Align(new FigletText(text).Color(Color.Blue), horizontal: HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    public async Task WriteStatus(string initialMessage, Func<IStatusScope, Task> work)
    {
        await _statusService.RunAsync(initialMessage, work);
    }

    public void WriteInfo(string message,
                          string? taskOutput = null,
                          bool addNewLine = false,
                          TimeSpan? elapsed = null,
                          string? taskOutputColor = null,
                          string? icon = null)
    {
        var sb = new StringBuilder();

        if(icon is { Length: > 0 })
            sb.Append(icon + " ");

        sb.Append(message);

        if(taskOutput is { Length: > 0 })
            sb.Append($" [bold {taskOutputColor ?? "yellow"}]{taskOutput}[/]");

        if(elapsed is not null)
            sb.Append($" [dim]({elapsed})[/]");

        AnsiConsole.MarkupLine(sb.ToString());

        if (addNewLine)
            AnsiConsole.WriteLine();
    }

    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
    }

    public void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
    }

    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {        
        var table = new Table();

        foreach (var h in headers)
        {
            table.AddColumn(h);
        }

        foreach (var r in rows)
        {
            table.AddRow(r.ToArray());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine(string.Empty);
    }

    public void WriteTable(List<string> headers, List<List<string>> rows)
    {
        var table = new Table();

        foreach (var h in headers)
        {
            table.AddColumn(h);
        }

        foreach (var r in rows)
        {
            table.AddRow(r.ToArray());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine(string.Empty);
    }
}

public class SpectreStatusService : IStatusService
{
    public void Run(string initialMessage, Action<IStatusScope> work)
    {
        AnsiConsole.Status().AutoRefresh(true).Start(initialMessage, ctx =>
        {
            var scope = new SpectreStatusScope(ctx);
            work(scope);
        });
    }

    public async Task RunAsync(string initialMessage, Func<IStatusScope, Task> work)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync(initialMessage, async ctx =>
            {
                var scope = new SpectreStatusScope(ctx);
                await work(scope);
            });
    }

    private class SpectreStatusScope : IStatusScope
    {
        private readonly StatusContext _context;

        public SpectreStatusScope(StatusContext context)
        {
            _context = context;
        }

        public void Update(string message)
        {
            _context.Status = message;
        }


        public Task UpdateAsync(string message)
        {
            _context.Status = message;

            return Task.CompletedTask;
        }
    }
}