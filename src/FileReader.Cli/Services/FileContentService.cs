using System.Buffers;
using FileReader.Cli.FileReaders.Services;

namespace FileReader.Cli.Services;

public class FileContentService
{
    public static byte[][] ParseColumns(byte[] rawLine, int numberOfColumns, byte delimiter)
    {
        var readOnlySequence = new ReadOnlySequence<byte>(rawLine);
        var reader = new SequenceReader<byte>(readOnlySequence);
        var parts = new byte[numberOfColumns][];

        while (!reader.End)
        {
            for (var i = 0; i < numberOfColumns; i++)
            {
                var linePart = reader.Read(delimiter);

                if (linePart.Length == 0)
                {
                    parts[i] = [];
                    continue;
                }

                parts[i] = linePart;
            }

            if (!reader.End) // If we reached the end of the line, we can break.                
                break;
        }

        return parts;
    }
}
