using System.Threading;
using System.Threading.Tasks;

namespace Anteater.Intercom.Device
{
    public abstract class BackgroundService
    {
        private readonly CancellationTokenSource _cts;

        private Task _runningTask;

        public BackgroundService()
        {
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            _runningTask = RunAsync(_cts.Token);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            await _runningTask;
        }

        protected abstract Task RunAsync(CancellationToken cancellationToken);
    }
}
