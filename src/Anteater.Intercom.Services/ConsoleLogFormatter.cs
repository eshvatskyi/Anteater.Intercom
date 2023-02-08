using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Anteater.Intercom.Services;

public sealed class ConsoleLogFormatter : ConsoleFormatter
{
    public ConsoleLogFormatter() : base(nameof(ConsoleLogFormatter)) { }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (message is null)
        {
            return;
        }

        textWriter.Write($"[MAUI] [{logEntry.LogLevel.ToString().ToUpper()}] ");

        if (!string.IsNullOrWhiteSpace(logEntry.Category))
        {
            textWriter.Write($"[{logEntry.Category}] ");
        }
        
        textWriter.WriteLine(message);
    }
}
