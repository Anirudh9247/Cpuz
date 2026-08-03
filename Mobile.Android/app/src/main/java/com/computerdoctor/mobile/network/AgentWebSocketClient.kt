package com.computerdoctor.mobile.network

import com.computerdoctor.mobile.model.*
import com.google.gson.Gson
import com.google.gson.JsonParser
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import okhttp3.*
import okio.ByteString
import java.util.concurrent.TimeUnit
import kotlin.math.min
import kotlin.math.pow

class AgentWebSocketClient(
    private val scope: CoroutineScope
) {
    private val gson = Gson()
    private val client = OkHttpClient.Builder()
        .readTimeout(10, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .pingInterval(5, TimeUnit.SECONDS)
        .build()

    private var webSocket: WebSocket? = null

    private val _connectionState = MutableStateFlow<ConnectionState>(ConnectionState.Disconnected)
    val connectionState: StateFlow<ConnectionState> = _connectionState.asStateFlow()

    private val _latestSnapshot = MutableStateFlow<HealthSnapshot?>(null)
    val latestSnapshot: StateFlow<HealthSnapshot?> = _latestSnapshot.asStateFlow()

    private val _commandAckEvent = MutableSharedFlow<CommandAckPayload>()
    val commandAckEvent: SharedFlow<CommandAckPayload> = _commandAckEvent.asSharedFlow()

    private val _pairingResultEvent = MutableSharedFlow<PairResponsePayload>()
    val pairingResultEvent: SharedFlow<PairResponsePayload> = _pairingResultEvent.asSharedFlow()

    private var activeWsUrl: String? = null
    private var sessionToken: String? = null
    private var isUserInitiatedDisconnect = false

    private var heartbeatJob: Job? = null
    private var reconnectJob: Job? = null

    fun connect(wsUrl: String) {
        activeWsUrl = wsUrl
        isUserInitiatedDisconnect = false
        reconnectJob?.cancel()

        if (_connectionState.value == ConnectionState.Connecting || _connectionState.value == ConnectionState.Active) {
            return
        }

        _connectionState.value = ConnectionState.Connecting
        val request = Request.Builder().url(wsUrl).build()
        webSocket = client.newWebSocket(request, createWebSocketListener())
    }

    fun disconnect() {
        isUserInitiatedDisconnect = true
        reconnectJob?.cancel()
        reconnectJob = null
        heartbeatJob?.cancel()
        heartbeatJob = null

        webSocket?.close(1000, "User requested disconnect")
        webSocket = null
        sessionToken = null
        _connectionState.value = ConnectionState.Disconnected
    }

    fun sendPairRequest(pin: String) {
        if (webSocket == null) return

        _connectionState.value = ConnectionState.Pairing
        val pairPayload = PairRequestPayload(pin = pin)
        val envelope = NetworkEnvelope(
            messageType = "PAIR_REQUEST",
            payload = pairPayload
        )

        val json = gson.toJson(envelope)
        webSocket?.send(json)
    }

    fun sendCommand(commandName: String, parameters: Map<String, String> = emptyMap()) {
        if (webSocket == null || _connectionState.value != ConnectionState.Active) return

        val cmdPayload = CommandRequestPayload(
            command = commandName,
            sessionToken = sessionToken ?: "",
            parameters = parameters
        )

        val envelope = NetworkEnvelope(
            messageType = "COMMAND",
            sessionId = sessionToken,
            payload = cmdPayload
        )

        val json = gson.toJson(envelope)
        webSocket?.send(json)
    }

    private fun createWebSocketListener() = object : WebSocketListener() {
        override fun onOpen(webSocket: WebSocket, response: Response) {
            _connectionState.value = ConnectionState.ConnectedUnpaired
            startHeartbeatTimer()
        }

        override fun onMessage(webSocket: WebSocket, text: String) {
            handleIncomingMessage(text)
        }

        override fun onMessage(webSocket: WebSocket, bytes: ByteString) {
            handleIncomingMessage(bytes.utf8())
        }

        override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
            _connectionState.value = ConnectionState.Disconnected
            scheduleReconnection()
        }

        override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
            _connectionState.value = ConnectionState.Disconnected
            scheduleReconnection()
        }

        override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
            _connectionState.value = ConnectionState.Faulted(t.message ?: "WebSocket connection failed")
            scheduleReconnection()
        }
    }

    private fun scheduleReconnection() {
        if (isUserInitiatedDisconnect || activeWsUrl == null) return

        reconnectJob?.cancel()
        reconnectJob = scope.launch(Dispatchers.IO) {
            var attempt = 1
            val maxAttempts = 5

            while (attempt <= maxAttempts && !isUserInitiatedDisconnect) {
                _connectionState.value = ConnectionState.Reconnecting(attempt, maxAttempts)
                val backoffMs = min(1000L * (2.0.pow(attempt.toDouble()).toLong()), 16000L)
                delay(backoffMs)

                if (isUserInitiatedDisconnect) break

                val url = activeWsUrl ?: break
                val request = Request.Builder().url(url).build()
                webSocket = client.newWebSocket(request, createWebSocketListener())
                
                // Give connection time to evaluate onOpen
                delay(3000)
                if (_connectionState.value is ConnectionState.ConnectedUnpaired || _connectionState.value is ConnectionState.Active) {
                    break
                }
                attempt++
            }

            if (_connectionState.value is ConnectionState.Reconnecting) {
                _connectionState.value = ConnectionState.Faulted("Max reconnect attempts exceeded")
            }
        }
    }

    private fun handleIncomingMessage(jsonText: String) {
        try {
            val jsonObject = JsonParser.parseString(jsonText).asJsonObject

            // Check versioned envelope schema (messageType or Type)
            val messageTypeStr = when {
                jsonObject.has("messageType") -> jsonObject.get("messageType").asString
                jsonObject.has("Type") -> jsonObject.get("Type").asString
                jsonObject.has("type") -> jsonObject.get("type").asString
                else -> null
            }

            if (messageTypeStr != null) {
                when (messageTypeStr.uppercase()) {
                    "PAIR_RESPONSE" -> {
                        val payloadObj = jsonObject.getAsJsonObject("payload") ?: jsonObject.getAsJsonObject("Payload")
                        val pairResponse = gson.fromJson(payloadObj, PairResponsePayload::class.java)
                        if (pairResponse.success) {
                            sessionToken = pairResponse.sessionToken
                            _connectionState.value = ConnectionState.Active
                        } else {
                            _connectionState.value = ConnectionState.ConnectedUnpaired
                        }
                        scope.launch { _pairingResultEvent.emit(pairResponse) }
                    }
                    "TELEMETRY" -> {
                        val payloadObj = jsonObject.getAsJsonObject("payload") ?: jsonObject.getAsJsonObject("Payload")
                        val snapshot = gson.fromJson(payloadObj, HealthSnapshot::class.java)
                        _latestSnapshot.value = snapshot
                        if (_connectionState.value == ConnectionState.ConnectedUnpaired && sessionToken != null) {
                            _connectionState.value = ConnectionState.Active
                        }
                    }
                    "COMMAND_ACK" -> {
                        val payloadObj = jsonObject.getAsJsonObject("payload") ?: jsonObject.getAsJsonObject("Payload")
                        val ack = gson.fromJson(payloadObj, CommandAckPayload::class.java)
                        scope.launch { _commandAckEvent.emit(ack) }
                    }
                    "PING" -> {
                        val pong = NetworkEnvelope(messageType = "PONG", payload = mapOf("status" to "PONG"))
                        webSocket?.send(gson.toJson(pong))
                    }
                }
            } else if (jsonObject.has("healthScore") || jsonObject.has("cpu")) {
                val snapshot = gson.fromJson(jsonObject, HealthSnapshot::class.java)
                _latestSnapshot.value = snapshot
                if (_connectionState.value == ConnectionState.ConnectedUnpaired) {
                    _connectionState.value = ConnectionState.Active
                }
            }
        } catch (e: Exception) {
            // Ignore parse errors
        }
    }

    private fun startHeartbeatTimer() {
        heartbeatJob?.cancel()
        heartbeatJob = scope.launch(Dispatchers.IO) {
            while (_connectionState.value != ConnectionState.Disconnected && _connectionState.value !is ConnectionState.Faulted) {
                delay(10000)
                if (webSocket != null) {
                    val pingEnvelope = NetworkEnvelope(messageType = "PING", payload = mapOf("status" to "PING"))
                    webSocket?.send(gson.toJson(pingEnvelope))
                }
            }
        }
    }
}
