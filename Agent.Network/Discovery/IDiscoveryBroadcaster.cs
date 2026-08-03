namespace Agent.Network.Discovery;

public interface IDiscoveryBroadcaster : IDisposable
{
    bool IsBroadcasting { get; }
    void Start(int port = 8888, int broadcastIntervalMs = 3000);
    void Stop();
}
