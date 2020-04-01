using System;
using System.Collections.Generic;

namespace Alanta.Client.Media.Dsp
{
	/// <summary>
	/// A workable base class for a wide range of echo cancellation algorithms.
	/// Includes a queue for buffering frames to account for extra latency incurred when playing audio.
	/// </summary>
	/// <remarks>Assumes a 16-bit (short) sample. Also, for performance reasons, 
	/// the individual methods of the class are NOT threadsafe. 
	/// Only call a given method on a given instance from one thread at a time.</remarks>
	public abstract class EchoCancelFilter : IAudioTwoWayFilter
	{
		#region Constructors

		/// <summary>
		/// Initializes the echo canceller.
		/// </summary>
		/// <param name="systemLatency">The amount of latency that the operating environment adds (in milliseconds). 
		/// Determines how long a played frame is held before being submitted to the echo canceller.
		/// For Silverlight v4, this is typically ~150ms.</param>
		/// <param name="filterLength">The length of the echo cancellation filter in milliseconds (typically ~150).</param>
		/// <param name="recordedAudioFormat">The format of the recorded audio</param>
		/// <param name="playedAudioFormat">The format of the played audio</param>
		/// <param name="playedResampler">An instance of an IAudioFilter&lt;short&gt; which can be used to resample or synchronize played frames.</param>
		/// <param name="recordedResampler">An instance of an IAudioFilter&lt;short&gt; which can be used to resample or synchronize played frames.</param>
		protected EchoCancelFilter(int systemLatency, int filterLength, AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat, IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null)
		{
			_recordedAudioFormat = recordedAudioFormat;
			_playedAudioFormat = playedAudioFormat;

			// We need to resample the audio we play (typically 16Khz) so that it matches the audio we 
			// get from the AudioSinkAdapter (sometimes 16Khz, but often 8Khz); otherwise, the echo cancellation 
			// wouldn't work.
			if (playedResampler == null)
			{
				playedResampler = new ResampleFilter(playedAudioFormat, recordedAudioFormat);
				playedResampler.InstanceName = "EchoCanceller_PlayedResampler";
			}

			// We don't typically need to resample the audio we get from the AudioSinkAdapter, but
			// this is here for historical reasons, as we have at times in the past tried to experiment with
			// synchronizing the played and the recorded streams, to account for differences in clock speed.
			// In general, that didn't seem to work, but I like the architectural ability to specify 
			// a resampler here, so I've kept it in the pipeline.
			if (recordedResampler == null)
			{
				recordedResampler = new NullAudioFilter(recordedAudioFormat.SamplesPerFrame * sizeof(short));
				recordedResampler.InstanceName = "EchoCanceller_RecordedResampler";
			}

			SystemLatency = systemLatency;
			FilterLength = filterLength * (recordedAudioFormat.SamplesPerSecond / 1000);
			SamplesPerFrame = recordedAudioFormat.SamplesPerFrame;
			SamplesPerSecond = recordedAudioFormat.SamplesPerSecond;
			_recorded = new short[SamplesPerFrame];

			// Configure the latency queue.
			QueueSize = Math.Max(systemLatency / recordedAudioFormat.MillisecondsPerFrame, 1);
			_maxQueueSize = QueueSize + 1;
			_playedQueue = new Queue<short[]>();

			_playedResampler = playedResampler;
			_recordedResampler = recordedResampler;

		}
		#endregion

		#region Fields and Properties
		private readonly Queue<short[]> _playedQueue;
		private bool _queueTargetReached;
		private readonly IAudioFilter _playedResampler;
		private readonly IAudioFilter _recordedResampler;
		private readonly short[] _recorded;
		protected readonly AudioFormat _recordedAudioFormat;
		protected readonly AudioFormat _playedAudioFormat;

		public int SystemLatency { get; private set; }
		public int FilterLength { get; private set; }
		public int SamplesPerFrame { get; private set; }
		public int SamplesPerSecond { get; private set; }
		public int QueueSize { get; private set; }
		private readonly int _maxQueueSize;

		/// <summary>
		/// Helps in debugging.
		/// </summary>
		public string InstanceName { get; set; }
		#endregion

		#region Methods

		public override string ToString()
		{
			return GetType().Name + " " + InstanceName + 
				":: SystemLatency:" + SystemLatency + 
				"; FilterLength:" + FilterLength + 
				"; RecordedAudioFormat:" + _recordedAudioFormat + 
				"; PlayedAudioFormat:" + _playedAudioFormat +
				"; PlayedResampler:" + _playedResampler;
		}

		/// <summary>
		/// Record that a frame was submitted to the speakers.
		/// </summary>
		/// <param name="speakerSample">The ByteStream containing the data submitted to the speakers.</param>
		public void RegisterFramePlayed(byte[] speakerSample)
		{
			lock (_playedQueue)
			{
				// If we have too many frames in the queue, discard the oldest.
				while (_playedQueue.Count > _maxQueueSize)
				{
					_playedQueue.Dequeue();
				}

				// Resample the frame in case the frames have been coming in at the wrong rate.
				_playedResampler.Write(speakerSample);
				var frame = new short[SamplesPerFrame];
				bool moreFrames;
				do
				{
					if (_playedResampler.Read(frame, out moreFrames))
					{
						_playedQueue.Enqueue(frame);
						if (!_queueTargetReached && _playedQueue.Count >= QueueSize)
						{
							_queueTargetReached = true;
						}
					}
				} while (moreFrames);
			}
		}

		/// <summary>
		/// Perform echo cancellation on a frame recorded from the local microphone.
		/// </summary>
		/// <param name="recordedData">The byte array containing the data recorded from the local microphone.</param>
		public virtual void Write(byte[] recordedData)
		{
			// Resample the incoming microphone sample onto a new buffer (to adjust for slight differences in timing on different sound cards).
			_recordedResampler.Write(recordedData);
		}

		public virtual bool Read(Array outBuffer, out bool moreFrames)
		{
			// FIXME: We're moving the data around twice, which is unnecessary. When I get it working,
			// I need to come back and get rid of one of the Buffer.BlockCopy() moves.

			// Dequeue the audio submitted to the speakers ~12 frames back.
			if (_recordedResampler.Read(_recorded, out moreFrames))
			{
				// If we successfully retrieved a buffered recorded frame, then try to get one of the buffered played frames.
				short[] played;
				lock (_playedQueue)
				{
					// Don't cancel anything if we haven't buffered enough packets yet.
					if (!_queueTargetReached || _playedQueue.Count == 0)
					{
						_queueTargetReached = false;
						Buffer.BlockCopy(_recorded, 0, outBuffer, 0, _recorded.Length * sizeof(short));
						return true;
					}
					played = _playedQueue.Dequeue();
				}

				// If we have both a recorded and a played frame, let's echo cancel those babies.
				PerformEchoCancellation(_recorded, played, (short[])outBuffer);				
			}
			else
			{
				return false;
			}
			return true;
		}

		protected abstract void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame);
		#endregion

	}
}
