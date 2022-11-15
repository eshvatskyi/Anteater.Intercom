using System.IO;

namespace Anteater.Intercom.Services.Audio.Headers;

public class ExtFrameAudioHeader
{
    private static readonly int HeaderLength = 32;

    public int StartFlag { get; set; }

    public short Ver { get; set; }

    public short Leight { get; set; }

    public int Timestamp { get; set; }

    public int EndFlag { get; set; }

    public ExtFrameAudio FrameAudio { get; set; }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(StartFlag);
        writer.Write(Ver);
        writer.Write(Leight);
        writer.Write(FrameAudio.ToBytes());
        writer.Write(Timestamp);
        writer.Write(EndFlag);

        return stream.ToArray();
    }
}
