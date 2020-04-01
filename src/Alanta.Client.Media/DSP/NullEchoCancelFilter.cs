using System;

namespace Alanta.Client.Media.Dsp
{
	public class NullEchoCancelFilter : EchoCancelFilter
	{
		public NullEchoCancelFilter(
			int systemLatency, 
			int filterLength, 
			AudioFormat recordedAudioFormat, 
			AudioFormat playedAudioFormat, 
			IAudioFilter playedResampler = null, 
			IAudioFilter recordedResampler = null) : 
			base(systemLatency, filterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
		{
		}

		protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
		{
			Buffer.BlockCopy(recorded, 0, outFrame, 0, outFrame.Length * sizeof(short));
		}
	}
}
