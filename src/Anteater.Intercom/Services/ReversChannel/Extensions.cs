using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Anteater.Intercom.Services.ReversChannel;

public static class Extensions
{
    public static void WriteString(this BinaryWriter writer, string value, int length)
    {
        Span<byte> buffer = stackalloc byte[length];

        Encoding.UTF8.GetBytes(value, buffer);

        writer.Write(buffer);
    }

    public static void WriteString(this BinaryWriter writer, string value, int length, byte[] encryptionPhase)
    {
        value = Encript(value, encryptionPhase);

        writer.WriteString(value, length);
    }

    static string Encript(string value, byte[] encryptionPhase)
    {
        var bytes = Encoding.ASCII.GetBytes(value);

        var i = 0;

        var result = bytes.Select(x =>
        {
            if (i == encryptionPhase.Length)
            {
                i = 0;
            }

            var b = (byte)(x ^ encryptionPhase[i]);

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
