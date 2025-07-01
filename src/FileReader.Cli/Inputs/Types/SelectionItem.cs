using FileReader.Cli.ConsoleWriters;

namespace FileReader.Cli.Inputs.Types;

public class SelectionItem
{
    public string Name { get; set; } = string.Empty;

    public SelectionPromptState State { get; set; }

    public List<SelectionItem> SubItems { get; set; } = new();

    public override string ToString() => Name;
}

public class InlineHandler<TInput, TResult> : SelectionItem
{
    public required Func<IConsoleWriter, TInput, TResult> Handler { get; set; }
}

public abstract class BasePrompt<TInput, TResult> : SelectionItem
{
    protected readonly IConsoleWriter _consoleWriter;

    public BasePrompt(IConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter;
    }

    public abstract TResult AcceptInput(TInput input);

}

public abstract class BaseHandler<TInput, TResult> : SelectionItem
{
    public readonly IConsoleWriter _consoleWriter;

    public BaseHandler(IConsoleWriter consoleWriter)
    {
        _consoleWriter = consoleWriter;
    }

    public abstract Task<TResult> Handle(TInput input);
}