namespace Anteater.Intercom.Core.ReversChannel;

public interface IDoorLockService
{
    Task<(bool Status, string Message)> UnlockDoorAsync();
}
