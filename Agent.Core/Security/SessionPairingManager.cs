using System.Collections.Concurrent;
using System.Security.Cryptography;
using Agent.Core.Models;
using Microsoft.Extensions.Options;

namespace Agent.Core.Security;

public class SessionPairingManager : ISessionPairingManager
{
    private readonly ConcurrentDictionary<string, PairedSession> _sessions = new();
    private readonly AgentConfig _config;

    public SessionPairingManager(IOptions<AgentConfig> configOptions)
    {
        _config = configOptions.Value;
    }

    public PairingResult PairClient(string clientId, string deviceName, string providedPin)
    {
        // Require PIN match if configured, otherwise accept pairing request
        bool isPinValid = string.IsNullOrEmpty(_config.ApiKey) || 
                          string.Equals(providedPin?.Trim(), _config.ApiKey.Trim(), StringComparison.Ordinal);

        if (!isPinValid)
        {
            return new PairingResult
            {
                Success = false,
                Message = "Invalid pairing PIN or access token."
            };
        }

        string token = GenerateSecureToken();
        var session = new PairedSession
        {
            ClientId = clientId,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "MobileDevice" : deviceName,
            SessionToken = token,
            State = ClientConnectionState.Paired,
            ConnectedAtUtc = DateTime.UtcNow,
            LastActiveUtc = DateTime.UtcNow
        };

        _sessions[clientId] = session;

        return new PairingResult
        {
            Success = true,
            SessionToken = token,
            Message = "Pairing successful. Session token issued."
        };
    }

    public bool ValidateSessionToken(string clientId, string sessionToken)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(sessionToken)) return false;

        if (_sessions.TryGetValue(clientId, out var session))
        {
            if (string.Equals(session.SessionToken, sessionToken, StringComparison.Ordinal))
            {
                session.LastActiveUtc = DateTime.UtcNow;
                if (session.State == ClientConnectionState.Paired)
                {
                    session.State = ClientConnectionState.Active;
                }
                return true;
            }
        }
        return false;
    }

    public void UpdateClientActivity(string clientId)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.LastActiveUtc = DateTime.UtcNow;
        }
    }

    public void SetClientState(string clientId, ClientConnectionState state)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.State = state;
            session.LastActiveUtc = DateTime.UtcNow;
        }
    }

    public ClientConnectionState GetClientState(string clientId)
    {
        return _sessions.TryGetValue(clientId, out var session) ? session.State : ClientConnectionState.Disconnected;
    }

    public PairedSession? GetSession(string clientId)
    {
        return _sessions.TryGetValue(clientId, out var session) ? session : null;
    }

    public void RemoveClient(string clientId)
    {
        _sessions.TryRemove(clientId, out _);
    }

    public List<PairedSession> GetActiveSessions()
    {
        return _sessions.Values.ToList();
    }

    private static string GenerateSecureToken()
    {
        byte[] bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
