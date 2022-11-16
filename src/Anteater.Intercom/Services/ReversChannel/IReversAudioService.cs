using System.Threading.Tasks;

namespace Anteater.Intercom.Services.ReversChannel;

public interface IReversAudioService
{
    bool IsOpen { get; }

    int AudioSamples { get; }

    int AudioChannels { get; }

    int AudioBits { get; }

    Task ConnectAsync();

    Task SendAsync(short[] data);

    void Disconnect();
}
