namespace Anteater.Intercom.Gui.ViewModels;

public partial class IntercomViewModel
{
    private Task _hideOverlayTask = Task.CompletedTask;

    private partial void ApplyOverlayChanges(bool initializeTimers)
    {
        _hideOverlayCancellation?.Cancel();
        _hideOverlayCancellation = new CancellationTokenSource();

        if (IsOverlayVisible)
        {
            if (initializeTimers == false)
            {
                _hideOverlayTask = _hideOverlayTask.ContinueWith(_ =>
                {
                    if (!Player.IsConnected)
                    {
                        Player.Connect();
                    }
                });
            }

            _ = Task.Delay(TimeSpan.FromSeconds(15), _hideOverlayCancellation.Token).ContinueWith(delegate
            {
                _hideOverlayCancellation = null;
                IsOverlayVisible = false;
                ApplyOverlayChanges();
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        else
        {
            _ = Task.Delay(TimeSpan.FromSeconds(30), _hideOverlayCancellation.Token).ContinueWith(delegate
            {
                _hideOverlayCancellation = null;
                _hideOverlayTask = _hideOverlayTask.ContinueWith(_ => Player.Stop());
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
