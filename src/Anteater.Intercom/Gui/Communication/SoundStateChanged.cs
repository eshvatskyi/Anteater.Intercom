using Anteater.Pipe;

namespace Anteater.Intercom.Gui.Communication;

public record SoundStateChanged(bool IsMuted): IEvent;
