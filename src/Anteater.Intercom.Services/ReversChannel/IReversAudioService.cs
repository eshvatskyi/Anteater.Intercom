using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.ReversChannel;

public interface IReversAudioService
{
    bool IsOpen { get; }

    Task ConnectAsync();

    Task SendAsync(AVSampleFormat format, int sampleRate, int channels, byte[] data);

    void Disconnect();
}
