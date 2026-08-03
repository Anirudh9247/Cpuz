package com.computerdoctor.mobile

import com.computerdoctor.mobile.model.DiscoveryBeaconPayload
import com.google.gson.Gson
import org.junit.Assert.*
import org.junit.Test

class UdpDiscoveryListenerTest {

    private val gson = Gson()

    @Test
    fun testDiscoveryBeaconParsing_ValidJson_ReturnsPayload() {
        val json = """
            {
                "service": "ComputerDoctorAI",
                "agentId": "AGENT-WIN-01",
                "agentName": "DESKTOP-TEST",
                "wsUrl": "ws://192.168.1.100:8080/ws",
                "port": 8080
            }
        """.trimIndent()

        val beacon = gson.fromJson(json, DiscoveryBeaconPayload::class.java)

        assertNotNull(beacon)
        assertEquals("ComputerDoctorAI", beacon.service)
        assertEquals("AGENT-WIN-01", beacon.agentId)
        assertEquals("DESKTOP-TEST", beacon.agentName)
        assertEquals("ws://192.168.1.100:8080/ws", beacon.wsUrl)
        assertEquals(8080, beacon.port)
    }

    @Test
    fun testDiscoveryBeaconParsing_DefaultService_MatchesComputerDoctorAI() {
        val json = """{"agentName": "LAPTOP-01"}"""
        val beacon = gson.fromJson(json, DiscoveryBeaconPayload::class.java)

        assertNotNull(beacon)
        assertEquals("ComputerDoctorAI", beacon.service)
        assertEquals("LAPTOP-01", beacon.agentName)
    }
}
