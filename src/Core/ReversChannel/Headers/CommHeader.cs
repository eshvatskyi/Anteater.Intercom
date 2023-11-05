namespace Anteater.Intercom.Core.ReversChannel.Headers;

public class CommHeader
{
    public static readonly int HeaderLength = 28;

    public int Flag { get; set; }

    public int Command { get; set; }

    public int LogonID { get; set; }

    public int Priority { get; set; }

    public int ErrorCode { get; set; }

    public int BufferSize { get; set; }

    public int Misc { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Flag);
        writer.Write(Command);
        writer.Write(LogonID);
        writer.Write(Priority);
        writer.Write(Misc);
        writer.Write(ErrorCode);
        writer.Write(BufferSize);
    }

    public static async Task<CommHeader> ReadAsync(Stream stream)
    {
        var buffer = new byte[HeaderLength];

        await stream.ReadAsync(buffer);

        return new CommHeader
        {
            Flag = BitConverter.ToInt32(buffer, 0),
            Command = BitConverter.ToInt32(buffer, 4),
            LogonID = BitConverter.ToInt32(buffer, 8),
            Priority = BitConverter.ToInt32(buffer, 12),
            Misc = BitConverter.ToInt32(buffer, 16),
            ErrorCode = BitConverter.ToInt32(buffer, 20),
            BufferSize = BitConverter.ToInt32(buffer, 24),
        };
    }
}
