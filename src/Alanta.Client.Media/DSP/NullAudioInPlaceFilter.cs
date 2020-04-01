namespace Alanta.Client.Media.Dsp
{
	public class NullAudioInplaceFilter : IDtxFilter
	{

		public void Filter(short[] sampleData)
		{

		}

		public string InstanceName { get; set; }

		public bool IsSilent { get; set; }
	}
}
