import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'ai_chat_service.dart';
import 'voice_chat_manager.dart';
import '../widgets/recommendation_cards_widget.dart';

class ChatMessage {
  final String text;
  final bool isUser;
  final bool isTyping;
  final Map<String, dynamic>? recommendations;
  ChatMessage({
    required this.text,
    required this.isUser,
    this.isTyping = false,
    this.recommendations,
  });
}

class AiChatBottomSheet extends StatefulWidget {
  const AiChatBottomSheet({super.key});

  @override
  State<AiChatBottomSheet> createState() => _AiChatBottomSheetState();
}

class _AiChatBottomSheetState extends State<AiChatBottomSheet>
    with TickerProviderStateMixin {
  final _aiChatService = AiChatService();
  final _voiceManager = VoiceChatManager();
  final _textController = TextEditingController();
  final _scrollController = ScrollController();
  final _focusNode = FocusNode();

  final List<ChatMessage> _messages = [];
  bool _isListening = false;
  bool _isGenerating = false;

  String _accountType = 'User';
  String _currentAssistantResponse = '';
  StreamSubscription? _voiceTextSub;
  StreamSubscription? _voiceStatusSub;
  int? _conversationId;

  late AnimationController _dotAnimController;

  @override
  void initState() {
    super.initState();
    _loadAccountType();
    _aiChatService.connect();
    _voiceManager.initialize();

    _dotAnimController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat();

    _voiceTextSub = _voiceManager.recognizedTextStream.listen((text) {
      if (text.isNotEmpty) {
        _sendMessage(text);
      }
    });

    _voiceStatusSub = _voiceManager.listeningStatusStream.listen((isListening) {
      if (mounted) {
        setState(() {
          _isListening = isListening;
        });
      }
    });
  }

  @override
  void dispose() {
    _dotAnimController.dispose();
    _voiceTextSub?.cancel();
    _voiceStatusSub?.cancel();
    _voiceManager.dispose();
    _textController.dispose();
    _scrollController.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  Future<void> _loadAccountType() async {
    final prefs = await SharedPreferences.getInstance();
    if (mounted) {
      setState(() {
        _accountType = prefs.getString('accountType') ?? 'User';
      });
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _sendMessage(String text) async {
    final trimmed = text.trim();
    if (trimmed.isEmpty || _isGenerating) return;

    _voiceManager.stopSpeaking();

    setState(() {
      _messages.add(ChatMessage(text: trimmed, isUser: true));
      _isGenerating = true;
      _currentAssistantResponse = '';
      _messages.add(ChatMessage(text: '', isUser: false, isTyping: true));
    });

    _scrollToBottom();
    _textController.clear();

    try {
      final stream =
          _aiChatService.streamChat(trimmed, conversationId: _conversationId);

      await for (final chunk in stream) {
        if (!mounted) break;

        if (chunk.eventType == 'conversation_started') {
          _conversationId = chunk.conversationId;
        } else if (chunk.eventType == 'token') {
          setState(() {
            _currentAssistantResponse += chunk.token ?? '';
            final lastMsg = _messages[_messages.length - 1];
            _messages[_messages.length - 1] = ChatMessage(
              text: _currentAssistantResponse,
              isUser: false,
              recommendations: lastMsg.recommendations,
            );
          });
          _scrollToBottom();
        } else if (chunk.eventType == 'completed') {
          final rawText = chunk.fullResponse ?? _currentAssistantResponse;
          final finalText = rawText.trim().isEmpty
              ? 'Sorry, I could not process that. Please try again.'.tr()
              : rawText.trim();
          setState(() {
            _currentAssistantResponse = '';
            final lastMsg = _messages[_messages.length - 1];
            _messages[_messages.length - 1] = ChatMessage(
              text: finalText,
              isUser: false,
              recommendations: lastMsg.recommendations,
            );
            _isGenerating = false;
          });
          _voiceManager.speak(finalText);
          _scrollToBottom();
        } else if (chunk.eventType == 'error') {
          final errText = chunk.error ?? 'An error occurred.'.tr();
          setState(() {
            _messages[_messages.length - 1] =
                ChatMessage(text: '${'Error:'.tr()} $errText', isUser: false);
            _isGenerating = false;
          });
        } else if (chunk.eventType == 'recommendations') {
          // Parse recommendation data and attach to current message
          try {
            final recData = jsonDecode(chunk.fullResponse ?? '{}') as Map<String, dynamic>;
            setState(() {
              final lastMsg = _messages[_messages.length - 1];
              _messages[_messages.length - 1] = ChatMessage(
                text: lastMsg.text,
                isUser: false,
                recommendations: recData,
              );
            });
            _scrollToBottom();
          } catch (e) {
            // Silently ignore parse errors — the AI text response still works
          }
        } else if (chunk.eventType == 'cart_updated') {
          // Cart updated silently — stream continues with AI follow-up token.
        }
      }
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isGenerating = false;
        if (_messages.isNotEmpty &&
            !_messages.last.isUser &&
            _messages.last.text.isEmpty) {
          _messages[_messages.length - 1] = ChatMessage(
              text: 'Connection error. Please try again.'.tr(), isUser: false);
        } else {
          _messages.add(ChatMessage(
              text: 'Connection error. Please try again.'.tr(), isUser: false));
        }
      });
    }
  }

  void _toggleListening() {
    if (_isListening) {
      _voiceManager.stopListening();
    } else {
      _voiceManager.startListening();
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // We add padding INSIDE the container so its total outer height remains fixed.
    // This perfectly prevents layout overflow and glitching when the keyboard appears.
    final screenHeight = MediaQuery.sizeOf(context).height;
    final topPadding = MediaQuery.paddingOf(context).top;

    return Container(
      // Height = screen minus status bar minus a small gap so the sheet
      // doesn't cover the very top of the screen.
      height: screenHeight - topPadding - 24,
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      decoration: BoxDecoration(
        color: theme.scaffoldBackgroundColor,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.15),
            blurRadius: 24,
            offset: const Offset(0, -4),
          ),
        ],
      ),
      child: Column(
        children: [
          // ── Drag handle ────────────────────────────────────────────────
          Center(
            child: Container(
              margin: const EdgeInsets.only(top: 10, bottom: 4),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: theme.dividerColor,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),

          // ── Header ─────────────────────────────────────────────────────
          _buildHeader(theme),

          // ── Message list ───────────────────────────────────────────────
          Expanded(
            child: _messages.isEmpty
                ? _buildWelcomeScreen(theme)
                : ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                    itemCount: _messages.length,
                    itemBuilder: (context, index) {
                      final msg = _messages[index];
                      return _buildMessageBubble(msg, theme);
                    },
                  ),
          ),

          // ── Input area — SafeArea handles the system nav bar ───────────
          _buildInputArea(theme),
        ],
      ),
    );
  }

  Widget _buildWelcomeScreen(ThemeData theme) {
    IconData mainIcon = Icons.auto_awesome;
    String greeting = 'Hi! How can I help you today?'.tr();
    String description =
        'Search products, track orders, manage your cart and more.'.tr();
    List<String> promptChips = [
      'Show me popular beverages',
      'Where is my order?',
      'Recommend top electronics',
      'Help me find deals'
    ];

    if (_accountType == 'Shop') {
      mainIcon = Icons.storefront;
      greeting = 'Welcome Shop Owner!'.tr();
      description =
          'Ask about revenue, inventory restock alerts, managing products, or sales analytics.'.tr();
      promptChips = [
        'Show my sales analytics',
        'Which products need restock?',
        'How can I boost my revenue?',
        'Summarize recent orders'
      ];
    } else if (_accountType == 'Admin') {
      mainIcon = Icons.admin_panel_settings;
      greeting = 'Welcome Platform Admin!'.tr();
      description =
          'Ask about store approvals, platform analytics, moderation, and system health.'.tr();
      promptChips = [
        'Show platform revenue report',
        'List pending store approvals',
        'Check fraud and security alerts',
        'Summarize active users'
      ];
    }

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: theme.primaryColor.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(mainIcon, size: 40, color: theme.primaryColor),
            ),
            const SizedBox(height: 16),
            Text(
              greeting,
              style: theme.textTheme.titleMedium
                  ?.copyWith(fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              description,
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium
                  ?.copyWith(color: theme.hintColor),
            ),
            const SizedBox(height: 24),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              alignment: WrapAlignment.center,
              children: promptChips.map((chipText) {
                return ActionChip(
                  avatar: const Icon(Icons.bolt, size: 16),
                  label: Text(chipText.tr()),
                  onPressed: () => _sendMessage(chipText),
                  backgroundColor: theme.primaryColor.withValues(alpha: 0.08),
                  side: BorderSide.none,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(20),
                  ),
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(ThemeData theme) {
    IconData icon = Icons.auto_awesome;
    String title = 'Shopping Assistant'.tr();
    String subtitle = 'Customer AI Agent'.tr();

    if (_accountType == 'Shop') {
      icon = Icons.storefront;
      title = 'Shop AI Agent'.tr();
      subtitle = 'Shop Management & Analytics'.tr();
    } else if (_accountType == 'Admin') {
      icon = Icons.admin_panel_settings;
      title = 'Admin AI Agent'.tr();
      subtitle = 'Platform Admin & Oversight'.tr();
    }

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 4, 8, 8),
      decoration: BoxDecoration(
        border: Border(
          bottom: BorderSide(color: theme.dividerColor.withValues(alpha: 0.15)),
        ),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: theme.primaryColor.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: theme.primaryColor, size: 20),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.bold),
                ),
                Text(
                  _isGenerating ? 'Thinking...'.tr() : subtitle,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: _isGenerating ? theme.primaryColor : theme.hintColor,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close),
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      ),
    );
  }

  Widget _buildMessageBubble(ChatMessage msg, ThemeData theme) {
    final isUser = msg.isUser;
    final hasRecs = msg.recommendations != null;

    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: EdgeInsets.only(
          bottom: 10,
          left: isUser ? 60 : 0,
          right: isUser ? 0 : (hasRecs ? 0 : 60),
        ),
        padding: hasRecs
            ? const EdgeInsets.only(left: 14, right: 14, top: 10, bottom: 4)
            : const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          gradient: isUser
              ? LinearGradient(
                  colors: [
                    theme.primaryColor,
                    theme.primaryColor.withValues(alpha: 0.85),
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                )
              : null,
          color: isUser
              ? null
              : (hasRecs ? Colors.transparent : theme.cardColor),
          borderRadius: BorderRadius.circular(18).copyWith(
            bottomRight: isUser
                ? const Radius.circular(4)
                : const Radius.circular(18),
            bottomLeft: !isUser
                ? const Radius.circular(4)
                : const Radius.circular(18),
          ),
          border: isUser || hasRecs
              ? null
              : Border.all(color: theme.dividerColor.withValues(alpha: 0.15)),
          boxShadow: hasRecs
              ? null
              : [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.04),
                    blurRadius: 4,
                    offset: const Offset(0, 2),
                  )
                ],
        ),
        child: msg.isTyping
            ? _buildTypingIndicator(theme)
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (msg.text.isNotEmpty)
                    SelectableText(
                      msg.text,
                      style: TextStyle(
                        color: isUser
                            ? Colors.white
                            : theme.textTheme.bodyLarge?.color,
                        fontSize: 14.5,
                        height: 1.45,
                      ),
                    ),
                  if (hasRecs) ...[                    
                    if (msg.text.isNotEmpty) const SizedBox(height: 8),
                    RecommendationCardsWidget(
                      data: msg.recommendations!,
                      onNavigateProduct: (productId) {
                        Navigator.of(context).pop(); // Close chat
                        // Navigate handled by parent
                      },
                    ),
                  ],
                ],
              ),
      ),
    );
  }

  Widget _buildTypingIndicator(ThemeData theme) {
    return AnimatedBuilder(
      animation: _dotAnimController,
      builder: (context, child) {
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: List.generate(3, (i) {
            final t = (_dotAnimController.value - i * 0.15).clamp(0.0, 1.0);
            final opacity = (t < 0.5 ? t * 2 : (1 - t) * 2).clamp(0.3, 1.0);
            return Container(
              margin: const EdgeInsets.symmetric(horizontal: 2),
              width: 7,
              height: 7,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: theme.primaryColor.withValues(alpha: opacity),
              ),
            );
          }),
        );
      },
    );
  }

  Widget _buildInputArea(ThemeData theme) {
    return SafeArea(
      // top: false — only protect the bottom (system nav bar).
      // When the keyboard is open, viewInsets > 0 so SafeArea bottom = 0,
      // which is correct — the keyboard itself provides the gap.
      top: false,
      child: Container(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 8),
        decoration: BoxDecoration(
          color: theme.scaffoldBackgroundColor,
          border: Border(
            top: BorderSide(
              color: theme.dividerColor.withValues(alpha: 0.2),
            ),
          ),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            // Mic button
            GestureDetector(
              onTap: _toggleListening,
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 250),
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: _isListening
                      ? Colors.red
                      : theme.primaryColor.withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  _isListening ? Icons.mic : Icons.mic_none,
                  size: 22,
                  color: _isListening ? Colors.white : theme.primaryColor,
                ),
              ),
            ),
            const SizedBox(width: 8),
            // Text field
            Expanded(
              child: Container(
                constraints: const BoxConstraints(maxHeight: 120),
                decoration: BoxDecoration(
                  color: theme.cardColor,
                  borderRadius: BorderRadius.circular(24),
                  border: Border.all(
                      color: theme.dividerColor.withValues(alpha: 0.25)),
                ),
                child: TextField(
                  controller: _textController,
                  focusNode: _focusNode,
                  maxLines: null,
                  textInputAction: TextInputAction.send,
                  decoration: InputDecoration(
                    hintText: 'Type a message...'.tr(),
                    hintStyle: TextStyle(color: theme.hintColor),
                    border: InputBorder.none,
                    contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 10),
                  ),
                  onSubmitted: (val) {
                    if (val.trim().isNotEmpty) _sendMessage(val);
                  },
                ),
              ),
            ),
            const SizedBox(width: 8),
            // Send button
            AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              decoration: BoxDecoration(
                color: _isGenerating
                    ? theme.disabledColor
                    : theme.primaryColor,
                shape: BoxShape.circle,
              ),
              child: IconButton(
                icon: _isGenerating
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          valueColor:
                              AlwaysStoppedAnimation<Color>(Colors.white),
                        ),
                      )
                    : const Icon(Icons.send, color: Colors.white, size: 20),
                onPressed: _isGenerating
                    ? null
                    : () {
                        if (_textController.text.trim().isNotEmpty) {
                          _sendMessage(_textController.text);
                        }
                      },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
