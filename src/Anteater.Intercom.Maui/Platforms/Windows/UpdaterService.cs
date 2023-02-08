using Microsoft.Extensions.Hosting;
using Squirrel;

namespace Anteater.Intercom;

public class UpdaterService : BackgroundService
{
    static readonly TimeSpan CheckDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(CheckDelay, stoppingToken);

                using var mgr = new UpdateManager("\\\\10.0.1.100\\Shared\\AnteaterIntercom");

                if (mgr.IsInstalledApp)
                {
                    try
                    {
                        if (mgr.UpdateApp().GetAwaiter().GetResult() != null)
                        {
                            UpdateManager.RestartApp();
                        }
                    }
                    catch { }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }
}
