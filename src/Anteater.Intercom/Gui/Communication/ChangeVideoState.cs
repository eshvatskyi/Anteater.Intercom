using Anteater.Pipe;

namespace Anteater.Intercom.Gui.Communication;

public record ChangeVideoState(bool IsPaused): ICommand;
