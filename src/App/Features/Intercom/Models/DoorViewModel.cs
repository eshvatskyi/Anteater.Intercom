using Anteater.Intercom.Core.ReversChannel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anteater.Intercom.Features.Intercom;

public partial class DoorViewModel : ObservableObject
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
