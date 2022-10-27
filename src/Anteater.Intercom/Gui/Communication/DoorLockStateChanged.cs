using Anteater.Pipe;

namespace Anteater.Intercom.Gui.Communication;

public record DoorLockStateChanged(bool IsLocked): IEvent;
