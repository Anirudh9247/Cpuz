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
    private readonly ToolStripMenuItem _cpuMenuItem;
    private readonly ToolStripMenuItem _gpuMenuItem;
    private readonly ToolStripMenuItem _memoryMenuItem;
    private readonly ToolStripMenuItem _storageMenuItem;
    private readonly ToolStripMenuItem _processMenuItem;
    private readonly ToolStripMenuItem _clientsMenuItem;
    private readonly ToolStripMenuItem _toggleMenuItem;
    private HealthSnapshot? _lastSnapshot;

    public TrayApplicationContext()
    {
        Console.WriteLine("[TrayApp] Constructor starting...");

        var config = Options.Create(new AgentConfig());

        // Initialize Core Services
        Console.WriteLine("[TrayApp] Creating core services...");
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
        Console.WriteLine("[TrayApp] Core services created.");

        // Menu Items
        _statusMenuItem = new ToolStripMenuItem("🟢 Status: Initializing...") { Enabled = false };
        _cpuMenuItem = new ToolStripMenuItem("💻 CPU: Reading...") { Enabled = false };
        _gpuMenuItem = new ToolStripMenuItem("🎮 GPU: Reading...") { Enabled = false };
        _memoryMenuItem = new ToolStripMenuItem("🧠 RAM: Reading...") { Enabled = false };
        _storageMenuItem = new ToolStripMenuItem("💽 Storage: Reading...") { Enabled = false };
        _processMenuItem = new ToolStripMenuItem("⚙️ Processes: Reading...") { Enabled = false };
        _clientsMenuItem = new ToolStripMenuItem("📱 Connected Devices: 0") { Enabled = false };
        _toggleMenuItem = new ToolStripMenuItem("⏹️ Stop Monitoring", null, OnToggleMonitoring);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_cpuMenuItem);
        contextMenu.Items.Add(_gpuMenuItem);
        contextMenu.Items.Add(_memoryMenuItem);
        contextMenu.Items.Add(_storageMenuItem);
        contextMenu.Items.Add(_processMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_clientsMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_toggleMenuItem);
        contextMenu.Items.Add(new ToolStripMenuItem("ℹ️ Full System Diagnostics", null, OnShowInfo));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(new ToolStripMenuItem("❌ Exit Agent", null, OnExit));
        Console.WriteLine("[TrayApp] Context menu built.");

        // Create a custom icon (green circle on transparent background)
        var icon = CreateTrayIcon();
        Console.WriteLine($"[TrayApp] Icon created: {icon.Width}x{icon.Height}");

        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = icon;
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.Text = "ComputerDoctor Agent";
        _notifyIcon.DoubleClick += (s, e) => OnShowInfo(s, e);
        _notifyIcon.Visible = true;
        Console.WriteLine("[TrayApp] NotifyIcon.Visible = true. Icon should now appear in system tray.");

        // Start Networking & Background Tasks
        StartServices();

        // 2-Second Telemetry Timer
        _timer = new System.Windows.Forms.Timer { Interval = 2000 };
        _timer.Tick += async (s, e) => await OnTimerTickAsync();
        _timer.Start();
        Console.WriteLine("[TrayApp] Timer started. Showing balloon tip...");

        _notifyIcon.ShowBalloonTip(5000, "ComputerDoctor Agent", "Agent active. Right-click icon for full hardware metrics.", ToolTipIcon.Info);
        Console.WriteLine("[TrayApp] Constructor complete. App is fully running.");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static Icon CreateTrayIcon()
    {
        // Create a 32x32 icon with a green filled circle (visible in tray)
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            // Green circle with dark border
            g.FillEllipse(Brushes.LimeGreen, 2, 2, 28, 28);
            g.DrawEllipse(new Pen(Color.DarkGreen, 2), 2, 2, 28, 28);
            // "+" cross in center for medical theme
            g.FillRectangle(Brushes.White, 12, 6, 8, 20);
            g.FillRectangle(Brushes.White, 6, 12, 20, 8);
        }

        IntPtr hIcon = bmp.GetHicon();
        using var tempIcon = Icon.FromHandle(hIcon);
        Icon clonedIcon = (Icon)tempIcon.Clone();
        DestroyIcon(hIcon);
        return clonedIcon;
    }

    private void StartServices()
    {
        // Try multiple ports for WebSocket server
        string[] ports = ["http://localhost:8085/ws/", "http://localhost:8086/ws/", "http://localhost:8087/ws/"];
        bool wsStarted = false;

        foreach (var url in ports)
        {
            try
            {
                _webSocketServer.StartAsync(url).GetAwaiter().GetResult();
                Console.WriteLine($"[Agent.TrayApp] WebSocket server started on {url}");
                wsStarted = true;
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agent.TrayApp] Port {url} unavailable: {ex.Message}");
            }
        }

        if (!wsStarted)
        {
            Console.WriteLine("[Agent.TrayApp] WARNING: WebSocket server could not start on any port. Monitoring-only mode.");
        }

        try
        {
            _discoveryBroadcaster.Start(port: 8888, broadcastIntervalMs: 3000);
            Console.WriteLine("[Agent.TrayApp] UDP Discovery Broadcaster started on port 8888");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agent.TrayApp] Discovery broadcaster failed: {ex.Message}");
        }

        try
        {
            _heartbeatMonitor.Start(pingIntervalMs: 5000, pongTimeoutMs: 15000);
            Console.WriteLine("[Agent.TrayApp] Heartbeat Monitor started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agent.TrayApp] Heartbeat monitor failed: {ex.Message}");
        }
    }

    private async Task OnTimerTickAsync()
    {
        if (!_isMonitoring) return;

        try
        {
            // Collect snapshot on background thread so STA UI thread remains 100% responsive
            var snapshot = await Task.Run(() => _telemetryCollector.CollectSnapshotAsync());
            _lastSnapshot = snapshot;

            string badge = snapshot.OverallStatus switch
            {
                OverallHealthStatus.Healthy => "🟢 Healthy",
                OverallHealthStatus.Warning => "🟡 Warning",
                OverallHealthStatus.Critical => "🔴 Critical",
                _ => "⚪ Unknown"
            };

            _statusMenuItem.Text = $"Status: {badge} ({snapshot.OverallHealthScore}/100)";

            // CPU
            double cpuLoad = snapshot.Cpu.LoadPercent.HasValue && snapshot.Cpu.LoadPercent.Value.HasValue ? snapshot.Cpu.LoadPercent.Value.Value : 0;
            string cpuTempStr = snapshot.Cpu.TempC.HasValue && snapshot.Cpu.TempC.Value.HasValue ? $"{snapshot.Cpu.TempC.Value.Value:F1}°C" : "N/A";
            _cpuMenuItem.Text = $"💻 CPU: {cpuLoad:F1}% | Temp: {cpuTempStr} | Cores: {snapshot.Cpu.LogicalProcessorCount}";

            // GPU
            double gpuLoad = snapshot.Gpu.LoadPercent.HasValue && snapshot.Gpu.LoadPercent.Value.HasValue ? snapshot.Gpu.LoadPercent.Value.Value : 0;
            string gpuTempStr = snapshot.Gpu.TempC.HasValue && snapshot.Gpu.TempC.Value.HasValue ? $"{snapshot.Gpu.TempC.Value.Value:F1}°C" : "N/A";
            _gpuMenuItem.Text = $"🎮 GPU: {gpuLoad:F1}% | Temp: {gpuTempStr}";

            // Memory
            double ramUsage = snapshot.Memory.UsagePercent.HasValue && snapshot.Memory.UsagePercent.Value.HasValue ? snapshot.Memory.UsagePercent.Value.Value : 0;
            double usedGb = snapshot.Memory.UsedMb.HasValue && snapshot.Memory.UsedMb.Value.HasValue ? snapshot.Memory.UsedMb.Value.Value / 1024.0 : 0;
            double totalGb = snapshot.Memory.TotalMb.HasValue && snapshot.Memory.TotalMb.Value.HasValue ? snapshot.Memory.TotalMb.Value.Value / 1024.0 : 0;
            _memoryMenuItem.Text = $"🧠 RAM: {ramUsage:F1}% ({usedGb:F1} / {totalGb:F1} GB)";

            // Storage
            if (snapshot.Drives.Count > 0)
            {
                var mainDrive = snapshot.Drives[0];
                _storageMenuItem.Text = $"💽 Disk ({mainDrive.Name}): {mainDrive.FreeSpaceGb:F1} GB Free / {mainDrive.TotalSizeGb:F1} GB";
            }
            else
            {
                _storageMenuItem.Text = "💽 Disk: Ready";
            }

            // Processes
            string topProcName = snapshot.Processes.TopProcesses.Count > 0 ? snapshot.Processes.TopProcesses[0].ProcessName : "None";
            double topProcMem = snapshot.Processes.TopProcesses.Count > 0 ? snapshot.Processes.TopProcesses[0].PrivateMemoryMb : 0;
            _processMenuItem.Text = $"⚙️ Processes: {snapshot.Processes.TotalRunningCount} Active | Top: {topProcName} ({topProcMem:F0} MB)";

            _clientsMenuItem.Text = $"📱 Connected Mobile Devices: {_webSocketServer.ConnectedClientCount}";

            Console.WriteLine($"[Agent.TrayApp] Telemetry updated: Score={snapshot.OverallHealthScore}, CPU={cpuLoad:F1}%, GPU={gpuLoad:F1}%, RAM={ramUsage:F1}%, Drives={snapshot.Drives.Count}, Procs={snapshot.Processes.TotalRunningCount}");

            // Trigger balloon notification on critical alert
            if (snapshot.OverallStatus == OverallHealthStatus.Critical && snapshot.Alerts.Count > 0)
            {
                var topAlert = snapshot.Alerts[0];
                _notifyIcon.ShowBalloonTip(3000, "🚨 Critical System Alert", $"{topAlert.Category}: {topAlert.Message}", ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Agent.TrayApp] Error during telemetry tick: {ex.Message}");
        }
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
        var s = _lastSnapshot;
        string diagText = s != null ?
            $"• Machine: {s.MachineName}\n" +
            $"• Health Score: {s.OverallHealthScore}/100 ({s.OverallStatus})\n" +
            $"• CPU Load: {s.Cpu.LoadPercent.Value?.ToString("F1") ?? "0"}% ({s.Cpu.LogicalProcessorCount} Logical Cores)\n" +
            $"• CPU Temp: {s.Cpu.TempC.Value?.ToString("F1") ?? "N/A"}°C\n" +
            $"• GPU Load: {s.Gpu.LoadPercent.Value?.ToString("F1") ?? "0"}% (Temp: {s.Gpu.TempC.Value?.ToString("F1") ?? "N/A"}°C)\n" +
            $"• Memory: {s.Memory.UsagePercent.Value?.ToString("F1") ?? "0"}% ({s.Memory.UsedMb.Value / 1024.0:F1} / {s.Memory.TotalMb.Value / 1024.0:F1} GB)\n" +
            $"• Total Processes: {s.Processes.TotalRunningCount}\n" +
            $"• Storage Drives: {s.Drives.Count} Drive(s) Detected\n" +
            $"• Active Alerts: {s.Alerts.Count}\n" +
            $"• Active Mobile Connections: {_webSocketServer.ConnectedClientCount}\n" :
            "Telemetry initializing...";

        MessageBox.Show(
            "ComputerDoctor Agent v1.0.0 — Full Hardware Diagnostics\n\n" +
            diagText + "\n" +
            "• Auto-Discovery Beacon: UDP Port 8888 (Broadcast: 3s)\n" +
            "• WebSocket Pairing Host: Active\n\n" +
            "All hardware sensors, processes, drives & security telemetry active.",
            "ComputerDoctor Agent Diagnostics",
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
