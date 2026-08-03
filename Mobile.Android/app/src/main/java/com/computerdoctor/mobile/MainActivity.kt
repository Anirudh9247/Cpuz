package com.computerdoctor.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.computerdoctor.mobile.model.ConnectionState
import com.computerdoctor.mobile.ui.DashboardScreen
import com.computerdoctor.mobile.ui.DiscoveryScreen
import com.computerdoctor.mobile.ui.PinPairingDialog
import com.computerdoctor.mobile.ui.theme.ComputerDoctorTheme
import com.computerdoctor.mobile.viewmodel.MainViewModel

class MainActivity : ComponentActivity() {

    private val viewModel: MainViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            ComputerDoctorTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    val agents by viewModel.discoveredAgents.collectAsState()
                    val connectionState by viewModel.connectionState.collectAsState()
                    val selectedAgent by viewModel.selectedAgent.collectAsState()
                    val latestSnapshot by viewModel.latestSnapshot.collectAsState()
                    val lastAckMessage by viewModel.lastAckMessage.collectAsState()
                    val pairingErrorMessage by viewModel.pairingErrorMessage.collectAsState()

                    when (val state = connectionState) {
                        is ConnectionState.Disconnected, is ConnectionState.Connecting, is ConnectionState.Faulted, is ConnectionState.Reconnecting -> {
                            DiscoveryScreen(
                                agents = agents,
                                onSelectAgent = { agent -> viewModel.selectAgentAndConnect(agent) },
                                onManualConnect = { url -> viewModel.manualConnect(url) }
                            )
                        }

                        is ConnectionState.ConnectedUnpaired, is ConnectionState.Pairing -> {
                            DiscoveryScreen(
                                agents = agents,
                                onSelectAgent = { agent -> viewModel.selectAgentAndConnect(agent) },
                                onManualConnect = { url -> viewModel.manualConnect(url) }
                            )

                            PinPairingDialog(
                                agentName = selectedAgent?.name ?: "Desktop Agent",
                                errorMessage = pairingErrorMessage,
                                onDismiss = { viewModel.disconnect() },
                                onSubmitPin = { pin -> viewModel.submitPin(pin) }
                            )
                        }

                        is ConnectionState.Paired, is ConnectionState.Active -> {
                            DashboardScreen(
                                agentName = selectedAgent?.name ?: "Desktop Agent",
                                snapshot = latestSnapshot,
                                onDisconnect = { viewModel.disconnect() },
                                onExecuteCommand = { cmd, pid -> viewModel.executeRemoteCommand(cmd, pid) },
                                lastAckMessage = lastAckMessage,
                                onClearAckMessage = { viewModel.clearAckMessage() }
                            )
                        }
                    }
                }
            }
        }
    }
}
