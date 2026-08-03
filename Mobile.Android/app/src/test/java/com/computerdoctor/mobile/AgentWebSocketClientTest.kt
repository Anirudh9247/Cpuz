package com.computerdoctor.mobile

import com.computerdoctor.mobile.model.*
import com.google.gson.Gson
import org.junit.Assert.*
import org.junit.Test

class AgentWebSocketClientTest {

    private val gson = Gson()

    @Test
    fun testNetworkEnvelopeSerialization_PairRequest_MatchesFormat() {
        val payload = PairRequestPayload(pin = "123456", deviceName = "Pixel 8")
        val envelope = NetworkEnvelope(
            type = "PAIR_REQUEST",
            payload = payload
        )

        val json = gson.toJson(envelope)

        assertTrue(json.contains("\"Type\":\"PAIR_REQUEST\""))
        assertTrue(json.contains("\"pin\":\"123456\""))
        assertTrue(json.contains("\"deviceName\":\"Pixel 8\""))
    }

    @Test
    fun testCommandRequestEnvelopeSerialization_MatchesFormat() {
        val payload = CommandRequestPayload(
            command = "FLUSH_DNS",
            sessionToken = "TOKEN123",
            parameters = mapOf("param1" to "val1")
        )
        val envelope = NetworkEnvelope(
            type = "COMMAND",
            payload = payload
        )

        val json = gson.toJson(envelope)

        assertTrue(json.contains("\"Type\":\"COMMAND\""))
        assertTrue(json.contains("\"command\":\"FLUSH_DNS\""))
        assertTrue(json.contains("\"sessionToken\":\"TOKEN123\""))
    }

    @Test
    fun testHealthSnapshotParsing_ValidTelemetryJson_ReturnsMetrics() {
        val json = """
            {
                "timestamp": "2026-08-03T12:00:00Z",
                "healthScore": 92,
                "healthStatus": "Good",
                "cpu": {
                    "tempC": 48.5,
                    "clockMhz": 3400,
                    "loadPercent": 14.2
                },
                "gpu": {
                    "tempC": 52.0,
                    "clockMhz": 1700
                },
                "ram": {
                    "usagePercent": 42.0,
                    "topProcesses": [
                        {"name": "devenv.exe", "mb": 1200, "pid": 4512}
                    ]
                },
                "ssd": {
                    "healthPercent": 98,
                    "smartStatus": "OK"
                }
            }
        """.trimIndent()

        val snapshot = gson.fromJson(json, HealthSnapshot::class.java)

        assertNotNull(snapshot)
        assertEquals(92, snapshot.healthScore)
        assertEquals("Good", snapshot.healthStatus)
        assertEquals(48.5, snapshot.cpu.tempC, 0.01)
        assertEquals(42.0, snapshot.ram.usagePercent, 0.01)
        assertEquals(1, snapshot.ram.topProcesses.size)
        assertEquals("devenv.exe", snapshot.ram.topProcesses[0].name)
    }
}
