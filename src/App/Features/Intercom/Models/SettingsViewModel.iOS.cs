using CommunityToolkit.Mvvm.Input;

namespace Anteater.Intercom.Features.Intercom;

public partial class SettingsViewModel
{
    partial void Init()
    {
        _isSoundMuted = false;
        _isAlarmMuted = true;

        SwitchSoundState = new RelayCommand(() => { });
        SwitchAlarmState = new RelayCommand(() => { });
        MuteAlarm = new RelayCommand(() => { });
    }
}
