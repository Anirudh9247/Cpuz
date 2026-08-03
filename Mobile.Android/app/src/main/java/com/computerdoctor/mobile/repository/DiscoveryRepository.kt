package com.computerdoctor.mobile.repository

import com.computerdoctor.mobile.model.DiscoveredAgent
import com.computerdoctor.mobile.network.UdpDiscoveryListener
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.StateFlow

interface DiscoveryRepository {
    val discoveredAgents: StateFlow<List<DiscoveredAgent>>
    fun startDiscovery(scope: CoroutineScope)
    fun stopDiscovery()
}

class DiscoveryRepositoryImpl(
    private val discoveryListener: UdpDiscoveryListener = UdpDiscoveryListener()
) : DiscoveryRepository {

    override val discoveredAgents: StateFlow<List<DiscoveredAgent>> = discoveryListener.discoveredAgents

    override fun startDiscovery(scope: CoroutineScope) {
        discoveryListener.startListening(scope)
    }

    override fun stopDiscovery() {
        discoveryListener.stopListening()
    }
}
