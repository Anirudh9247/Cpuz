package com.computerdoctor.mobile.ui

import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.computerdoctor.mobile.ui.theme.AccentCyan
import com.computerdoctor.mobile.ui.theme.CriticalRed
import com.computerdoctor.mobile.ui.theme.DarkCard
import com.computerdoctor.mobile.ui.theme.DarkCardBorder
import com.computerdoctor.mobile.ui.theme.PrimaryBlue

@Composable
fun RemoteControlPanel(
    onExecuteCommand: (String, Int?) -> Unit
) {
    var confirmCommandName by remember { mutableStateOf<String?>(null) }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .border(1.dp, DarkCardBorder, RoundedCornerShape(12.dp)),
        colors = CardDefaults.cardColors(containerColor = DarkCard),
        shape = RoundedCornerShape(12.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.SettingsRemote, contentDescription = null, tint = AccentCyan)
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = "Remote Command Control Panel",
                    color = Color.White,
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Bold
                )
            }
            Spacer(modifier = Modifier.height(12.dp))

            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    CommandButton(
                        modifier = Modifier.weight(1f),
                        title = "Clear Temp Files",
                        icon = Icons.Default.CleaningServices,
                        color = PrimaryBlue,
                        onClick = { confirmCommandName = "CLEAR_TEMP_FILES" }
                    )
                    CommandButton(
                        modifier = Modifier.weight(1f),
                        title = "Flush DNS",
                        icon = Icons.Default.Dns,
                        color = AccentCyan,
                        onClick = { confirmCommandName = "FLUSH_DNS" }
                    )
                }

                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    CommandButton(
                        modifier = Modifier.weight(1f),
                        title = "Restart Explorer",
                        icon = Icons.Default.RestartAlt,
                        color = PrimaryBlue,
                        onClick = { confirmCommandName = "RESTART_EXPLORER" }
                    )
                    CommandButton(
                        modifier = Modifier.weight(1f),
                        title = "Kill Process",
                        icon = Icons.Default.Cancel,
                        color = CriticalRed,
                        onClick = { confirmCommandName = "KILL_PROCESS" }
                    )
                }
            }
        }
    }

    // Confirmation Dialog
    if (confirmCommandName != null) {
        val cmd = confirmCommandName!!
        AlertDialog(
            onDismissRequest = { confirmCommandName = null },
            title = { Text("Confirm Remote Action", color = Color.White) },
            text = { Text("Are you sure you want to execute command '$cmd' on the desktop agent?", color = Color.LightGray) },
            confirmButton = {
                Button(
                    onClick = {
                        onExecuteCommand(cmd, null)
                        confirmCommandName = null
                    },
                    colors = ButtonDefaults.buttonColors(containerColor = if (cmd == "KILL_PROCESS") CriticalRed else PrimaryBlue)
                ) {
                    Text("Execute")
                }
            },
            dismissButton = {
                TextButton(onClick = { confirmCommandName = null }) {
                    Text("Cancel", color = Color.Gray)
                }
            },
            containerColor = DarkCard
        )
    }
}

@Composable
fun CommandButton(
    modifier: Modifier = Modifier,
    title: String,
    icon: ImageVector,
    color: Color,
    onClick: () -> Unit
) {
    OutlinedButton(
        onClick = onClick,
        modifier = modifier.height(48.dp),
        shape = RoundedCornerShape(8.dp),
        colors = ButtonDefaults.outlinedButtonColors(contentColor = Color.White),
        border = ButtonDefaults.outlinedButtonBorder.copy(brush = androidx.compose.ui.graphics.SolidColor(color))
    ) {
        Icon(imageVector = icon, contentDescription = null, tint = color, modifier = Modifier.size(18.dp))
        Spacer(modifier = Modifier.width(6.dp))
        Text(text = title, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
    }
}
