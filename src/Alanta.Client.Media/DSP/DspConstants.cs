using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alanta.Client.Media.Dsp
{
    class Constants
    {
        /* dB Values */
        public const float M0dB = 1.0f;
        public const float M3dB = 0.71f;
        public const float M6dB = 0.50f;
        public const float M9dB = 0.35f;
        public const float M12dB = 0.25f;
        public const float M18dB = 0.125f;
        public const float M24dB = 0.063f;

        /* dB values for 16bit PCM */
        /* MxdB_PCM = 32767 * 10 ^(x / 20) */
        public const float M10dB_PCM = 10362.0f;
        public const float M20dB_PCM = 3277.0f;
        public const float M25dB_PCM = 1843.0f;
        public const float M30dB_PCM = 1026.0f;
        public const float M35dB_PCM = 583.0f;
        public const float M40dB_PCM = 328.0f;
        public const float M45dB_PCM = 184.0f;
        public const float M50dB_PCM = 104.0f;
        public const float M55dB_PCM = 58.0f;
        public const float M60dB_PCM = 33.0f;
        public const float M65dB_PCM = 18.0f;
        public const float M70dB_PCM = 10.0f;
        public const float M75dB_PCM = 6.0f;
        public const float M80dB_PCM = 3.0f;
        public const float M85dB_PCM = 2.0f;
        public const float M90dB_PCM = 1.0f;

        public const float MAXPCM = 32767.0f;
        public const int NLMS_LEN = 20; // 1600; // 0.1 s when sampling at 16000 per second
        public const int DELAY_LEN = 640;  // 2 * 20 ms when sampling at 16000 per second
        public const float STEP_SIZE = 0.7f;
        public const float MIN_SPEAKER_SAMPLES_WHITE = M75dB_PCM;
        public const float GEIGEL_THRESHOLD = M6dB;
        public const int DTD_DEFAULT_HANGOVER = 4000; // 0.25 s when sampling at 16000 per second
        public const int DTD_LEN = 16;
    }
}
