using System;

namespace Alanta.Client.Media.Dsp.WebRtc
{
	public class WebRtcFilter : EchoCancelFilter
	{
		private readonly AecCore _aec;
		private readonly NoiseSuppressor _ns;
		private readonly HighPassFilter _highPassFilter = new HighPassFilter();
		private readonly bool _enableAec;
		private readonly bool _enableDenoise;
		private readonly bool _enableAgc;
		private readonly Agc _agc;

		/// <summary>
		/// Constructs an instance of the WebRtcFilter.
		/// </summary>
		/// <param name="expectedAudioLatency">The expected audio latency in milliseconds</param>
		/// <param name="filterLength">The length of the echo cancellation filter in milliseconds</param>
		/// <param name="recordedAudioFormat">The audio format into which the recorded audio has been transformed prior to encoding (e.g., not the raw audio)</param>
		/// <param name="playedAudioFormat">The audio format which the audio must be transformed after decoding and before playing</param>
		/// <param name="enableAec">Whether to enable acoustic echo cancellation</param>
		/// <param name="enableDenoise">Whether to enable denoising</param>
		/// <param name="enableAgc">Whether to enable automatic gain control</param>
		/// <param name="playedResampler">The resampler which should be used on played audio</param>
		/// <param name="recordedResampler">The resampler which should be used on recorded audio</param>
		public WebRtcFilter(int expectedAudioLatency, int filterLength,
			AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat,
			bool enableAec, bool enableDenoise, bool enableAgc,
			IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null) :
			base(expectedAudioLatency, filterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
		{
			// Default settings.
			var aecConfig = new AecConfig(FilterLength, recordedAudioFormat.SamplesPerFrame, recordedAudioFormat.SamplesPerSecond)
			{
				NlpMode = AecNlpMode.KAecNlpModerate,
				SkewMode = false,
				MetricsMode = false
			};
			_ns = new NoiseSuppressor(recordedAudioFormat);
			_aec = new AecCore(aecConfig);

			if (aecConfig.NlpMode != AecNlpMode.KAecNlpConservative &&
				aecConfig.NlpMode != AecNlpMode.KAecNlpModerate &&
				aecConfig.NlpMode != AecNlpMode.KAecNlpAggressive)
			{
				throw new ArgumentException();
			}

			_aec.targetSupp = WebRtcConstants.targetSupp[(int)aecConfig.NlpMode];
			_aec.minOverDrive = WebRtcConstants.minOverDrive[(int)aecConfig.NlpMode];

			if (aecConfig.MetricsMode && aecConfig.MetricsMode != true)
			{
				throw new ArgumentException();
			}
			_aec.metricsMode = aecConfig.MetricsMode;
			if (_aec.metricsMode)
			{
				_aec.InitMetrics();
			}
			_enableAec = enableAec;
			_enableDenoise = enableDenoise;
			_enableAgc = enableAgc;

			_agc = new Agc(0, 255, Agc.AgcMode.AgcModeAdaptiveDigital, (uint)recordedAudioFormat.SamplesPerSecond);
		}

		protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
		{

			// ks 11/2/11 - This seems to be more-or-less the order in which things are processed in the WebRtc audio_processing_impl.cc file.
			_highPassFilter.Filter(recorded);

			if (_enableAgc)
			{
				_agc.WebRtcAgc_AddFarend(played, (short)played.Length);
				gain_control_AnalyzeCaptureAudio(recorded);
			}

			if (_enableAec)
			{
				_aec.ProcessFrame(recorded, played, outFrame, 0);
			}
			else
			{
				Buffer.BlockCopy(recorded, 0, outFrame, 0, SamplesPerFrame * sizeof(short));
			}

			if (_enableDenoise)
			{
				// ks 11/14/11 - The noise suppressor only supports 10 ms blocks. I might be able to fix that,
				// but this is easier for now.
				_ns.ProcessFrame(outFrame, 0, outFrame, 0);
				_ns.ProcessFrame(outFrame, _recordedAudioFormat.SamplesPer10Ms, outFrame, _recordedAudioFormat.SamplesPer10Ms);
			}

			if (_enableAgc)
			{
				gain_control_ProcessCaptureAudio(outFrame);
			}
		}

		void gain_control_AnalyzeCaptureAudio(short[] data)
		{
			const int analogCaptureLevel = 127;
			//WriteDebugMessage(String.Format("(C#) AGC 503   analog_capture_level_ = {0}", analog_capture_level_));
			int v;
			//WriteDebugMessage(String.Format("(C#) AGC 03001     data[143] = {0}", data[143]));//todo
			_agc.WebRtcAgc_VirtualMic(data, null, (short)data.Length, analogCaptureLevel, out v);
			_captureLevels0 = v;
			//micLevelIn = v;
		}

		int _captureLevels0 = 127;
		void gain_control_ProcessCaptureAudio(short[] data)
		{
			bool saturationWarning;
			int captureLevelOut;
			// Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC capture_levels_0", (double)capture_levels_0, 1);
			_agc.WebRtcAgc_Process(data, null, (short)data.Length, data, null, _captureLevels0, out captureLevelOut, 0, out saturationWarning);

			// Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC capture_level_out", (double)capture_level_out, 1);
			_captureLevels0 = captureLevelOut;
		}
	}
}