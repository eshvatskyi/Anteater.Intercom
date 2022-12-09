using System.IO;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class HvFrameHeader
{
    public static readonly int HeaderLength = 12;

    public short ZeroFlag { get; set; }

    public byte OneFlag { get; set; }

    public byte SteamFlag { get; set; }

    public int BufferSize { get; set; }

    public int Timestamp { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ZeroFlag);
        writer.Write(OneFlag);
        writer.Write(SteamFlag);
        writer.Write(BufferSize);
        writer.Write(Timestamp);
    }
}
