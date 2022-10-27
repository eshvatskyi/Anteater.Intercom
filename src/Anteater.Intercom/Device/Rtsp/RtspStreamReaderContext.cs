using System;

namespace Anteater.Intercom.Device.Rtsp;

public struct RtspStreamReaderContext
{
    public DateTime Timestamp;

    public bool IsReadingStopped => IsReconnecting || IsStopping || IsStopped;

    public bool IsReconnecting;

    public bool IsStopping;

    public bool IsStopped;
}
