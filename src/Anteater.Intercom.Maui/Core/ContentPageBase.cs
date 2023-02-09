namespace Anteater.Intercom.Core;

public abstract class ContentPageBase : ContentPage
{
    protected abstract void Build();

    protected override void OnAppearing()
    {
        base.OnAppearing();

#if DEBUG
        HotReloadService.UpdateApplicationEvent += Refresh;
#endif
    }

    protected override void OnDisappearing()
    {
#if DEBUG
        HotReloadService.UpdateApplicationEvent -= Refresh;
#endif

        base.OnDisappearing();
    }

    void Refresh(Type[] obj) => MainThread.InvokeOnMainThreadAsync(Build);
}
