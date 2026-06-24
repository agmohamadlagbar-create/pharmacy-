import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/api.dart';
import 'package:online_store_mobile/chat/ai_chat_service.dart';
import 'package:online_store_mobile/signalr_service.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  test('Api exposes a configured mobile base URL', () {
    expect(Api.baseUrl, startsWith('http://'));
    expect(Api.baseUrl, contains('lagbar.runasp.net'));
  });

  test('SignalRService stays disconnected when there is no token', () async {
    final service = SignalRService();

    await service.disconnect();
    await service.connect();

    expect(service.state, HubConnectionState.Disconnected);
  });

  test('AiChatService yields a connection error when no token exists', () async {
    final service = AiChatService();
    await service.disconnect();

    final chunks = await service.streamChat('hello').toList();

    expect(chunks, hasLength(1));
    expect(chunks.single.eventType, 'error');
    expect(chunks.single.error, contains('Not connected'));
  });

  test('AiChatStreamChunkDto maps stream JSON payloads', () {
    final chunk = AiChatStreamChunkDto.fromJson({
      'eventType': 'token',
      'conversationId': 42,
      'token': 'hello',
      'fullResponse': 'hello world',
      'error': null,
    });

    expect(chunk.eventType, 'token');
    expect(chunk.conversationId, 42);
    expect(chunk.token, 'hello');
    expect(chunk.fullResponse, 'hello world');
    expect(chunk.error, isNull);
  });
}
