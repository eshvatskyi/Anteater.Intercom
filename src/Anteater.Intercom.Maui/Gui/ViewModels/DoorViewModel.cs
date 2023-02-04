using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Input;

namespace Anteater.Intercom.Gui.ViewModels;

public partial class DoorViewModel : ObservableViewModelBase
{
    private readonly IDoorLockService _doorLock;

    private bool _isLocked = true;

    public DoorViewModel(IDoorLockService doorLock)
    {
        _doorLock = doorLock;

        Unlock = new RelayCommand(UnlockCommand);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    public IRelayCommand Unlock { get; }

    void UnlockCommand()
    {
        _ = Task.Run(async () =>
        {
            if (IsLocked)
            {
                IsLocked = false;

                try
                {
                    await _doorLock.UnlockDoorAsync();
                }
                catch { }

                IsLocked = true;
            }
        });
    }
}
