namespace Agent.Core.Security;

public interface IHeartbeatMonitor : IDisposable
{
    void Start(int pingIntervalMs = 5000, int pongTimeoutMs = 15000);
    void RecordPong(string clientId);
    void Stop();
}
