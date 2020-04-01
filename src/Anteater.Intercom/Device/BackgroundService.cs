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
            _runningTask = Task.Factory.StartNew(async () => await RunAsync(_cts.Token).ConfigureAwait(false)).Unwrap();
        }

        public void Stop()
        {
            _cts.Cancel();

            _runningTask.GetAwaiter().GetResult();
        }

        protected abstract Task RunAsync(CancellationToken cancellationToken);
    }
}
