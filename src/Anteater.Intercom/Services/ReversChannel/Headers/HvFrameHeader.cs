using System.IO;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class HvFrameHeader
{
    private static readonly int HeaderLength = 12;

    public short ZeroFlag { get; set; }

    public byte OneFlag { get; set; }

    public byte SteamFlag { get; set; }

    public int BufferSize { get; set; }

    public int Timestamp { get; set; }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(ZeroFlag);
        writer.Write(OneFlag);
        writer.Write(SteamFlag);
        writer.Write(BufferSize);
        writer.Write(Timestamp);

        return stream.ToArray();
    }
}
