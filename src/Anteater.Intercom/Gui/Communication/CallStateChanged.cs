using Anteater.Pipe;

namespace Anteater.Intercom.Gui.Communication
{
    public record CallStateChanged(bool IsCalling): IEvent;
}
