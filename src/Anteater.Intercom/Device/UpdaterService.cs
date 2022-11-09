using System;
using System.Threading;
using System.Threading.Tasks;
using Squirrel;

namespace Anteater.Intercom.Device;

public class UpdaterService : BackgroundService
{
    static readonly TimeSpan CheckDelay = TimeSpan.FromMinutes(5);

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(CheckDelay);

                using var mgr = new UpdateManager("\\\\10.0.1.100\\Shared\\AnteaterIntercom");

                if (mgr.IsInstalledApp)
                {
                    if (mgr.UpdateApp().GetAwaiter().GetResult() != null)
                    {
                        UpdateManager.RestartApp();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }
}
