using Cronos;

namespace Anteater.Intercom;

public sealed class CronosPeriodicTimer : IDisposable
{
    private static readonly TimeSpan MinDelay = TimeSpan.FromMilliseconds(500);

    private readonly CronExpression _cronExpression;
    private PeriodicTimer _timer;
    private bool _disposed;

    public CronosPeriodicTimer(string expression, CronFormat format = CronFormat.Standard)
    {
        _cronExpression = CronExpression.Parse(expression, format);
    }

    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PeriodicTimer timer;

        lock (_cronExpression)
        {
            if (_disposed)
            {
                return false;
            }

            if (_timer is not null)
            {
                throw new InvalidOperationException("One consumer at a time.");
            }

            var utcNow = DateTime.UtcNow;
            var utcNext = _cronExpression.GetNextOccurrence(utcNow + MinDelay);
            if (utcNext is null)
            {
                throw new InvalidOperationException("Unreachable date.");
            }

            var delay = utcNext.Value - utcNow;

            timer = _timer = new(delay);
        }
        try
        {
            using (timer)
            {
                return await timer.WaitForNextTickAsync(cancellationToken);
            }
        }
        finally
        {
            Volatile.Write(ref _timer, null);
        }
    }

    public void Dispose()
    {
        lock (_cronExpression)
        {
            if (!_disposed)
            {
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}
