using Anteater.Intercom.Gui.Messages;
using Anteater.Intercom.Services.ReversChannel;
using CommunityToolkit.Mvvm.Messaging;

namespace Anteater.Intercom.Gui.Controls;

public partial class DoorLockButton : FlexLayout
{
    public static readonly BindableProperty IsDoorLockedProperty =
        BindableProperty.Create(nameof(IsDoorLocked), typeof(bool), typeof(DoorLockButton), true);

    private readonly IMessenger _messeger;
    private readonly IDoorLockService _doorLock;

    public DoorLockButton()
    {
        _messeger = App.Services.GetRequiredService<IMessenger>();
        _doorLock = App.Services.GetRequiredService<IDoorLockService>();

        InitializeComponent();
    }

    public bool IsDoorLocked
    {
        get => GetValue(IsDoorLockedProperty) as bool? ?? true;
        set => SetValue(IsDoorLockedProperty, value);
    }

    void OnPressed(object sender, EventArgs e)
    {
        if (IsDoorLocked)
        {
            IsDoorLocked = false;

            _ = Task.Run(async () =>
            {
                _messeger.Send(new DoorLockStateChanged(false));

                try
                {
                    await _doorLock.UnlockDoorAsync();
                }
                catch { }

                MainThread.BeginInvokeOnMainThread(delegate
                {
                    IsDoorLocked = true;
                });

                _messeger.Send(new DoorLockStateChanged(true));
            });
        }
    }
}
