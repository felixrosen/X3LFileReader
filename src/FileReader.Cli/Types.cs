namespace FileReader.Cli;

public class DataLine
{
    public byte[]? RawLine { get; set; }
    //public byte[][]? RawColumns { get; set; }
}

public class FileColumn
{
    public required int Index { get; set; }
    public required string Name { get; set; }

    public bool SelectedForOutput { get; set; } = true;

    public bool OrderBy { get; set; } = false;

    public override string ToString()
    {
        return $"{Index}: {Name} (Selected: {SelectedForOutput})";
    }
}
