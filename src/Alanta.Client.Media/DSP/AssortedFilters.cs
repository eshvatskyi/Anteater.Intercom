using System;

namespace Alanta.Client.Media.Dsp
{
    /// <summary>
    /// Exponential Smoothing or IIR Infinite Impulse Response Filter
    /// </summary>
    class IIR_HP
    {
        float x;

        public IIR_HP()
        {
            x = 0.0f;
        }

        public float highpass(float input)
        {
            const float a0 = 0.01f;      /* controls Transfer Frequency */
            /* Highpass = Signal - Lowpass. Lowpass = Exponential Smoothing */
            x += a0 * (input - x);
            return input - x;
        }
    }

    /// <summary>
    /// 35 taps FIR Finite Impulse Response filter
    /// </summary>
    /// <remarks>
    ///  Passband 150Hz to 4kHz for 8kHz sample rate, 300Hz to 8kHz for 16kHz sample rate.
    ///  Coefficients calculated with http://www.dsptutor.freeuk.com/KaiserFilterDesign/KaiserFilterDesign.html
    /// </remarks>
    class FIR_HP_300Hz
    {
        float[] z;

        /// <summary>
        /// Kaiser Window FIR Filter, Filter type: High pass
        /// </summary>
        /// <remarks>
        /// Passband: 150.0 - 4000.0 Hz, Order: 34
        /// Transition band: 34.0 Hz, Stopband attenuation: 10.0 dB
        /// </remarks>
        static readonly float[] a = {
          -0.016165324f, -0.017454365f, -0.01871232f, -0.019931411f, 
          -0.021104068f, -0.022222936f, -0.02328091f, -0.024271343f, 
          -0.025187887f, -0.02602462f, -0.026776174f, -0.027437767f, 
          -0.028004972f, -0.028474221f, -0.028842418f, -0.029107114f, 
          -0.02926664f, 0.8524841f, -0.02926664f, -0.029107114f, 
          -0.028842418f, -0.028474221f, -0.028004972f, -0.027437767f, 
          -0.026776174f, -0.02602462f, -0.025187887f, -0.024271343f, 
          -0.02328091f, -0.022222936f, -0.021104068f, -0.019931411f, 
          -0.01871232f, -0.017454365f, -0.016165324f, 0.0f    
                           };

        public FIR_HP_300Hz()
        {
            z = new float[36];
        }

        public float highpass(float input)
        {
            Buffer.BlockCopy(z, 0, z, 1 * sizeof(float), 35 * sizeof(float));
            z[0] = input;
            float sum0 = 0.0f;

            for (int j = 0; j < a.Length; j++)
            {
                sum0 += a[j] * z[j];
            }
            return sum0;
        }
    }

    /// <summary>
    /// Recursive single pole IIR Infinite Impulse response High-pass filter. "Pre-whitens" the signal.
    /// </summary>
    /// <remark>
    /// Reference: The Scientist and Engineer's Guide to Digital Processing
    /// output[N] = A0 * input[N] + A1 * input[N-1] + B1 * output[N-1]
    /// X = exp(-2.0 * pi * Fc)
    /// A0 = (1 + X) / 2
    /// A1 = -(1 + X) / 2
    /// B1 = X
    /// Fc = cutoff freq / sample rate
    /// </remark>
    class PreWhiteningFilter
    {
        float in0, out0;
        float a0, a1, b1;

        public void Init(float Fc)
        {
            b1 = (float)Math.Exp(-2.0f * Math.PI * Fc);
            a0 = (1.0f + b1) / 2.0f;
            a1 = -a0;
            in0 = 0.0f;
            out0 = 0.0f;
        }

        public float Highpass(float input)
        {
            float output = a0 * input + a1 * in0 + b1 * out0;
            in0 = input;
            out0 = output;
            return output;
        }
    }

    /// <summary>
    /// Recursive two pole IIR Infinite Impulse Response filter 
    /// </summary>
    /// <remarks>
    /// Coefficients calculated with http://www.dsptutor.freeuk.com/IIRFilterDesign/IIRFiltDes102.html
    /// </remarks>
    class IIR2
    {
        float[] x = new float[2];
        float[] y = new float[2];

        static readonly float[] a = { 0.29289323f, -0.58578646f, 0.29289323f };
        static readonly float[] b = { 1.3007072E-16f, 0.17157288f };

        public IIR2()
        {
            // memset(this, 0, sizeof(IIR2));
        }

        public float highpass(float input)
        {
            // Butterworth IIR filter, Filter type: HP
            // Passband: 2000 - 4000.0 Hz, Order: 2
            float output = a[0] * input + a[1] * x[0] + a[2] * x[1] - b[0] * y[0] - b[1] * y[1];
            x[1] = x[0];
            x[0] = input;
            y[1] = y[0];
            y[0] = output;
            return output;
        }
    }


}
