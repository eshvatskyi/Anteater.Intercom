using System.IO;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class ExtFrameAudioHeader
{
    public static readonly int HeaderLength = 32;

    public int StartFlag { get; set; }

    public short Ver { get; set; }

    public short Leight { get; set; }

    public int Timestamp { get; set; }

    public int EndFlag { get; set; }

    public ExtFrameAudio FrameAudio { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(StartFlag);
        writer.Write(Ver);
        writer.Write(Leight);
        FrameAudio.Write(writer);
        writer.Write(Timestamp);
        writer.Write(EndFlag);
    }
}
