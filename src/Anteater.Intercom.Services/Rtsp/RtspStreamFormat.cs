using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.Rtsp;

public record RtspStreamFormat(RtspStreamVideoFormat Video, RtspStreamAudioFormat Audio);

public record RtspStreamVideoFormat(int Width, int Height, AVPixelFormat Format);

public record RtspStreamAudioFormat(int SampleRate, int Channels);
