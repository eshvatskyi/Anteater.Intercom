using Anteater.Intercom.Services.Events;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams;

namespace Anteater.Intercom.Gui.ViewModels;

public partial class SettingsViewModel : ObservableViewModelBase, IRecipient<AlarmEvent>
{
    private WaveOut _waveOut;
    private CancellationTokenSource _alarmCancellation;

    partial void Init()
    {
        _waveOut = new WaveOut();

        _waveOut.Initialize(new LoopStream(CodecFactory.Instance
                .GetCodec("doorbell.wav")
                .ToSampleSource()
                .ToWaveSource()));

        _isSoundMuted = true;
        _isAlarmMuted = false;

        SwitchSoundState = new RelayCommand(() => IsSoundMuted = !IsSoundMuted);
        SwitchAlarmState = new RelayCommand(() =>
        {
            IsAlarmMuted = !IsAlarmMuted;

            try
            {
                _waveOut.Volume = IsAlarmMuted ? 0 : 1;
            }
            catch { }

        });
        MuteAlarm = new RelayCommand(() => _alarmCancellation?.Cancel());


        _messenger.Register(this);
    }

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.SensorAlarm)
        {
            IsAlarmActive = true;

            _alarmCancellation = new CancellationTokenSource();

            _ = Task.Delay(TimeSpan.FromSeconds(15), _alarmCancellation.Token).ContinueWith(_ =>
            {
                IsAlarmActive = false;

                _ = Task.Run(_waveOut.Stop);
            });

            _ = Task.Run(_waveOut.Play);
        }
    }
}
