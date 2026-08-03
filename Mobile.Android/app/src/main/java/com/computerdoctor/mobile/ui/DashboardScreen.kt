package com.computerdoctor.mobile.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.computerdoctor.mobile.model.HealthSnapshot
import com.computerdoctor.mobile.model.TelemetryAlert
import com.computerdoctor.mobile.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    agentName: String,
    snapshot: HealthSnapshot?,
    onDisconnect: () -> Unit,
    onExecuteCommand: (String, Int?) -> Unit,
    lastAckMessage: String?,
    onClearAckMessage: () -> Unit
) {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(DarkBg)
            .padding(16.dp)
    ) {
        // Header
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column {
                Text(
                    text = agentName,
                    color = Color.White,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold
                )
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Box(
                        modifier = Modifier
                            .size(8.dp)
                            .clip(CircleShape)
                            .background(SuccessGreen)
                    )
                    Spacer(modifier = Modifier.width(6.dp))
                    Text(
                        text = "Connected & Streaming (2s)",
                        color = SuccessGreen,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            IconButton(onClick = onDisconnect) {
                Icon(
                    imageVector = Icons.Default.PowerSettingsNew,
                    contentDescription = "Disconnect",
                    tint = CriticalRed
                )
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        if (snapshot == null) {
            Box(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxWidth(),
                contentAlignment = Alignment.Center
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    CircularProgressIndicator(color = AccentCyan)
                    Spacer(modifier = Modifier.height(12.dp))
                    Text(
                        text = "Waiting for initial hardware snapshot...",
                        color = Color.Gray,
                        fontSize = 14.sp
                    )
                }
            }
        } else {
            Column(
                modifier = Modifier
                    .weight(1f)
                    .verticalScroll(scrollState),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                // Command ACK Snack Banner if any
                if (lastAckMessage != null) {
                    Card(
                        colors = CardDefaults.cardColors(containerColor = DarkCard),
                        shape = RoundedCornerShape(12.dp),
                        modifier = Modifier.border(1.dp, PrimaryBlue, RoundedCornerShape(12.dp))
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = lastAckMessage,
                                color = Color.White,
                                fontSize = 13.sp,
                                modifier = Modifier.weight(1f)
                            )
                            IconButton(onClick = onClearAckMessage) {
                                Icon(Icons.Default.Close, contentDescription = null, tint = Color.Gray)
                            }
                        }
                    }
                }

                // Health Score Card
                HealthScoreCard(score = snapshot.healthScore, status = snapshot.healthStatus)

                // Active Alerts
                if (snapshot.alerts.isNotEmpty()) {
                    AlertsSection(alerts = snapshot.alerts)
                }

                // Grid of Telemetry Cards
                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    MetricCard(
                        modifier = Modifier.weight(1f),
                        title = "CPU Temp & Load",
                        value = "${snapshot.cpu.tempC}°C",
                        subtitle = "Load: ${snapshot.cpu.loadPercent}%",
                        icon = Icons.Default.Memory,
                        accentColor = if (snapshot.cpu.tempC > 75) CriticalRed else AccentCyan
                    )
                    MetricCard(
                        modifier = Modifier.weight(1f),
                        title = "GPU Temp",
                        value = "${snapshot.gpu.tempC}°C",
                        subtitle = "Clock: ${snapshot.gpu.clockMhz} MHz",
                        icon = Icons.Default.DeveloperBoard,
                        accentColor = if (snapshot.gpu.tempC > 80) CriticalRed else PrimaryBlue
                    )
                }

                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    MetricCard(
                        modifier = Modifier.weight(1f),
                        title = "RAM Usage",
                        value = "${snapshot.ram.usagePercent}%",
                        subtitle = "${snapshot.ram.topProcesses.size} processes listed",
                        icon = Icons.Default.Speed,
                        accentColor = if (snapshot.ram.usagePercent > 85) WarningOrange else SuccessGreen
                    )
                    MetricCard(
                        modifier = Modifier.weight(1f),
                        title = "SSD Health",
                        value = "${snapshot.ssd.healthPercent}%",
                        subtitle = "SMART: ${snapshot.ssd.smartStatus}",
                        icon = Icons.Default.Storage,
                        accentColor = SuccessGreen
                    )
                }

                // Top Memory Processes
                if (snapshot.ram.topProcesses.isNotEmpty()) {
                    TopProcessesCard(processes = snapshot.ram.topProcesses, onKillProcess = { pid ->
                        onExecuteCommand("KILL_PROCESS", pid)
                    })
                }

                // Remote Command Control Panel Section
                RemoteControlPanel(onExecuteCommand = onExecuteCommand)
            }
        }
    }
}

