using System.IO;
using System.Text;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class ExtFrameAudio
{
    private static readonly int HeaderLength = 16;

    public short AudioEncodeType { get; set; }

    public short AudioChannels { get; set; }

    public short AudioBits { get; set; }

    public int AudioSamples { get; set; }

    public int AudioBitrate { get; set; }

    public string Reserve { get; set; }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(AudioEncodeType);
        writer.Write(AudioChannels);
        writer.Write(AudioBits);
        writer.Write(StringToBytes(Reserve ?? string.Empty, 2));
        writer.Write(AudioSamples);
        writer.Write(AudioBitrate);

        return stream.ToArray();
    }

    static byte[] StringToBytes(string value, int length)
    {
        var buffer = new byte[length];

        Encoding.UTF8.GetBytes(value).CopyTo(buffer, 0);

        return buffer;
    }
}
