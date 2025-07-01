namespace FileReader.Cli.Services;

public class BuffersService
{
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.AsSpan().CopyTo(combined);
        second.AsSpan().CopyTo(combined.AsSpan(first.Length));

        return combined;
    }
}
