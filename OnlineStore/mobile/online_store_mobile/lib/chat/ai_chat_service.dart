import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../api.dart';

class AiChatStreamChunkDto {
  final String eventType; // Token, ConversationStarted, Completed, Error
  final int? conversationId;
  final String? token;
  final String? fullResponse;
  final String? error;

  AiChatStreamChunkDto({
    required this.eventType,
    this.conversationId,
    this.token,
    this.fullResponse,
    this.error,
  });

  factory AiChatStreamChunkDto.fromJson(Map<String, dynamic> json) {
    return AiChatStreamChunkDto(
      eventType: json['eventType'] as String,
      conversationId: json['conversationId'] as int?,
      token: json['token'] as String?,
      fullResponse: json['fullResponse'] as String?,
      error: json['error'] as String?,
    );
  }
}

class AiChatService {
  static final AiChatService _instance = AiChatService._internal();
  factory AiChatService() => _instance;
  AiChatService._internal();

  HubConnection? _connection;

  Future<void> connect() async {
    if (_connection != null && _connection!.state != HubConnectionState.Disconnected) {
      return;
    }

    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    if (token == null) return;

    final hubUrl = '${Api.baseUrl}/hubs/ai-chat?access_token=$token';

    _connection = HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    try {
      await _connection!.start();
      if (kDebugMode) print('SignalR AiChatHub Connected');
    } catch (e) {
      if (kDebugMode) print('SignalR AiChatHub Connection Error: $e');
    }
  }

  Future<void> disconnect() async {
    await _connection?.stop();
    _connection = null;
  }

  Stream<AiChatStreamChunkDto> streamChat(String message, {int? conversationId}) async* {
    if (_connection?.state != HubConnectionState.Connected) {
      await connect();
    }

    if (_connection?.state != HubConnectionState.Connected) {
      yield AiChatStreamChunkDto(eventType: 'error', error: 'Not connected to chat service.');
      return;
    }

    final request = {
      'message': message,
      if (conversationId != null) 'conversationId': conversationId,
    };

    // The stream is returned by StreamChat method in the hub
    final stream = _connection!.stream('StreamChat', [request]);

    await for (final event in stream) {
      if (event == null) continue;
      // Depending on the signalr package version, event might be Map or List
      final data = event is List ? (event.isNotEmpty ? event.first : null) : event;
      if (data is Map<String, dynamic>) {
        yield AiChatStreamChunkDto.fromJson(data);
      }
    }
  }
}
