namespace AutoWeldSystem.Core.Enums;


public enum PlcConnectionState
{
    Stopped = 0,
    Connecting = 1,
    Connected = 2,
    Reconnecting = 3,
    Disconnected = 4,
    Faulted = 5,
    Unverified = 6
}
