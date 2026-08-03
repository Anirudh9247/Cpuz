package com.computerdoctor.mobile.repository

import com.computerdoctor.mobile.model.CommandAckPayload
import com.computerdoctor.mobile.model.ConnectionState
import com.computerdoctor.mobile.model.HealthSnapshot
import com.computerdoctor.mobile.model.PairResponsePayload
import com.computerdoctor.mobile.network.AgentWebSocketClient
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

interface AgentRepository {
    val connectionState: StateFlow<ConnectionState>
    val latestSnapshot: StateFlow<HealthSnapshot?>
    val commandAckEvent: SharedFlow<CommandAckPayload>
    val pairingResultEvent: SharedFlow<PairResponsePayload>

    fun connect(wsUrl: String)
    fun disconnect()
    fun sendPairRequest(pin: String)
    fun sendCommand(commandName: String, parameters: Map<String, String> = emptyMap())
}

class AgentRepositoryImpl(
    private val client: AgentWebSocketClient
) : AgentRepository {

    override val connectionState: StateFlow<ConnectionState> = client.connectionState
    override val latestSnapshot: StateFlow<HealthSnapshot?> = client.latestSnapshot
    override val commandAckEvent: SharedFlow<CommandAckPayload> = client.commandAckEvent
    override val pairingResultEvent: SharedFlow<PairResponsePayload> = client.pairingResultEvent

    override fun connect(wsUrl: String) = client.connect(wsUrl)
    override fun disconnect() = client.disconnect()
    override fun sendPairRequest(pin: String) = client.sendPairRequest(pin)
    override fun sendCommand(commandName: String, parameters: Map<String, String>) = client.sendCommand(commandName, parameters)
}
