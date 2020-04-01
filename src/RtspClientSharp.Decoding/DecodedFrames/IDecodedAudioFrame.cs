using System;

namespace RtspClientSharp.Decoding.DecodedFrames
{
    public interface IDecodedAudioFrame
    {
        DateTime Timestamp { get; }
        ArraySegment<byte> DecodedBytes { get; }
        AudioFrameFormat Format { get; }
    }
}
