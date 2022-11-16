using System.Threading.Tasks;

namespace Anteater.Intercom.Services.ReversChannel;

public interface IDoorLockService
{
    Task<(bool Status, string Message)> UnlockDoorAsync();
}
