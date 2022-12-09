using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;

namespace Anteater.Intercom.Services.ReversChannel;

public interface IReversAudioService
{
    bool IsOpen { get; }

    Task ConnectAsync(AVSampleFormat format, int sampleRate, int channels);

    Task SendAsync(byte[] data);

    void Disconnect();
}
