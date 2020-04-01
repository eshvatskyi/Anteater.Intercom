namespace Alanta.Client.Media.Dsp
{
	public interface IDtxFilter: IAudioInplaceFilter
	{
		bool IsSilent { get; }
	}
}
