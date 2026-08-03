namespace Agent.Core.Security;

public enum ClientConnectionState
{
    Disconnected,
    Connecting,
    Paired,
    Active,
    Faulted
}

public class PairedSession
{
    public string ClientId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public ClientConnectionState State { get; set; } = ClientConnectionState.Connecting;
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;
}

public class PairingResult
{
    public bool Success { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public interface ISessionPairingManager
{
    PairingResult PairClient(string clientId, string deviceName, string providedPin);
    bool ValidateSessionToken(string clientId, string sessionToken);
    void UpdateClientActivity(string clientId);
    void SetClientState(string clientId, ClientConnectionState state);
    ClientConnectionState GetClientState(string clientId);
    PairedSession? GetSession(string clientId);
    void RemoveClient(string clientId);
    List<PairedSession> GetActiveSessions();
}
