using System.Text;
using FileReader.Cli.Services;

namespace FileReader.Cli.Inputs.Prompts.Export.Handlers;

public static class WriteToFile
{
    public static async Task<string> Write(FileInfo fileInfo, string info, StringBuilder sb)
    {
        var filename = $"{fileInfo.Name.Replace(fileInfo.Extension, string.Empty)}_{info}";
        var exportsFolder = FolderService.ExportsPath;

        Directory.CreateDirectory(exportsFolder); // safe even if it exists

        var filePath = Path.Combine(exportsFolder, $"{filename}_{DateTime.Now.Ticks}.csv");

        await File.WriteAllTextAsync(filePath, sb.ToString(), encoding: Encoding.UTF8);

        return filePath;
    }
}

public static class ConstructExportResult
{
    public static StringBuilder Construct(List<FileColumn> columns, List<List<string>> rows)
    {
        var sb = new StringBuilder();

        foreach(var c in columns)
        {
            sb.Append($"{c.Name};");
        }

        sb.Append(Environment.NewLine);

        foreach (var row in rows)
        {
            foreach(var column in row)
            {
                sb.Append($"{column};");
            }

            sb.Append(Environment.NewLine);
        }

        return sb;
    }
}