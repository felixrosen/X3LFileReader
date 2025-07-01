namespace FileReader.Cli.Inputs.Types;

public class StoredFileSettings
{
    public required string FileName { get; set; }
    public required List<FileColumn> FileColumns { get; set; } = [];
}
