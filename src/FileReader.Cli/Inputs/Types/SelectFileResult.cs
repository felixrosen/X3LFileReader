using System.Text;
using System.Text.Json.Serialization;
using FileReader.Cli.Services;

namespace FileReader.Cli.Inputs.Types;

public class SelectFileResult
{
    [JsonIgnore]
    public Encoding Encoding => EncodingService.GetEncodingFromString(EncodingString);

    public bool IsNewFile { get; set; }

    public string? FullName { get; set; }
    public string? EncodingString { get; set; }
    public byte? Delimiter { get; set; }

    public required bool Success { get; set; }
    public required string Message { get; set; }
    public bool AddLineNumbers { get; set; }
}