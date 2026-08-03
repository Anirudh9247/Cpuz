package com.computerdoctor.mobile.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.computerdoctor.mobile.model.*
import com.computerdoctor.mobile.network.AgentWebSocketClient
import com.computerdoctor.mobile.repository.AgentRepository
import com.computerdoctor.mobile.repository.AgentRepositoryImpl
import com.computerdoctor.mobile.repository.DiscoveryRepository
import com.computerdoctor.mobile.repository.DiscoveryRepositoryImpl
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

class MainViewModel(
    private val discoveryRepository: DiscoveryRepository = DiscoveryRepositoryImpl(),
    private val agentRepository: AgentRepository = AgentRepositoryImpl(AgentWebSocketClient(viewModelScope))
) : ViewModel() {

    val discoveredAgents: StateFlow<List<DiscoveredAgent>> = discoveryRepository.discoveredAgents
    val connectionState: StateFlow<ConnectionState> = agentRepository.connectionState
    val latestSnapshot: StateFlow<HealthSnapshot?> = agentRepository.latestSnapshot

    private val _selectedAgent = MutableStateFlow<DiscoveredAgent?>(null)
    val selectedAgent: StateFlow<DiscoveredAgent?> = _selectedAgent.asStateFlow()

    private val _lastAckMessage = MutableStateFlow<String?>(null)
    val lastAckMessage: StateFlow<String?> = _lastAckMessage.asStateFlow()

    private val _pairingErrorMessage = MutableStateFlow<String?>(null)
    val pairingErrorMessage: StateFlow<String?> = _pairingErrorMessage.asStateFlow()

    init {
        discoveryRepository.startDiscovery(viewModelScope)

        viewModelScope.launch {
            agentRepository.commandAckEvent.collect { ack ->
                val text = if (ack.success) {
                    "✅ Executed in ${ack.executionTimeMs}ms: ${ack.output.ifBlank { "Success" }}"
                } else {
                    "❌ Command Failed: ${ack.errorMessage ?: "Unknown error"}"
                }
                _lastAckMessage.value = text
            }
        }

        viewModelScope.launch {
            agentRepository.pairingResultEvent.collect { res ->
                if (!res.success) {
                    _pairingErrorMessage.value = res.errorMessage ?: "Invalid PIN provided"
                } else {
                    _pairingErrorMessage.value = null
                }
            }
        }
    }

    fun selectAgentAndConnect(agent: DiscoveredAgent) {
        _selectedAgent.value = agent
        agentRepository.connect(agent.wsUrl)
    }

    fun manualConnect(wsUrl: String) {
        val customAgent = DiscoveredAgent(
            id = "MANUAL",
            name = "Manual Agent ($wsUrl)",
            ipAddress = wsUrl,
            wsUrl = wsUrl,
            port = 8085
        )
        _selectedAgent.value = customAgent
        agentRepository.connect(wsUrl)
    }

    fun submitPin(pin: String) {
        _pairingErrorMessage.value = null
        agentRepository.sendPairRequest(pin)
    }

    fun executeRemoteCommand(command: String, pid: Int? = null) {
        val params = mutableMapOf<String, String>()
        if (pid != null) {
            params["processId"] = pid.toString()
            params["pid"] = pid.toString()
        }
        agentRepository.sendCommand(command, params)
    }

    fun disconnect() {
        agentRepository.disconnect()
        _selectedAgent.value = null
    }

    fun clearAckMessage() {
        _lastAckMessage.value = null
    }

    override fun onCleared() {
        super.onCleared()
        discoveryRepository.stopDiscovery()
        agentRepository.disconnect()
    }
}
