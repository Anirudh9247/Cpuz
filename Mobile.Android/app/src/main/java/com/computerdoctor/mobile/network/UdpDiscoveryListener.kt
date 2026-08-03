package com.computerdoctor.mobile.network

import com.computerdoctor.mobile.model.DiscoveredAgent
import com.computerdoctor.mobile.model.DiscoveryBeaconPayload
import com.google.gson.Gson
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetSocketAddress

class UdpDiscoveryListener(
    private val port: Int = 8888
) {
    private val gson = Gson()
    private var socket: DatagramSocket? = null
    private var listeningJob: Job? = null

    private val _discoveredAgents = MutableStateFlow<List<DiscoveredAgent>>(emptyList())
    val discoveredAgents: StateFlow<List<DiscoveredAgent>> = _discoveredAgents.asStateFlow()

    private val agentsMap = mutableMapOf<String, DiscoveredAgent>()

    fun startListening(scope: CoroutineScope) {
        if (listeningJob?.isActive == true) return

        listeningJob = scope.launch(Dispatchers.IO) {
            try {
                socket = DatagramSocket(null).apply {
                    reuseAddress = true
                    bind(InetSocketAddress(port))
                }

                val buffer = ByteArray(2048)
                while (listeningJob?.isActive == true) {
                    val packet = DatagramPacket(buffer, buffer.size)
                    socket?.receive(packet)

                    val json = String(packet.data, 0, packet.length, Charsets.UTF8)
                    val senderIp = packet.address.hostAddress ?: continue

                    try {
                        val beacon = gson.fromJson(json, DiscoveryBeaconPayload::class.java)
                        if (beacon != null && beacon.service == "ComputerDoctorAI") {
                            val wsUrl = if (beacon.wsUrl.isNotBlank()) beacon.wsUrl else "ws://$senderIp:${beacon.port}/ws"
                            val agent = DiscoveredAgent(
                                id = beacon.agentId.ifBlank { senderIp },
                                name = beacon.agentName.ifBlank { "Desktop Agent ($senderIp)" },
                                ipAddress = senderIp,
                                wsUrl = wsUrl,
                                port = beacon.port,
                                lastSeenTimestamp = System.currentTimeMillis()
                            )

                            agentsMap[agent.id] = agent
                            // Prune stale agents (>15 seconds since last beacon)
                            val now = System.currentTimeMillis()
                            agentsMap.entries.removeIf { now - it.value.lastSeenTimestamp > 15000 }

                            _discoveredAgents.value = agentsMap.values.toList()
                        }
                    } catch (e: Exception) {
                        // Ignore malformed JSON packets
                    }
                }
            } catch (e: Exception) {
                // Handle socket closing or bind error
            } finally {
                stopListening()
            }
        }
    }

    fun stopListening() {
        listeningJob?.cancel()
        listeningJob = null
        try {
            socket?.close()
        } catch (e: Exception) { }
        socket = null
    }
}
