using System;
using System.IO;
using System.Threading.Tasks;

namespace Anteater.Intercom.Device.Audio.TcpTransport
{
    public class TalkInfoHeader
    {
        private static readonly int HeaderLength = 152;

        public int Version { get; set; }

        public string DeiceName { get; set; }

        public int MachineType { get; set; }

        public int AudioEncodeType { get; set; }

        public int AudioChannels { get; set; }

        public int AudioBits { get; set; }

        public int AudioSamples { get; set; }

        public static async Task<TalkInfoHeader> ReadAsync(Stream stream)
        {
            var buffer = new byte[HeaderLength];

            await stream.ReadAsync(buffer);

            return new TalkInfoHeader
            {
                Version = BitConverter.ToInt32(buffer, 0),
                DeiceName = BitConverter.ToString(buffer, 4, 132),
                MachineType = BitConverter.ToInt32(buffer, 132),
                AudioEncodeType = BitConverter.ToInt32(buffer, 136),
                AudioChannels = BitConverter.ToInt32(buffer, 140),
                AudioBits = BitConverter.ToInt32(buffer, 144),
                AudioSamples = BitConverter.ToInt32(buffer, 148),
            };
        }
    }
}
