using System.Text;

namespace FileReader.Cli.Services;

public class EncodingService
{
    public static Encoding GetEncodingFromString(string? encoding) => encoding is null ? Encoding.UTF8 : encoding switch
    {
        "ASCII" => Encoding.ASCII,
        _ => Encoding.UTF8,
    };
}