@Composable
fun HealthScoreCard(score: Int, status: String) {
    val scoreColor = when {
        score >= 85 -> SuccessGreen
        score >= 60 -> WarningOrange
        else -> CriticalRed
    }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .border(1.dp, scoreColor.copy(alpha = 0.5f), RoundedCornerShape(16.dp)),
        colors = CardDefaults.cardColors(containerColor = DarkCard),
        shape = RoundedCornerShape(16.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column {
                Text(text = "System Health Score", color = Color.Gray, fontSize = 14.sp)
                Text(
                    text = "$score / 100",
                    color = scoreColor,
                    fontSize = 32.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "Status: $status",
                    color = Color.White,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium
                )
            }

            Box(
                modifier = Modifier
                    .size(64.dp)
                    .clip(CircleShape)
                    .background(scoreColor.copy(alpha = 0.2f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = if (score >= 80) Icons.Default.CheckCircle else Icons.Default.Warning,
                    contentDescription = null,
                    tint = scoreColor,
                    modifier = Modifier.size(36.dp)
                )
            }
        }
    }
}

@Composable
fun MetricCard(
    modifier: Modifier = Modifier,
    title: String,
    value: String,
    subtitle: String,
    icon: ImageVector,
    accentColor: Color
) {
    Card(
        modifier = modifier.border(1.dp, DarkCardBorder, RoundedCornerShape(12.dp)),
        colors = CardDefaults.cardColors(containerColor = DarkCard),
        shape = RoundedCornerShape(12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(imageVector = icon, contentDescription = null, tint = accentColor, modifier = Modifier.size(20.dp))
                Spacer(modifier = Modifier.width(8.dp))
                Text(text = title, color = Color.Gray, fontSize = 12.sp, fontWeight = FontWeight.Medium)
            }
            Spacer(modifier = Modifier.height(10.dp))
            Text(text = value, color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold)
            Text(text = subtitle, color = Color.LightGray, fontSize = 11.sp)
        }
    }
}

@Composable
fun AlertsSection(alerts: List<TelemetryAlert>) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .border(1.dp, WarningOrange, RoundedCornerShape(12.dp)),
        colors = CardDefaults.cardColors(containerColor = DarkCard),
        shape = RoundedCornerShape(12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.Warning, contentDescription = null, tint = WarningOrange)
                Spacer(modifier = Modifier.width(8.dp))
                Text(text = "Active Warnings & Alerts", color = WarningOrange, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            }
            Spacer(modifier = Modifier.height(8.dp))
            alerts.forEach { alert ->
                Text(
                    text = "• [${alert.component}] ${alert.message}",
                    color = Color.White,
                    fontSize = 13.sp,
                    modifier = Modifier.padding(vertical = 2.dp)
                )
            }
        }
    }
}

@Composable
fun TopProcessesCard(
    processes: List<com.computerdoctor.mobile.model.TopProcess>,
    onKillProcess: (Int) -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .border(1.dp, DarkCardBorder, RoundedCornerShape(12.dp)),
        colors = CardDefaults.cardColors(containerColor = DarkCard),
        shape = RoundedCornerShape(12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(text = "Top Memory Consumers", color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            Spacer(modifier = Modifier.height(10.dp))
            processes.take(5).forEach { proc ->
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 4.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column {
                        Text(text = proc.name, color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Medium)
                        Text(text = "RAM: ${proc.mb} MB", color = Color.Gray, fontSize = 11.sp)
                    }
                    if (proc.pid > 0) {
                        IconButton(
                            onClick = { onKillProcess(proc.pid) },
                            modifier = Modifier.size(28.dp)
                        ) {
                            Icon(Icons.Default.Close, contentDescription = "Kill Process", tint = CriticalRed)
                        }
                    }
                }
            }
        }
    }
}
