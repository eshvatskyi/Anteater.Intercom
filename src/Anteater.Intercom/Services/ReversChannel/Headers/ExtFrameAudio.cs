using System.IO;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class ExtFrameAudio
{
    public static readonly int HeaderLength = 16;

    public short AudioEncodeType { get; set; }

    public short AudioChannels { get; set; }

    public short AudioBits { get; set; }

    public int AudioSamples { get; set; }

    public int AudioBitrate { get; set; }

    public string Reserve { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(AudioEncodeType);
        writer.Write(AudioChannels);
        writer.Write(AudioBits);
        writer.WriteString(Reserve ?? "", 2);
        writer.Write(AudioSamples);
        writer.Write(AudioBitrate);
    }
}
