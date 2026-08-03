using System.Drawing;
using System.Windows.Forms;
using Agent.Core.Alerts;
using Agent.Core.Commands;
using ICommandExecutor = Agent.Core.Commands.ICommandExecutor;
using Agent.Core.Hardware;
using Agent.Core.Health;
using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Security;
using Agent.Core.Storage;
using Agent.Core.Telemetry;
using Agent.Core.Validation;
using Agent.Network.Discovery;
using Agent.Network.Security;
using Agent.Network.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Agent.TrayApp;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ITelemetryCollector _telemetryCollector;
    private readonly IAgentWebSocketServer _webSocketServer;
    private readonly IDiscoveryBroadcaster _discoveryBroadcaster;
    private readonly ISessionPairingManager _sessionPairingManager;
    private readonly IHeartbeatMonitor _heartbeatMonitor;
    private readonly ICommandExecutor _commandExecutor;
    private bool _isMonitoring = true;

    // Menu Item References
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _telemetryMenuItem;
    private readonly ToolStripMenuItem _clientsMenuItem;
    private readonly ToolStripMenuItem _toggleMenuItem;

    public TrayApplicationContext()
    {
        var config = Options.Create(new AgentConfig());

        // Initialize Core Services
        var sensorPipeline = new SensorPipeline();
        var hardwareMonitor = new HardwareMonitor();
        var processMonitor = new ProcessMonitor();
        var storageMonitor = new StorageMonitor();
        var alertEngine = new AlertEngine(config);
        var healthCalculator = new HealthScoreCalculator();
        var snapshotBuilder = new HealthSnapshotBuilder(alertEngine, healthCalculator);
        var validator = new HealthSnapshotValidator();

        _telemetryCollector = new TelemetryCollector(hardwareMonitor, processMonitor, storageMonitor, snapshotBuilder, config);
        _sessionPairingManager = new SessionPairingManager(config);
        _commandExecutor = new CommandExecutor(processMonitor, _sessionPairingManager);
        _webSocketServer = new AgentWebSocketServer();
        _discoveryBroadcaster = new DiscoveryBroadcaster();
        _heartbeatMonitor = new HeartbeatMonitor(_sessionPairingManager, _webSocketServer, NullLogger<HeartbeatMonitor>.Instance);

        // Menu Items
        _statusMenuItem = new ToolStripMenuItem("💚 Status: Initializing...", null) { Enabled = false };
        _telemetryMenuItem = new ToolStripMenuItem("💻 Hardware: Reading...", null) { Enabled = false };
        _clientsMenuItem = new ToolStripMenuItem("📱 Connected Devices: 0", null) { Enabled = false };
        _toggleMenuItem = new ToolStripMenuItem("⏹️ Stop Monitoring", null, OnToggleMonitoring);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(_telemetryMenuItem);
        contextMenu.Items.Add(_clientsMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_toggleMenuItem);
        contextMenu.Items.Add(new ToolStripMenuItem("ℹ️ Server Info (Port 8080/8888)", null, OnShowInfo));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(new ToolStripMenuItem("❌ Exit Agent", null, OnExit));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            ContextMenuStrip = contextMenu,
            Text = "ComputerDoctor Agent",
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => OnShowInfo(s, e);

        // Start Networking & Background Tasks
        StartServices();

        // 2-Second Telemetry Timer
        _timer = new System.Windows.Forms.Timer { Interval = 2000 };
        _timer.Tick += async (s, e) => await OnTimerTickAsync();
        _timer.Start();

        _notifyIcon.ShowBalloonTip(3000, "ComputerDoctor Agent Running", "System tray monitoring and P2P server active.", ToolTipIcon.Info);
    }

    private void StartServices()
    {
        try
        {
            _webSocketServer.StartAsync("http://localhost:8080/ws/").GetAwaiter().GetResult();
            _discoveryBroadcaster.Start(port: 8888, broadcastIntervalMs: 3000);
            _heartbeatMonitor.Start(pingIntervalMs: 5000, pongTimeoutMs: 15000);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Service Initialization Error: {ex.Message}", "ComputerDoctor Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task OnTimerTickAsync()
    {
        if (!_isMonitoring) return;

        try
        {
            var snapshot = await _telemetryCollector.CollectSnapshotAsync();

            string badge = snapshot.OverallStatus switch
            {
                OverallHealthStatus.Healthy => "🟢 Healthy",
                OverallHealthStatus.Warning => "🟡 Warning",
                OverallHealthStatus.Critical => "🔴 Critical",
                _ => "⚪ Unknown"
            };

            _statusMenuItem.Text = $"System Status: {badge} ({snapshot.OverallHealthScore}/100)";
            
            double cpuLoad = snapshot.Cpu.LoadPercent.HasValue && snapshot.Cpu.LoadPercent.Value.HasValue ? snapshot.Cpu.LoadPercent.Value.Value : 0;
            string cpuTempStr = snapshot.Cpu.TempC.HasValue && snapshot.Cpu.TempC.Value.HasValue ? $"{snapshot.Cpu.TempC.Value.Value:F1}°C" : "N/A";
            double ramUsage = snapshot.Memory.UsagePercent.HasValue && snapshot.Memory.UsagePercent.Value.HasValue ? snapshot.Memory.UsagePercent.Value.Value : 0;

            _telemetryMenuItem.Text = $"💻 CPU: {cpuLoad:F1}% ({cpuTempStr}) | RAM: {ramUsage:F1}%";
            _clientsMenuItem.Text = $"📱 Connected Devices: {_webSocketServer.ConnectedClientCount}";

            // Trigger balloon notification on critical alert
            if (snapshot.OverallStatus == OverallHealthStatus.Critical && snapshot.Alerts.Count > 0)
            {
                var topAlert = snapshot.Alerts[0];
                _notifyIcon.ShowBalloonTip(3000, "🚨 Critical System Alert", $"{topAlert.Category}: {topAlert.Message}", ToolTipIcon.Warning);
            }
        }
        catch { }
    }

    private void OnToggleMonitoring(object? sender, EventArgs e)
    {
        _isMonitoring = !_isMonitoring;
        if (_isMonitoring)
        {
            _toggleMenuItem.Text = "⏹️ Stop Monitoring";
            _timer.Start();
            _notifyIcon.Text = "ComputerDoctor Agent (Monitoring Active)";
        }
        else
        {
            _toggleMenuItem.Text = "▶️ Start Monitoring";
            _timer.Stop();
            _statusMenuItem.Text = "⏸️ Monitoring Paused";
            _notifyIcon.Text = "ComputerDoctor Agent (Paused)";
        }
    }

    private void OnShowInfo(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "ComputerDoctor Agent v1.0.0\n\n" +
            "• WebSocket Server: ws://localhost:8080/ws/\n" +
            "• Auto-Discovery Beacon: UDP Port 8888 (Broadcast: 3s)\n" +
            "• Active Clients: " + _webSocketServer.ConnectedClientCount + "\n\n" +
            "P2P Network Host is running and ready for mobile pairing.",
            "ComputerDoctor Agent Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _timer.Stop();
        _heartbeatMonitor.Dispose();
        _discoveryBroadcaster.Dispose();
        _webSocketServer.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        ExitThread();
    }
}
