using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Core.ReversChannel;

public interface IReversAudioService
{
    ValueTask ConnectAsync();

    Task SendAsync(AVSampleFormat format, int sampleRate, int channels, byte[] data);

    void Disconnect();
}
