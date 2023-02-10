using Anteater.Intercom.Core;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Anteater.Intercom.Features.Intercom;

public partial class SettingsViewModel : ObservableViewModelBase, IRecipient<AlarmEvent>
{
    private AudioFileReader _reader;
    private WaveOut _waveOut;
    private CancellationTokenSource _alarmCancellation;

    partial void Init()
    {
        _reader = new AudioFileReader("doorbell.wav");

        _waveOut = new WaveOut();
        _waveOut.Init(new WaveToSampleProvider(_reader));

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

                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
            });

            _reader.Position = 0;
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Play();
        }
    }

    void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        _reader.Position = 0;
        _waveOut.Play();
    }
}
