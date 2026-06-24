import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:speech_to_text/speech_to_text.dart' as stt;
import 'package:permission_handler/permission_handler.dart';

class VoiceChatManager {
  final stt.SpeechToText _speech = stt.SpeechToText();
  final FlutterTts _flutterTts = FlutterTts();

  bool _isListening = false;
  bool _isSpeechInitialized = false;

  final _recognizedTextController = StreamController<String>.broadcast();
  final _listeningStatusController = StreamController<bool>.broadcast();

  Stream<String> get recognizedTextStream => _recognizedTextController.stream;
  Stream<bool> get listeningStatusStream => _listeningStatusController.stream;

  bool get isListening => _isListening;

  Future<void> initialize() async {
    await _flutterTts.setLanguage("en-US");
    await _flutterTts.setSpeechRate(0.5);
    await _flutterTts.setVolume(1.0);
    await _flutterTts.setPitch(1.0);
    
    // Check mic permission
    var status = await Permission.microphone.status;
    if (!status.isGranted) {
      await Permission.microphone.request();
    }

    _isSpeechInitialized = await _speech.initialize(
      onStatus: (status) {
        if (kDebugMode) print('STT Status: $status');
        if (status == 'done' || status == 'notListening') {
          _isListening = false;
          _listeningStatusController.add(false);
        }
      },
      onError: (errorNotification) {
        if (kDebugMode) print('STT Error: $errorNotification');
        _isListening = false;
        _listeningStatusController.add(false);
      },
    );
  }

  Future<void> startListening() async {
    if (!_isSpeechInitialized) {
      await initialize();
    }

    if (_isSpeechInitialized && !_isListening) {
      _isListening = true;
      _listeningStatusController.add(true);
      await _speech.listen(
        onResult: (result) {
          if (result.finalResult) {
            _recognizedTextController.add(result.recognizedWords);
            _isListening = false;
            _listeningStatusController.add(false);
          }
        },
      );
    }
  }

  Future<void> stopListening() async {
    if (_isListening) {
      await _speech.stop();
      _isListening = false;
      _listeningStatusController.add(false);
    }
  }

  Future<void> speak(String text) async {
    if (text.isNotEmpty) {
      await _flutterTts.speak(text);
    }
  }

  Future<void> stopSpeaking() async {
    await _flutterTts.stop();
  }

  void dispose() {
    _recognizedTextController.close();
    _listeningStatusController.close();
    _speech.stop();
    _flutterTts.stop();
  }
}
