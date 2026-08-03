package com.computerdoctor.mobile.model

import com.google.gson.annotations.SerializedName
import java.util.UUID

/**
 * Discovery beacon payload received from Desktop Agent over UDP port 8888.
 */
data class DiscoveryBeaconPayload(
    @SerializedName("service") val service: String = "ComputerDoctorAI",
    @SerializedName("agentId") val agentId: String = "COMPUTER-DOCTOR-AGENT-01",
    @SerializedName("agentName") val agentName: String = "",
    @SerializedName("agentVersion") val agentVersion: String = "1.0.0",
    @SerializedName("wsUrl") val wsUrl: String = "",
    @SerializedName("port") val port: Int = 8080
)

/**
 * Container for discovered agents on local network.
 */
data class DiscoveredAgent(
    val id: String,
    val name: String,
    val ipAddress: String,
    val wsUrl: String,
    val port: Int,
    val lastSeenTimestamp: Long = System.currentTimeMillis()
)

/**
 * Client connection state machine as a Sealed Class for type-safe exhaustive pattern matching.
 */
sealed class ConnectionState {
    object Disconnected : ConnectionState()
    object Connecting : ConnectionState()
    object ConnectedUnpaired : ConnectionState()
    object Pairing : ConnectionState()
    object Paired : ConnectionState()
    object Active : ConnectionState()
    data class Reconnecting(val attempt: Int, val maxAttempts: Int = 5) : ConnectionState()
    data class Faulted(val error: String) : ConnectionState()
}

/**
 * Standardized protocol envelope matching Desktop Agent NetworkEnvelope<T>.
 */
data class NetworkEnvelope<T>(
    @SerializedName("schemaVersion") val schemaVersion: Int = 1,
    @SerializedName("messageId") val messageId: String = UUID.randomUUID().toString().replace("-", ""),
    @SerializedName("messageType") val messageType: String, // TELEMETRY, PAIR_REQUEST, PAIR_RESPONSE, COMMAND, COMMAND_ACK, PING, PONG
    @SerializedName("timestamp") val timestamp: String = java.time.Instant.now().toString(),
    @SerializedName("agentId") val agentId: String? = null,
    @SerializedName("sessionId") val sessionId: String? = null,
    @SerializedName("payload") val payload: T? = null
)

/**
 * Pairing Request payload.
 */
data class PairRequestPayload(
    @SerializedName("pin") val pin: String,
    @SerializedName("deviceName") val deviceName: String = android.os.Build.MODEL
)

/**
 * Pairing Response payload.
 */
data class PairResponsePayload(
    @SerializedName("success") val success: Boolean,
    @SerializedName("sessionToken") val sessionToken: String? = null,
    @SerializedName("sessionId") val sessionId: String? = null,
    @SerializedName("errorMessage") val errorMessage: String? = null
)

/**
 * Command Request payload sent to Desktop Agent.
 */
data class CommandRequestPayload(
    @SerializedName("command") val command: String,
    @SerializedName("sessionToken") val sessionToken: String = "",
    @SerializedName("parameters") val parameters: Map<String, String> = emptyMap()
)

/**
 * Command Acknowledgement payload returned by Desktop Agent.
 */
data class CommandAckPayload(
    @SerializedName("commandId") val commandId: String = "",
    @SerializedName("success") val success: Boolean,
    @SerializedName("executionTimeMs") val executionTimeMs: Long = 0,
    @SerializedName("output") val output: String = "",
    @SerializedName("errorMessage") val errorMessage: String? = null
)

// Hardware Telemetry Snapshot Models
data class CpuMetrics(
    @SerializedName("tempC") val tempC: Double = 0.0,
    @SerializedName("clockMhz") val clockMhz: Int = 0,
    @SerializedName("loadPercent") val loadPercent: Double = 0.0
)

data class GpuMetrics(
    @SerializedName("tempC") val tempC: Double = 0.0,
    @SerializedName("clockMhz") val clockMhz: Int = 0
)

data class FanMetrics(
    @SerializedName("rpm") val rpm: Int = 0
)

data class TopProcess(
    @SerializedName("name") val name: String = "",
    @SerializedName("mb") val mb: Int = 0,
    @SerializedName("pid") val pid: Int = 0
)

data class RamMetrics(
    @SerializedName("usagePercent") val usagePercent: Double = 0.0,
    @SerializedName("topProcesses") val topProcesses: List<TopProcess> = emptyList()
)

data class SsdMetrics(
    @SerializedName("healthPercent") val healthPercent: Int = 100,
    @SerializedName("smartStatus") val smartStatus: String = "OK",
    @SerializedName("writeWearPercent") val writeWearPercent: Int = 0
)

data class BatteryMetrics(
    @SerializedName("healthPercent") val healthPercent: Int = 100
)

data class SecurityMetrics(
    @SerializedName("defenderEnabled") val defenderEnabled: Boolean = true,
    @SerializedName("definitionsUpToDate") val definitionsUpToDate: Boolean = true
)

data class TelemetryAlert(
    @SerializedName("type") val type: String = "",
    @SerializedName("severity") val severity: String = "Info", // Info, Warning, Critical
    @SerializedName("component") val component: String = "",
    @SerializedName("message") val message: String = ""
)

data class HealthSnapshot(
    @SerializedName("timestamp") val timestamp: String = "",
    @SerializedName("healthScore") val healthScore: Int = 100,
    @SerializedName("healthStatus") val healthStatus: String = "Good",
    @SerializedName("cpu") val cpu: CpuMetrics = CpuMetrics(),
    @SerializedName("gpu") val gpu: GpuMetrics = GpuMetrics(),
    @SerializedName("fan") val fan: FanMetrics = FanMetrics(),
    @SerializedName("ram") val ram: RamMetrics = RamMetrics(),
    @SerializedName("ssd") val ssd: SsdMetrics = SsdMetrics(),
    @SerializedName("battery") val battery: BatteryMetrics = BatteryMetrics(),
    @SerializedName("security") val security: SecurityMetrics = SecurityMetrics(),
    @SerializedName("alerts") val alerts: List<TelemetryAlert> = emptyList()
)

data class TelemetryPayloadWrapper(
    @SerializedName("messageType") val messageType: String = "HEALTH_SNAPSHOT",
    @SerializedName("agentVersion") val agentVersion: String = "1.0.0",
    @SerializedName("snapshot") val snapshot: HealthSnapshot = HealthSnapshot()
)
