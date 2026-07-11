import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useVoice } from '../hooks/useVoice';
import { api } from '../apiClient';
import RecommendationCards from './RecommendationCards';
import AiMessageFeedback from './AiMessageFeedback';
import './AiChatWidget.css';

export default function AiChatWidget(props) {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState([]);
  const [inputText, setInputText] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [connection, setConnection] = useState(null);
  const [conversationId, setConversationId] = useState(null);

  const messagesEndRef = useRef(null);
  const currentAssistantResponseRef = useRef('');

  const {
    isListening,
    recognizedText,
    startListening,
    stopListening,
    speak,
    stopSpeaking,
    setRecognizedText
  } = useVoice();

  useEffect(() => {
    if (recognizedText) {
      sendMessage(recognizedText);
      setRecognizedText('');
    }
  }, [recognizedText]);

  useEffect(() => {
    if (isOpen && !connection) {
      const token = localStorage.getItem('token');
      if (!token) return;

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${api.defaults.baseURL}/hubs/ai-chat?access_token=${token}`)
        .withAutomaticReconnect()
        .build();

      newConnection.start()
        .then(() => {
          console.log('Connected to AiChatHub');
          setConnection(newConnection);
        })
        .catch(err => console.error('SignalR Connection Error: ', err));
    }
  }, [isOpen]);

  useEffect(() => {
    scrollToBottom();
  }, [messages, isGenerating]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const toggleOpen = () => setIsOpen(!isOpen);

  const sendMessage = async (text) => {
    if (!text.trim() || !connection || isGenerating) return;

    stopSpeaking();

    const userMessage = text;
    setMessages(prev => [...prev, { text: userMessage, isUser: true }]);
    setInputText('');
    setIsGenerating(true);
    currentAssistantResponseRef.current = '';

    setMessages(prev => [...prev, { text: '', isUser: false }]);

    try {
      const stream = connection.stream('StreamChat', {
        message: userMessage,
        conversationId: conversationId
      });

      stream.subscribe({
        next: (chunk) => {
          if (chunk.eventType === 'conversation_started') {
            setConversationId(chunk.conversationId);
          } else if (chunk.eventType === 'token') {
            currentAssistantResponseRef.current += chunk.token || '';
            setMessages(prev => {
              const newMsgs = [...prev];
              const lastMsg = newMsgs[newMsgs.length - 1];
              newMsgs[newMsgs.length - 1] = {
                ...lastMsg,
                text: currentAssistantResponseRef.current,
                isUser: false,
                recommendations: lastMsg ? lastMsg.recommendations : undefined
              };
              return newMsgs;
            });
          } else if (chunk.eventType === 'completed') {
            const finalText = chunk.fullResponse || currentAssistantResponseRef.current;
            setMessages(prev => {
              const newMsgs = [...prev];
              const lastMsg = newMsgs[newMsgs.length - 1];
              newMsgs[newMsgs.length - 1] = {
                ...lastMsg,
                text: finalText,
                isUser: false,
                messageId: chunk.messageId,
                recommendations: lastMsg ? lastMsg.recommendations : undefined
              };
              return newMsgs;
            });
            setIsGenerating(false);
            speak(finalText);
          } else if (chunk.eventType === 'cart_updated') {
            console.log('[AiChatWidget] Received cart_updated event from SignalR');
            if (props.onCartUpdate) props.onCartUpdate();
            if (props.onAddToCart) props.onAddToCart();
          } else if (chunk.eventType === 'recommendations') {
            try {
              const recData = JSON.parse(chunk.fullResponse);
              setMessages(prev => {
                const newMsgs = [...prev];
                const lastMsg = newMsgs[newMsgs.length - 1];
                newMsgs[newMsgs.length - 1] = { ...lastMsg, recommendations: recData };
                return newMsgs;
              });
            } catch (parseErr) {
              console.error('[AiChatWidget] Failed to parse recommendations:', parseErr);
            }
          } else if (chunk.eventType === 'error') {
            setMessages(prev => {
              const newMsgs = [...prev];
              newMsgs[newMsgs.length - 1] = { text: `Error: ${chunk.error}`, isUser: false };
              return newMsgs;
            });
            setIsGenerating(false);
          }
        },
        error: (err) => {
          console.error(err);
          setMessages(prev => {
            const newMsgs = [...prev];
            newMsgs[newMsgs.length - 1] = { text: 'Connection error.', isUser: false };
            return newMsgs;
          });
          setIsGenerating(false);
        },
        complete: () => {
          setIsGenerating(false);
        }
      });
    } catch (e) {
      console.error(e);
      setIsGenerating(false);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') {
      sendMessage(inputText);
    }
  };

  const handleMicToggle = () => {
    if (isListening) {
      stopListening();
    } else {
      startListening();
    }
  };

  return (
    <div className="ai-chat-widget">
      {isOpen && (
        <div className="ai-chat-window">
          <div className="ai-chat-header">
            <span className="ai-chat-header-title">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M12 2a10 10 0 0 1 10 10v4a2 2 0 0 1-2 2h-4a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2h4a10 10 0 1 0-14 0h4a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4a10 10 0 0 1 10-10z"></path>
              </svg>
              AI Assistant
            </span>
            <button className="ai-chat-close" onClick={toggleOpen}>&times;</button>
          </div>
          <div className="ai-chat-messages">
            {messages.length === 0 && (
              <div style={{ textAlign: 'center', color: '#888', marginTop: 20 }}>
                Hi! How can I help you today?
              </div>
            )}
            {messages.map((msg, idx) => (
              <div key={idx} className={`ai-message ${msg.isUser ? 'user' : 'assistant'}${msg.recommendations ? ' has-recommendations' : ''}`}>
                {msg.text || (isGenerating && idx === messages.length - 1 ? '...' : '')}
                {msg.recommendations && (
                  <RecommendationCards
                    data={msg.recommendations}
                    onAddToCart={(productId, qty) => {
                      // Use the already-imported api client to add to cart
                      api.post('/api/cart/items', { productId, quantity: qty })
                        .then(() => {
                          if (props.onCartUpdate) props.onCartUpdate();
                          if (props.onAddToCart) props.onAddToCart();
                        })
                        .catch(err => console.error('Failed to add to cart:', err));
                    }}
                    onNavigateProduct={(id) => { window.location.href = `/product/${id}`; }}
                  />
                )}
                {!msg.isUser && msg.text && msg.messageId && (
                  <AiMessageFeedback conversationId={conversationId} messageId={msg.messageId} />
                )}
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>
          <div className="ai-chat-input-area">
            <input 
              type="text" 
              className="ai-chat-input" 
              placeholder="Type a message..." 
              value={inputText}
              onChange={e => setInputText(e.target.value)}
              onKeyDown={handleKeyDown}
            />
            <button className={`ai-mic-btn ${isListening ? 'listening' : ''}`} onClick={handleMicToggle}>
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3Z"></path>
                <path d="M19 10v2a7 7 0 0 1-14 0v-2"></path>
                <line x1="12" x2="12" y1="19" y2="22"></line>
              </svg>
            </button>
            <button className="ai-send-btn" onClick={() => sendMessage(inputText)}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <line x1="22" x2="11" y1="2" y2="13"></line>
                <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
              </svg>
            </button>
          </div>
        </div>
      )}

      {!isOpen && (
        <button className="ai-fab" onClick={toggleOpen}>
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 2a10 10 0 0 1 10 10v4a2 2 0 0 1-2 2h-4a2 2 0 0 1-2-2v-4a2 2 0 0 1 2-2h4a10 10 0 1 0-14 0h4a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-4a10 10 0 0 1 10-10z"></path>
          </svg>
        </button>
      )}
    </div>
  );
}
