using Agent.Core.Models;
using Agent.Network.Json;
using Xunit;

namespace Agent.Tests.Network;

public class JsonSerializerTests
{
    [Fact]
    public void Serialize_Deserializes_SystemTelemetryReport_Correctly()
    {
        // Arrange
        var report = new SystemTelemetryReport
        {
            AgentId = "TEST-AGENT-01",
            MachineName = "TEST-HOST",
            TimestampUtc = DateTime.UtcNow,
            Hardware = new HardwareMetrics
            {
                CpuTotalUsagePercentage = 42.5f,
                MemoryUsagePercentage = 68.2f,
                LogicalProcessorCount = 8
            },
            TopProcesses = new List<ProcessInfo>
            {
                new ProcessInfo { Id = 100, ProcessName = "test_proc", WorkingSetMemoryBytes = 104857600 }
            }
        };

        var wrapper = new TelemetryPayloadWrapper
        {
            MessageType = "TELEMETRY_REPORT",
            AgentVersion = "1.0.0",
            Report = report
        };

        // Act
        string json = AgentJsonSerializer.Serialize(wrapper);
        var deserialized = AgentJsonSerializer.Deserialize<TelemetryPayloadWrapper>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("TELEMETRY_REPORT", deserialized.MessageType);
        Assert.Equal("TEST-AGENT-01", deserialized.Report.AgentId);
        Assert.Equal(42.5f, deserialized.Report.Hardware?.CpuTotalUsagePercentage);
        Assert.Single(deserialized.Report.TopProcesses!);
        Assert.Equal("test_proc", deserialized.Report.TopProcesses![0].ProcessName);
    }
}
