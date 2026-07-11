import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { api } from '../apiClient';
import AiMessageFeedback from './AiMessageFeedback';
import './ShopAiChatWidget.css';

/**
 * AI Chat Widget for Shop Owners.
 * Connects to the shop-ai-chat SignalR hub and provides 
 * natural language access to analytics, inventory, and product management AI agents.
 */
export default function ShopAiChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState([]);
  const [inputText, setInputText] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [connection, setConnection] = useState(null);
  const [conversationId, setConversationId] = useState(null);

  const messagesEndRef = useRef(null);
  const currentAssistantResponseRef = useRef('');

  useEffect(() => {
    if (isOpen && !connection) {
      const token = localStorage.getItem('token');
      if (!token) return;

      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${api.defaults.baseURL}/hubs/shop-ai-chat?access_token=${token}`)
        .withAutomaticReconnect()
        .build();

      newConnection.start()
        .then(() => {
          console.log('Connected to ShopAiChatHub');
          setConnection(newConnection);
        })
        .catch(err => console.error('ShopAI SignalR Connection Error: ', err));
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

    const userMessage = text;
    setMessages(prev => [...prev, { text: userMessage, isUser: true }]);
    setInputText('');
    setIsGenerating(true);
    currentAssistantResponseRef.current = '';

    setMessages(prev => [...prev, { text: '', isUser: false }]);

    try {
      const stream = connection.stream('SendMessage', {
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
              newMsgs[newMsgs.length - 1] = {
                ...newMsgs[newMsgs.length - 1],
                text: currentAssistantResponseRef.current,
                isUser: false
              };
              return newMsgs;
            });
          } else if (chunk.eventType === 'completed') {
            const finalText = chunk.fullResponse || currentAssistantResponseRef.current;
            setMessages(prev => {
              const newMsgs = [...prev];
              newMsgs[newMsgs.length - 1] = {
                ...newMsgs[newMsgs.length - 1],
                text: finalText,
                isUser: false,
                messageId: chunk.messageId
              };
              return newMsgs;
            });
            setIsGenerating(false);
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
            newMsgs[newMsgs.length - 1] = { text: 'Connection error. Please try again.', isUser: false };
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
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage(inputText);
    }
  };

  const quickActions = [
    { label: '📊 Revenue Report', message: 'Show my revenue this month' },
    { label: '📦 Low Stock', message: 'What products are running low on stock?' },
    { label: '🔥 Top Sellers', message: 'Show my fast-selling products this week' },
    { label: '📈 Sales Trend', message: 'Analyze my sales trend this month' },
  ];

  return (
    <div className="shop-ai-widget">
      {!isOpen && (
        <button className="shop-ai-toggle" onClick={toggleOpen} title="Shop AI Assistant">
          <span className="shop-ai-toggle-icon">🤖</span>
          <span className="shop-ai-toggle-label">AI Assistant</span>
        </button>
      )}

      {isOpen && (
        <div className="shop-ai-panel">
          <div className="shop-ai-header">
            <div className="shop-ai-header-info">
              <span className="shop-ai-header-icon">🤖</span>
              <div>
                <h3>Shop AI Assistant</h3>
                <span className="shop-ai-status">
                  {connection ? '● Connected' : '○ Connecting...'}
                </span>
              </div>
            </div>
            <button className="shop-ai-close" onClick={toggleOpen}>✕</button>
          </div>

          {messages.length === 0 && (
            <div className="shop-ai-welcome">
              <p className="shop-ai-welcome-text">
                I can help you with reports, inventory, analytics, and product management.
              </p>
              <div className="shop-ai-quick-actions">
                {quickActions.map((action, i) => (
                  <button
                    key={i}
                    className="shop-ai-quick-btn"
                    onClick={() => sendMessage(action.message)}
                    disabled={!connection || isGenerating}
                  >
                    {action.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="shop-ai-messages">
            {messages.map((msg, idx) => (
              <div key={idx} className={`shop-ai-msg ${msg.isUser ? 'shop-ai-msg-user' : 'shop-ai-msg-assistant'}`}>
                <div className="shop-ai-msg-content">
                  {msg.isUser ? msg.text : (
                    <>
                      <div className="shop-ai-msg-text" dangerouslySetInnerHTML={{
                        __html: formatResponse(msg.text)
                      }} />
                      {msg.messageId && (
                        <AiMessageFeedback conversationId={conversationId} messageId={msg.messageId} />
                      )}
                    </>
                  )}
                </div>
              </div>
            ))}
            {isGenerating && messages.length > 0 && !messages[messages.length - 1]?.text && (
              <div className="shop-ai-typing">
                <span></span><span></span><span></span>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <div className="shop-ai-input-area">
            <textarea
              className="shop-ai-input"
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask about reports, inventory, analytics..."
              disabled={!connection || isGenerating}
              rows={1}
            />
            <button
              className="shop-ai-send"
              onClick={() => sendMessage(inputText)}
              disabled={!inputText.trim() || !connection || isGenerating}
            >
              {isGenerating ? '⏳' : '➤'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

/**
 * Simple markdown-like formatting for AI responses.
 * Converts bullet points, bold, and line breaks to HTML.
 */
function formatResponse(text) {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/^- (.*)/gm, '• $1')
    .replace(/^(\d+)\. (.*)/gm, '$1. $2')
    .replace(/↑/g, '<span style="color:#22c55e">↑</span>')
    .replace(/↓/g, '<span style="color:#ef4444">↓</span>')
    .replace(/\n/g, '<br/>');
}
