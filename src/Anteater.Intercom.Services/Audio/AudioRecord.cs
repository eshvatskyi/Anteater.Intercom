using Microsoft.Extensions.Logging;

namespace Anteater.Intercom.Services.Audio;

public partial class AudioRecord : IAudioRecord
{
    private readonly ILogger<AudioRecord> _logger;

    public AudioRecord(ILoggerFactory logger)
    {
        _logger = logger.CreateLogger<AudioRecord>();

        Init();
    }

    public event IAudioRecord.DataAvailableEventHandler DataAvailable;

    public event IAudioRecord.StoppedEventHandler Stopped;

    private partial void Init();

    public partial void Start();

    public partial void Stop();
}
