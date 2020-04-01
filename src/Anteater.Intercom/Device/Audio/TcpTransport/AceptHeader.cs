using System.IO;
using System.Linq;
using System.Text;

namespace Anteater.Intercom.Device.Audio.TcpTransport
{
    public class AceptHeader
    {
        private static readonly byte[] EncryptionPhase = Encoding.UTF8.GetBytes("*(!@#$%^nanshanshenzhenchina20060704!@#$%^)*");

        public string DeviceName { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int Flag { get; set; }

        public int SocketType { get; set; }

        public int Misc { get; set; }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(Flag);
            writer.Write(SocketType);
            writer.Write(Misc);
            writer.Write(StringToBytes(Username ?? string.Empty, 17, true));
            writer.Write(StringToBytes(Password ?? string.Empty, 17, true));
            writer.Write(StringToBytes(DeviceName ?? string.Empty, 66));

            return stream.ToArray();
        }

        static byte[] StringToBytes(string value, int length, bool encrypt = false)
        {
            var buffer = new byte[length];

            if (encrypt)
            {
                value = Encript(value);
            }

            Encoding.UTF8.GetBytes(value).CopyTo(buffer, 0);

            return buffer;
        }

        static string Encript(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);

            var i = 0;

            var result = bytes.Select(x =>
            {
                if (i == EncryptionPhase.Length)
                {
                    i = 0;
                }

                var b = (byte)(x ^ EncryptionPhase[i]);

                if (b <= 0)
                {
                    b = x;
                }

                i++;

                return b;
            }).ToArray();

            return Encoding.UTF8.GetString(result);
        }
    }
}
