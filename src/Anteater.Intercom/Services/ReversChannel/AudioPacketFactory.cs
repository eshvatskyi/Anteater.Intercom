using System;
using System.IO;
using Anteater.Intercom.Services.ReversChannel.Headers;

namespace Anteater.Intercom.Services.ReversChannel;

public class AudioPacketFactory : IDisposable
{
    private readonly CommHeader _tcpCommHeader;
    private readonly HvFrameHeader _tcpHvFrameHeader;
    private readonly ExtFrameAudioHeader _tcpExtFrameAudioHeader;
    private readonly BinaryWriter _writer;

    private bool _disposed;

    public AudioPacketFactory(TalkInfoHeader info, BinaryWriter writer)
    {
        var audioSamples = info.AudioSamples / 8000 * BufferSize;

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
                AudioEncodeType = (short)info.AudioEncodeType,
                AudioChannels = (short)info.AudioChannels,
                AudioBits = 16,
                AudioSamples = info.AudioSamples,
                AudioBitrate = 16000
            }
        };

        _writer = writer;
    }

    public int BufferSize => 320;

    public void Write(ReadOnlySpan<byte> data)
    {
        try
        {
            _tcpCommHeader.Write(_writer);
            _tcpHvFrameHeader.Write(_writer);
            _tcpExtFrameAudioHeader.Write(_writer);
            _writer.Write(data);
        }
        finally
        {
            _tcpExtFrameAudioHeader.Timestamp++;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
