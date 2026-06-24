import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:online_store_mobile/chat/voice_chat_manager.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  const flutterTtsChannel = MethodChannel('flutter_tts');

  setUp(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(flutterTtsChannel, (_) async => null);
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(flutterTtsChannel, null);
  });

  test('VoiceChatManager exposes initial idle state and broadcast streams', () {
    final manager = VoiceChatManager();

    expect(manager.isListening, isFalse);
    expect(manager.recognizedTextStream.isBroadcast, isTrue);
    expect(manager.listeningStatusStream.isBroadcast, isTrue);

    manager.dispose();
  });
}
