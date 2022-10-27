using FFmpeg.AutoGen;

namespace Anteater.Intercom.Device.Rtsp;

public record RtspStreamFormat(RtspStreamVideoFormat Video, RtspStreamAudioFormat Audio);

public record RtspStreamVideoFormat(int Width, int Height, AVPixelFormat Format);

public record RtspStreamAudioFormat(int SampleRate, int Channels);

