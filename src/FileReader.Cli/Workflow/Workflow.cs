using System.Diagnostics;
using FileReader.Cli.ConsoleWriters;

namespace FileReader.Cli.Workflow;

public class WorkflowRunner(IConsoleWriter _consoleWriter)
{
    public async Task<WorkflowResults> Execute(Workflow workflow)
    {
        var workflowResult = new WorkflowResults();

        var sw = Stopwatch.StartNew();

        foreach (var stage in workflow.Stages)
        {
            foreach (var w in stage.Steps)
            {
                await ExecuteStep(workflowResult, stage, w);
            }

            await Task.WhenAll(stage.ParallelTasks);

            Console.WriteLine("");
        }

        sw.Stop();

        _consoleWriter.WriteInfo("Workflow completed",
                                 $"",
                                 addNewLine: true,
                                 elapsed: sw.Elapsed);

        return workflowResult;
    }

    private async Task ExecuteStep(WorkflowResults workflowResult, WorkflowStages stage, KeyValuePair<string, WorkflowStep> w)
    {
        if (w.Value.StepType == WorkflowStepType.Parallel)
        {
            stage.ParallelTasks.Add(Task.Run(async () =>
            {
                _consoleWriter.WriteInfo(w.Value.StartMessage, addNewLine: true, icon: ":rocket:");

                await w.Value.Action(w.Value, workflowResult);

                _consoleWriter.WriteInfo(w.Value.CompletedMessage.Message,
                                         w.Value.CompletedMessage.TaskOutput,
                                         elapsed: w.Value.CompletedMessage.Elapsed,
                                         addNewLine: false,
                                         icon: ":check_mark_button:");

            }));
        }
        else if (w.Value.ProgressType == WorkflowProgressType.Status)
        {
            await _consoleWriter.WriteStatus(w.Value.StartMessage, async (ctx) =>
            {
                await w.Value.Action(w.Value, workflowResult);
            });

            _consoleWriter.WriteInfo(w.Value.CompletedMessage.Message,
                                     w.Value.CompletedMessage.TaskOutput,
                                     elapsed: w.Value.CompletedMessage.Elapsed,
                                     addNewLine: false,
                                     icon: ":check_mark_button:");
        }
        else if (w.Value.ProgressType == WorkflowProgressType.Message)
        {
            // Info messages
            await w.Value.Action(w.Value, workflowResult);
        }
    }
}

public class Workflow(IConsoleWriter _consoleWriter)
{
    public required List<WorkflowStages> Stages { get; set; }

    public async Task ExecuteStep(WorkflowResults workflowResult, WorkflowStages stage, KeyValuePair<string, WorkflowStep> w)
    {
        if (w.Value.StepType == WorkflowStepType.Parallel)
        {
            stage.ParallelTasks.Add(Task.Run(async () =>
            {
                _consoleWriter.WriteInfo(w.Value.StartMessage, addNewLine: true, icon: ":rocket:");

                await w.Value.Action(w.Value, workflowResult);

                _consoleWriter.WriteInfo(w.Value.CompletedMessage.Message,
                                         w.Value.CompletedMessage.TaskOutput,
                                         elapsed: w.Value.CompletedMessage.Elapsed,
                                         addNewLine: false,
                                         icon: ":check_mark_button:");

            }));
        }
        else if (w.Value.ProgressType == WorkflowProgressType.Status)
        {
            await _consoleWriter.WriteStatus(w.Value.StartMessage, async (ctx) =>
            {
                await w.Value.Action(w.Value, workflowResult);
            });

            _consoleWriter.WriteInfo(w.Value.CompletedMessage.Message,
                                     w.Value.CompletedMessage.TaskOutput,
                                     elapsed: w.Value.CompletedMessage.Elapsed,
                                     addNewLine: false,
                                     icon: ":check_mark_button:");
        }
        else if (w.Value.ProgressType == WorkflowProgressType.Message)
        {
            // Info messages
            await w.Value.Action(w.Value, workflowResult);
        }
    }
}

public class WorkflowStages
{
    public required Dictionary<string, WorkflowStep> Steps { get; set; } = new();
    public List<Task> ParallelTasks { get; set; } = new();
}

public class WorkflowStep
{
    public required string StartMessage { get; set; }
    public required WorkflowOutput CompletedMessage { get; set; }

    public required Func<WorkflowStep, WorkflowResults, Task> Action { get; set; }

    public required WorkflowStepType StepType { get; set; }

    public required WorkflowProgressType ProgressType { get; set; }
}

public enum WorkflowStepType
{
    Sequential,
    Parallel
}

public enum WorkflowProgressType
{
    Status,
    Message,
}

public class WorkflowOutput(string message, string? taskOutput = null, TimeSpan? elapsed = null)
{
    public string Message { get; private set; } = message;
    public string? TaskOutput { get; private set; } = taskOutput;
    public TimeSpan? Elapsed { get; private set; } = elapsed;
}

public class WorkflowResults
{
    public byte? Delimiter { get; set; }
    public List<FileColumn>? FileColumns { get; set; }
    public DataLine[]? FileLines { get; set; }
    public string? FileHash { get; set; }
}