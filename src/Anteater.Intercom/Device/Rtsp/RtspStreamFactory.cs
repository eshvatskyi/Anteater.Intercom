using System;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Device.Rtsp;

public class RtspStreamFactory
{
    public static unsafe RtspStream Create(AVStream* stream, Action<byte[]> callback)
    {
        RtspStream rtspStream = stream->codecpar->codec_type switch
        {
            AVMediaType.AVMEDIA_TYPE_VIDEO => new RtspStreamVideo(stream),
            AVMediaType.AVMEDIA_TYPE_AUDIO => new RtspStreamAudio(stream),
            _ => new RtspStreamUnknown(),
        };

        rtspStream.FrameDecoded += (_, data) => callback?.Invoke(data);


        return rtspStream;
    }
}
