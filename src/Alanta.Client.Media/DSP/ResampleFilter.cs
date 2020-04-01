using System;

namespace Alanta.Client.Media.Dsp
{
    /// <summary>
    /// The Resampler class resamples inbound audio data from one format (e.g., 24-bit) to another (e.g., 16-bit).
    /// </summary>
    public class ResampleFilter : IAudioFilter
    {
        #region Constructors and Initializers

        /// <summary>
        /// Creates a new instance of the ResampleFilter class.
        /// </summary>
        public ResampleFilter(AudioFormat input, AudioFormat output)
        {
            if (output.Channels < 1 || output.Channels > 2)
            {
                throw new ArgumentException("At the moment, we only support resampling to single-channel audio");
            }
            if (input.Channels < 1 || input.Channels > 2)
            {
                throw new ArgumentException("The number of channels on the input audio format was incorrect");
            }
            _inputFormat = input;
            _outputFormat = output;
            InputBytesPerSample = input.BytesPerSample;
            InputSamplesPerSecond = input.SamplesPerSecond;
            InputChannels = input.Channels;
            OutputBytesPerSample = output.BytesPerSample;
            OutputSamplesPerSecond = output.SamplesPerSecond;
            OutputChannels = output.Channels;
            OutputBytesPerFrame = output.BytesPerFrame;
            OutputMillisecondsPerFrame = output.MillisecondsPerFrame;
            CorrectionFactor = 1.0;

            _scaledBuffer = new byte[output.BytesPerFrame * 100]; // Leave room for buffering 100 outbound frames.
            _scalingFactor = input.SamplesPerSecond * input.Channels / (double)(output.SamplesPerSecond * output.Channels);
        }

        #endregion

        #region Fields and Properties

        private readonly AudioFormat _inputFormat;
        private readonly AudioFormat _outputFormat;
        private readonly byte[] _scaledBuffer; // Leave room for ~100 samples.
        private readonly double _scalingFactor;
        private string _instanceName;
        private int _sampleBufferReadPosition;
        private int _sampleBufferWritePosition;
        public int InputBytesPerSample { get; private set; }
        public int InputSamplesPerSecond { get; private set; }
        public int InputChannels { get; private set; }
        public int OutputBytesPerSample { get; private set; }
        public int OutputSamplesPerSecond { get; private set; }
        public int OutputChannels { get; private set; }
        public int OutputBytesPerFrame { get; private set; }
        public int OutputMillisecondsPerFrame { get; private set; }

        public int UnreadBytes
        {
            get { return _sampleBufferWritePosition - _sampleBufferReadPosition; }
        }

        /// <summary>
        /// The ratio of expected frames vs. actual frames
        /// </summary>
        public double CorrectionFactor { get; protected set; }

        public string InstanceName
        {
            get { return _instanceName; }
            set
            {
                _instanceName = value;
            }
        }

        #endregion

        #region Public methods

        public override string ToString()
        {
            return "ResampleFilter " + InstanceName + ":: InputFormat: " + _inputFormat + "; OutputFormat: " + _outputFormat;
        }

        public virtual void Write(byte[] sampleData)
        {
            int scaledLength = GetScaledLength(sampleData.Length);
            int correctedLength = GetCorrectedLength(scaledLength);
            ScaleSampleOntoBuffer(sampleData, correctedLength);
        }

