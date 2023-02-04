using CommunityToolkit.Mvvm.Input;

namespace Anteater.Intercom.Gui.ViewModels;

public partial class SettingsViewModel : ObservableViewModelBase
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
