using Anteater.Pipe;

namespace Anteater.Intercom.Gui.Communication;

public record AlarmStateChanged(bool IsMuted): IEvent;
