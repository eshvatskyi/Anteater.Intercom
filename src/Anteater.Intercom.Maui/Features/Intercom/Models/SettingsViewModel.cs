using Anteater.Intercom.Core;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Features.Intercom;

public partial class SettingsViewModel : ObservableViewModelBase
{
    private readonly IMessenger _messenger;

    private bool _isAlarmActive = false;
    private bool _isSoundMuted = true;
    private bool _isAlarmMuted = false;

    public SettingsViewModel(IMessenger messenger)
    {
        _messenger = messenger;

        Init();        
    }

    partial void Init();

    public bool IsAlarmActive
    {
        get => _isAlarmActive;
        set => SetProperty(ref _isAlarmActive, value);
    }

    public bool IsSoundMuted
    {
        get => _isSoundMuted;
        set => SetProperty(ref _isSoundMuted, value);
    }

    public bool IsAlarmMuted
    {
        get => _isAlarmMuted;
        set => SetProperty(ref _isAlarmMuted, value);
    }

    public IRelayCommand SwitchSoundState { get; private set; }

    public IRelayCommand SwitchAlarmState { get; private set; }

    public IRelayCommand MuteAlarm { get; private set; }
}