        /// <summary>
        /// Copies the next scaled frame onto the outBuffer. 
        /// </summary>
        /// <param name="outBuffer">The buffer onto which the frame should be copied.</param>
        /// <param name="moreFrames">Whether there are more frames still in the buffer.</param>
        /// <returns>True if a frame was copied onto the buffer, false if not.</returns>
        /// <remarks>
        /// Yes, it would be much easier to create a List&lt;ouT&gt; and return that. However, then that List&lt;outT&gt; would need to get garbage collected,
        /// at a rate of 50+ objects / second, and garbage collection is the enemy of real-time audio processing. Trust me, it needs to work this way.
        /// Oh, and the outBuffer should be recycled as well (rather than garbage collected).
        /// </remarks>
        public virtual bool Read(Array outBuffer, out bool moreFrames)
        {
            if (UnreadBytes >= OutputBytesPerFrame)
            {
                // Return the next available frame.
                lock (this)
                {
                    Buffer.BlockCopy(_scaledBuffer, _sampleBufferReadPosition, outBuffer, 0, OutputBytesPerFrame);
                    _sampleBufferReadPosition += OutputBytesPerFrame;
                    moreFrames = (UnreadBytes >= OutputBytesPerFrame);

                    // If there are no more remaining bytes, move the pointer to the start of the buffer.
                    if (_sampleBufferWritePosition == _sampleBufferReadPosition)
                    {
                        _sampleBufferWritePosition = 0;
                        _sampleBufferReadPosition = 0;
                    }

                    return true;
                }
            }

            moreFrames = false;
            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Given the size of the sample, returns the appropriate size to scale it to given the calculated scaling factor.
        /// </summary>
        private int GetScaledLength(int sampleLength)
        {
            return (int)((sampleLength / (float)OutputBytesPerSample) / _scalingFactor) * OutputBytesPerSample;
        }

        protected virtual int GetCorrectedLength(int scaledLength)
        {
            return scaledLength;
        }

        protected void ScaleSampleOntoBuffer(byte[] originalData, int scaledLength)
        {
            // If there's not enough room left in the buffer, move any unprocessed data back to the beginning.
            if (scaledLength > _scaledBuffer.Length - _sampleBufferWritePosition)
            {
                _sampleBufferWritePosition -= _sampleBufferReadPosition;
                if (_sampleBufferWritePosition > _scaledBuffer.Length - scaledLength)
                {
                    // If the whole buffer is full of unprocessed data, we're kinda screwed anyway,
                    // so just reset everything and start over. There might be a more elegant way of handling this,
                    // but this works for the fairly small number of times this should actually happen (which is usually
                    // just when it's been paused during a debug session).
                    _sampleBufferWritePosition = 0;
                    scaledLength = Math.Min(scaledLength, _scaledBuffer.Length); // Fix to correct scale issues when app has been paused for debugging.
                }
                else
                {
                    // Otherwise, move any unprocessed data back to the beginning.
                    Buffer.BlockCopy(_scaledBuffer, _sampleBufferReadPosition, _scaledBuffer, 0, _sampleBufferWritePosition);
                }
                _sampleBufferReadPosition = 0;
            }

            // Minor optimization if the two are the same length.
            if (originalData.Length == scaledLength && InputChannels == OutputChannels)
            {
                Buffer.BlockCopy(originalData, 0, _scaledBuffer, _sampleBufferWritePosition, scaledLength);
                _sampleBufferWritePosition += scaledLength;
            }
            else
            {
                float totalScalingFactor = originalData.Length / (float)scaledLength;
                int lastOriginalSamplePosition = -1;
                // For each sample in the scaled buffer, determine the best location to pull from in the original sample buffer.
                for (int scaledSamplePosition = 0; scaledSamplePosition < scaledLength; scaledSamplePosition += OutputBytesPerSample)
                {
                    // Get the best original sample, aligned on sample size boundaries.
                    int originalSamplePosition = (int)((scaledSamplePosition / (float)OutputBytesPerSample) * totalScalingFactor) * OutputBytesPerSample;

                    // If we're about to include the same sample twice, perform some smoothing.
                    if (lastOriginalSamplePosition == originalSamplePosition)
                    {
                        if (originalSamplePosition > 0 && originalSamplePosition < originalData.Length - 2)
                        {
                            //short prev1 = BitConverter.ToInt16(originalData, originalSamplePosition - sizeof(short));
                            //short next1 = BitConverter.ToInt16(originalData, originalSamplePosition + sizeof(short));
                            var prev = (short)((originalData[originalSamplePosition - 1] << 8) | originalData[originalSamplePosition - 2]);
                            var next = (short)((originalData[originalSamplePosition + 3] << 8) | originalData[originalSamplePosition + 2]);
                            var step = (short)((prev - next) / 3);
                            var s1 = (short)(prev - step);
                            var s2 = (short)(next + step);
                            _scaledBuffer[_sampleBufferWritePosition - 2] = (byte)(s1);
                            _scaledBuffer[_sampleBufferWritePosition - 1] = (byte)(s1 >> 8);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(s2);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(s2 >> 8);
                        }
                        else if (originalSamplePosition == 0)
                        {
                            var curr = (short)((originalData[originalSamplePosition + 1] << 8) | originalData[originalSamplePosition]);
                            var next = (short)((originalData[originalSamplePosition + 3] << 8) | originalData[originalSamplePosition + 2]);
                            curr = (short)((curr + next) / 2);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(curr);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(curr >> 8);
                        }
                        else
                        {
                            // ks 10/16/10 - I don't think we'll ever hit this, but I wanted to have it in here in case.
                            var curr = (short)((originalData[originalSamplePosition + 1] << 8) | originalData[originalSamplePosition]);
                            var prev = (short)((originalData[originalSamplePosition - 1] << 8) | originalData[originalSamplePosition - 2]);
                            curr = (short)((curr + prev) / 2);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(curr);
                            _scaledBuffer[_sampleBufferWritePosition++] = (byte)(curr >> 8);
                        }
                    }
                    else
                    {
                        // Assumes that this is only two bytes -- no loop for extra speed.
                        // Note that this is ~ twice as fast as Buffer.BlockCopy().
                        lastOriginalSamplePosition = originalSamplePosition;
                        _scaledBuffer[_sampleBufferWritePosition++] = originalData[originalSamplePosition++];
                        _scaledBuffer[_sampleBufferWritePosition++] = originalData[originalSamplePosition];
                    }
                }
            }
        }

        #endregion
    }
}
