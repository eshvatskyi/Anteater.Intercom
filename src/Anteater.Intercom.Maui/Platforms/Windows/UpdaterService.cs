using Cronos;
using Microsoft.Extensions.Hosting;
using Squirrel;

namespace Anteater.Intercom;

public class UpdaterService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            StartUpdaterTimerAsync(stoppingToken),
            StartRestartTimerAsync(stoppingToken)
        );
    }

    async Task StartUpdaterTimerAsync(CancellationToken stoppingToken)
    {
        var timer = new CronosPeriodicTimer("*/5 * * * *");

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var mgr = new UpdateManager("\\\\10.0.1.100\\Shared\\AnteaterIntercom");

                if (mgr.IsInstalledApp)
                {
                    if (await mgr.UpdateApp() != null)
                    {
                        UpdateManager.RestartApp();
                    }
                }
            }
            catch { }
        }
    }

    async Task StartRestartTimerAsync(CancellationToken stoppingToken)
    {
        var timer = new CronosPeriodicTimer("0 6,18 * * *");

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                UpdateManager.RestartApp();
            }
            catch { }
        }
    }
}
