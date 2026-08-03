package com.computerdoctor.mobile.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

val DarkBg = Color(0F, 15, 23, 42)           // #0F172A (Slate 900)
val DarkCard = Color(0F, 30, 41, 59)         // #1E293B (Slate 800)
val DarkCardBorder = Color(0F, 51, 65, 85)   // #334155 (Slate 700)

val PrimaryBlue = Color(0F, 59, 130, 246)    // #3B82F6 (Blue 500)
val AccentCyan = Color(0F, 6, 182, 212)      // #06B6D4 (Cyan 500)
val SuccessGreen = Color(0F, 34, 197, 94)    // #22C55E (Green 500)
val WarningOrange = Color(0F, 245, 158, 11)  // #F59E0B (Amber 500)
val CriticalRed = Color(0F, 239, 68, 68)     // #EF4444 (Red 500)

private val DarkColorScheme = darkColorScheme(
    primary = PrimaryBlue,
    secondary = AccentCyan,
    background = DarkBg,
    surface = DarkCard,
    onPrimary = Color.White,
    onSecondary = Color.White,
    onBackground = Color.White,
    onSurface = Color.White
)

@Composable
fun ComputerDoctorTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = DarkColorScheme,
        content = content
    )
}
