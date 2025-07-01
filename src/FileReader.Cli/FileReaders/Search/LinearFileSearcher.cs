using System.Buffers;
using System.Diagnostics;
using System.Text;
using FileReader.Cli.FileReaders.Services;
using FileReader.Cli.Services;

namespace FileReader.Cli.FileReaders.Search;

public class LinearFileSearcher
{
    public static async Task<List<List<string>>> Search(DataLine[] RawLines,
                                                        int[] selectedOutputColumns,
                                                        byte[] searchCriteriaBytes,
                                                        int numberOfFileColumns,
                                                        byte delimiter)
    {
        var chunkSize = RawLines.Length / (Environment.ProcessorCount / 2) + 1;
        var tasks = new List<Task<List<List<string>>>>();

        var sw = Stopwatch.StartNew();

        foreach (var chunk in RawLines.Chunk(chunkSize))
        {
            var task = Task.Run(() =>
            {
                var searchCounter = 0;
                var rows = new List<List<string>>();
                var selectedOutputColumnIndexes = new ReadOnlySpan<int>(selectedOutputColumns);

                foreach (var l in chunk)
                {
                    var row = new List<string>();

                    if (l.RawLine is null)
                    { searchCounter++; continue; }

                    if (l.RawLine.Length == 0)
                    { searchCounter++; continue; }

                    if (l.RawLine.AsSpan().IndexOf(searchCriteriaBytes) >= 0)
                    {
                        var columns = FileContentService.ParseColumns(l.RawLine, numberOfFileColumns, delimiter);

                        for (var i = 0; i < columns.Length; i++)
                        {
                            if (!selectedOutputColumnIndexes.Contains(i))
                                continue;

                            row.Add(Encoding.UTF8.GetString(columns[i]));
                        }

                        rows.Add(row);
                    }
                }

                return rows;
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        sw.Stop();

        var result = new List<List<string>>();

        foreach (var t in tasks)
        {
            result.AddRange(t.Result);
        }

        return result;
    }    
}
