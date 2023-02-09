namespace Anteater.Intercom.Messages;

public record WindowStateChanged(WindowState State);

public enum WindowState
{
    Stopped,
    Resumed,
    Closing
}
