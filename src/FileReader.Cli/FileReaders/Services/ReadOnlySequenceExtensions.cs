using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace FileReader.Cli.FileReaders.Services;

public static class ReadOnlySequenceExtensions
{
    private static readonly byte[] _newlineDelimiter = Encoding.ASCII.GetBytes("\r\n");
    //private static readonly byte _carret = Encoding.ASCII.GetBytes("\r")[0];
    //private static readonly byte _return = Encoding.ASCII.GetBytes("\n")[0];
    private static readonly byte[] _emptyLineDelimiter = Encoding.ASCII.GetBytes(" ");
    //private static readonly byte[] _colonDelimiter = Encoding.ASCII.GetBytes(":");
    //private static readonly byte[] _commaDelimiter = Encoding.ASCII.GetBytes(",");
    private static readonly byte[] _quoteDelimiter = Encoding.ASCII.GetBytes("\"");

    public static void ReadAndSkipNumberOfLines(ref this SequenceReader<byte> reader, int numberOfLines)
    {
        if (reader.End)
            return;

        for (var i = 0; i < numberOfLines; i++)
        {
            if (!reader.TryReadTo(out ReadOnlySpan<byte> _, _newlineDelimiter, advancePastDelimiter: true))
                throw new InvalidOperationException("Could not read row from sequence");
        }
    }

    public static void Print(this in ReadOnlySpan<byte> span)
    {
        var str = Encoding.ASCII.GetString(span);
        Debug.WriteLine(str);
    }

    public static ReadOnlySpan<byte> GetValueBetween(in this ReadOnlySpan<byte> span, byte[] fromChar, byte[] toChar)
    {
        return span.Slice(span.IndexOf(fromChar) + 1, span.LastIndexOf(toChar) - span.IndexOf(fromChar));
    }

    public static ReadOnlySpan<byte> TrimStartEnd(in this ReadOnlySpan<byte> span)
    {
        return span.TrimStart(_emptyLineDelimiter).TrimStart(_quoteDelimiter).TrimEnd(_quoteDelimiter);
    }

    public static string ToStringValue(in this ReadOnlySpan<byte> span)
    {
        return Encoding.ASCII.GetString(span);
    }

    public static string ToStringValueUtf(in this ReadOnlySpan<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }

    public static string ToStringValueUtf(in this ReadOnlySequence<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }

    public static ReadOnlySequence<byte> Read(ref this SequenceReader<byte> reader, byte[] delimiter)
    {
        var res = reader.TryReadTo(out ReadOnlySequence<byte> ros, delimiter, advancePastDelimiter: true);

        if (!res)
        {
            var text = reader.UnreadSpan.ToStringValueUtf();
            Debug.WriteLine($"Could not read sequence with delimiter: {text}");

            if (!reader.End)
                reader.Advance(1);

            return ReadOnlySequence<byte>.Empty;
            //throw new InvalidOperationException("Could not read sequence with delimiter");
        }

        return ros;
    }

    public static ReadOnlySequence<byte> ReadOnlySeq(ref this SequenceReader<byte> reader, byte delimiter)
    {
        if (reader.CurrentSpanIndex < reader.CurrentSpan.Length && reader.CurrentSpan[reader.CurrentSpanIndex] == '"')
        {
            //Debug.WriteLine($"Unread segment: {reader.UnreadSequence.ToStringValueUtf()}");

            reader.Advance(1);

            if (reader.TryReadTo(out ReadOnlySequence<byte> segment, _quoteDelimiter, advancePastDelimiter: true))
            {
                //Debug.WriteLine($"Read segment: {segment.ToStringValueUtf()}");

                // We must advance past the ";"
                while (true)
                {
                    reader.TryPeek(out var nextChar);
                    if (nextChar == ';')
                        break;
                    else
                        reader.Advance(1);
                }

                reader.Advance(1); // Advance past the ";"

                return segment;
            }

            return ReadOnlySequence<byte>.Empty;
        }
        else
        {
            var res = reader.TryReadTo(out ReadOnlySequence<byte> ros, delimiter, advancePastDelimiter: true);

            if (!res)
            {
                // Read the rest of the unread span
                var remaining = reader.UnreadSequence;
                reader.Advance(remaining.Length);
                return remaining;
            }

            return ros;
        }
    }

    public static byte[] Read(ref this SequenceReader<byte> reader, byte delimiter)
    {
        if (reader.CurrentSpanIndex < reader.CurrentSpan.Length && reader.CurrentSpan[reader.CurrentSpanIndex] == '"')
        {
            //Debug.WriteLine($"Unread segment: {reader.UnreadSequence.ToStringValueUtf()}");

            reader.Advance(1);

            if (reader.TryReadTo(out ReadOnlySequence<byte> segment, _quoteDelimiter, advancePastDelimiter: true))
            {
                //Debug.WriteLine($"Read segment: {segment.ToStringValueUtf()}");

                // We must advance past the ";"
                while (true)
                {
                    reader.TryPeek(out var nextChar);
                    if (nextChar == ';')
                        break;
                    else
                        reader.Advance(1);
                }

                reader.Advance(1); // Advance past the ";"

                return segment.ToArray();
            }

            return [];
        }
        else
        {
            var res = reader.TryReadTo(out ReadOnlySequence<byte> ros, delimiter, advancePastDelimiter: true);

            if (!res)
            {
                // Read the rest of the unread span
                var remaining = reader.UnreadSequence;
                reader.Advance(remaining.Length);
                return remaining.ToArray();
            }

            return ros.ToArray();
        }
    }
}
