using System.IO;
using System.Text;

namespace Anteater.Intercom.Services.ReversChannel.Headers;

public class AcceptHeader
{
    private static readonly byte[] EncryptionPhase = Encoding.UTF8.GetBytes("*(!@#$%^nanshanshenzhenchina20060704!@#$%^)*");

    public string DeviceName { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public int Flag { get; set; }

    public int SocketType { get; set; }

    public int Misc { get; set; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Flag);
        writer.Write(SocketType);
        writer.Write(Misc);
        writer.WriteString(Username ?? "", 17, EncryptionPhase);
        writer.WriteString(Password ?? "", 17, EncryptionPhase);
        writer.WriteString(DeviceName ?? "", 66);
    }
}
