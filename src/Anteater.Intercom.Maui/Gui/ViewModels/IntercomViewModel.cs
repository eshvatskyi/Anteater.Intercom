using System.ComponentModel;
using Anteater.Intercom.Services.Events;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Gui.ViewModels;

public partial class IntercomViewModel : ObservableViewModelBase, IRecipient<AlarmEvent>
{
    private bool _isSettingsEnaled;
    private bool _isOverlayVisible;

    private CancellationTokenSource _hideOverlayCancellation;

    public IntercomViewModel(IMessenger messenger, PlayerViewModel playerViewModel, DoorViewModel doorViewModel, CallViewModel callViewModel, SettingsViewModel settingsViewModel)
    {
        messenger.RegisterAll(this);

        Player = playerViewModel;
        Door = doorViewModel;
        Call = callViewModel;
        Settings = settingsViewModel;

        ShowOverlay = new RelayCommand<bool?>((initializeTimers) =>
        {
            IsOverlayVisible = true;
            ApplyOverlayChanges(initializeTimers ?? false);
        });

        Door.PropertyChanged += OnDoorOrCallStateChanged;
        Call.PropertyChanged += OnDoorOrCallStateChanged;
        Settings.PropertyChanged += OnSettingsStateChanged;

        Player.IsSoundMuted(Settings.IsSoundMuted);

        IsSettingsEnaled = Door.IsLocked && !Call.IsStarted;
        IsOverlayVisible = true;
        ApplyOverlayChanges(true);
    }

    public PlayerViewModel Player { get; }

    public DoorViewModel Door { get; }

    public CallViewModel Call { get; }

    public SettingsViewModel Settings { get; }

    public bool IsSettingsEnaled
    {
        get => _isSettingsEnaled;
        set => SetProperty(ref _isSettingsEnaled, value);
    }

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set => SetProperty(ref _isOverlayVisible, value);
    }

    public IRelayCommand<bool?> ShowOverlay { get; }

    void OnDoorOrCallStateChanged(object sender, PropertyChangedEventArgs args)
    {
        Settings.MuteAlarm?.Execute(null);

        IsSettingsEnaled = Door.IsLocked && !Call.IsStarted;

        IsOverlayVisible = true;
        ApplyOverlayChanges();

        if (!IsSettingsEnaled)
        {
            _hideOverlayCancellation?.Cancel();

            Player.IsSoundMuted(false);
        }
        else
        {
            Player.IsSoundMuted(Settings.IsSoundMuted);
        }
    }

    void OnSettingsStateChanged(object sender, PropertyChangedEventArgs e)
    {
        IsOverlayVisible = true;
        ApplyOverlayChanges();

        if (Settings.IsAlarmActive)
        {
            _hideOverlayCancellation?.Cancel();
        }
    }

    private partial void ApplyOverlayChanges(bool initializeTimers = false);

    void IRecipient<AlarmEvent>.Receive(AlarmEvent message)
    {
        if (message.Status && message.Type == AlarmEvent.EventType.MotionDetection)
        {
            IsOverlayVisible = true;
            ApplyOverlayChanges();
        }
    }
}
