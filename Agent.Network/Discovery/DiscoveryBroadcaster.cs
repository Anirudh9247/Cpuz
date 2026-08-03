using System.Net;
using System.Net.Sockets;
using System.Text;
using Agent.Network.Json;

namespace Agent.Network.Discovery;

public class DiscoveryBeaconPayload
{
    public string Service { get; set; } = "ComputerDoctorAI";
    public string AgentId { get; set; } = "COMPUTER-DOCTOR-AGENT-01";
    public string AgentName { get; set; } = Environment.MachineName;
    public string AgentVersion { get; set; } = "1.0.0";
    public string WsUrl { get; set; } = "ws://0.0.0.0:8080/ws";
    public int Port { get; set; } = 8080;
}

public class DiscoveryBroadcaster : IDiscoveryBroadcaster
{
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private bool _isBroadcasting;

    public bool IsBroadcasting => _isBroadcasting;

    public void Start(int port = 8888, int broadcastIntervalMs = 3000)
    {
        if (_isBroadcasting) return;

        _udpClient = new UdpClient();
        _udpClient.EnableBroadcast = true;
        _cts = new CancellationTokenSource();
        _isBroadcasting = true;

        string localIp = GetLocalIpAddress();
        var beacon = new DiscoveryBeaconPayload
        {
            AgentName = Environment.MachineName,
            WsUrl = $"ws://{localIp}:8080/ws",
            Port = 8080
        };

        string json = AgentJsonSerializer.Serialize(beacon);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        _ = Task.Run(async () =>
        {
            var endpoint = new IPEndPoint(IPAddress.Broadcast, port);
            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
                }
                catch
                {
                    // Ignore broadcast send errors when network adapter reconnects
                }

                try
                {
                    await Task.Delay(broadcastIntervalMs, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    public void Stop()
    {
        _isBroadcasting = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch { }
        _udpClient = null;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch { }
        return "127.0.0.1";
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
