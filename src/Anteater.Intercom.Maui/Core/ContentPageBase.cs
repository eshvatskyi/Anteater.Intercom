namespace Anteater.Intercom.Core;

public abstract class ContentPageBase : ContentPage
{
    protected abstract void Build();

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        Build();

#if DEBUG
        HotReloadService.UpdateApplicationEvent += Refresh;
#endif
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);

#if DEBUG
        HotReloadService.UpdateApplicationEvent -= Refresh;
#endif
    }

    void Refresh(Type[] obj) => MainThread.InvokeOnMainThreadAsync(Build);
}
