using Agent.Core.Alerts;
using Agent.Core.Hardware;
using Agent.Core.Health;
using Agent.Core.Models;
using Agent.Core.Processes;
using Agent.Core.Storage;
using Agent.Core.Telemetry;
using Agent.Core.Validation;
using Agent.Network.WebSocket;
using Agent.Service.BackgroundService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args);

builder.UseWindowsService(options =>
{
    options.ServiceName = "ComputerDoctorAgent";
});

builder.ConfigureServices((hostContext, services) =>
{
    // Configuration
    services.Configure<AgentConfig>(hostContext.Configuration.GetSection("AgentConfig"));

    // Core Pipeline & Engines
    services.AddSingleton<ISensorPipeline, SensorPipeline>();
    services.AddSingleton<IHardwareMonitor, HardwareMonitor>();
    services.AddSingleton<IProcessMonitor, ProcessMonitor>();
    services.AddSingleton<IStorageMonitor, StorageMonitor>();
    services.AddSingleton<IAlertEngine, AlertEngine>();
    services.AddSingleton<IHealthScoreCalculator, HealthScoreCalculator>();
    services.AddSingleton<IHealthSnapshotBuilder, HealthSnapshotBuilder>();
    services.AddSingleton<IHealthSnapshotValidator, HealthSnapshotValidator>();
    services.AddSingleton<ITelemetryCollector, TelemetryCollector>();

    // Network Component
    services.AddSingleton<IAgentWebSocketClient, AgentWebSocketClient>();
    services.AddSingleton<IAgentWebSocketServer, AgentWebSocketServer>();

    // Background Worker
    services.AddHostedService<AgentWorker>();
});

builder.ConfigureLogging((hostContext, logging) =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddEventLog();
});

var host = builder.Build();
await host.RunAsync();
