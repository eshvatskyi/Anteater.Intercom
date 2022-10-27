using System;
using System.Linq;
using NAudio.Codecs;

namespace Anteater.Intercom.Device.Audio.TcpTransport;

public class ExtFrameAudioPacketFactory
{
    private readonly CommHeader _tcpCommHeader;
    private readonly HvFrameHeader _tcpHvFrameHeader;
    private readonly ExtFrameAudioHeader _tcpExtFrameAudioHeader;
    private readonly Func<short, byte> _encoder;

    public ExtFrameAudioPacketFactory(int encodeType, int samples, int channels)
    {
        _encoder = encodeType switch
        {
            7 => MuLawEncoder.LinearToMuLawSample,
            3 => ALawEncoder.LinearToALawSample,
            _ => Convert.ToByte,
        };

        var audioSamples = (samples / 8000) * 320;

        _tcpCommHeader = new CommHeader
        {
            Command = 3,
            Flag = 17767,
            BufferSize = audioSamples + 44
        };

        _tcpHvFrameHeader = new HvFrameHeader
        {
            ZeroFlag = 0,
            OneFlag = 1,
            SteamFlag = 13,
            BufferSize = audioSamples + 32
        };

        _tcpExtFrameAudioHeader = new ExtFrameAudioHeader
        {
            StartFlag = 101124105,
            EndFlag = 168496141,
            Leight = 32,
            Ver = 16,
            FrameAudio = new ExtFrameAudio
            {
                AudioEncodeType = (short)encodeType,
                AudioChannels = (short)channels,
                AudioBits = 16,
                AudioSamples = samples,
                AudioBitrate = 16000
            }
        };
    }

    public byte[] Create(short[] data)
    {
        try
        {
            return _tcpCommHeader.ToBytes()
                .Concat(_tcpHvFrameHeader.ToBytes())
                .Concat(_tcpExtFrameAudioHeader.ToBytes())
                .Concat(data.Select(_encoder).ToArray())
                .ToArray();
        }
        finally
        {
            _tcpExtFrameAudioHeader.Timestamp++;
        }
    }
}
