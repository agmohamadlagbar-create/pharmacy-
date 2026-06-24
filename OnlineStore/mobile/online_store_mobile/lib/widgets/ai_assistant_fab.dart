import 'package:flutter/material.dart';
import '../chat/ai_chat_bottom_sheet.dart';

class AiAssistantFab extends StatelessWidget {
  const AiAssistantFab({super.key});

  void _showAiChat(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: false, // We handle safe areas manually for more control
      backgroundColor: Colors.transparent,
      enableDrag: true,
      // Do NOT wrap in Padding here. It causes layout overflows when the keyboard appears.
      builder: (ctx) {
        return const AiChatBottomSheet();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return FloatingActionButton(
      heroTag: 'ai_assistant_fab',
      onPressed: () => _showAiChat(context),
      backgroundColor: Theme.of(context).primaryColor,
      child: const Icon(Icons.auto_awesome, color: Colors.white),
    );
  }
}
