import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { api } from '../apiClient';
import AiMessageFeedback from './AiMessageFeedback';
import './AdminAiChatWidget.css';

export default function AdminAiChatWidget() {
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
        .withUrl(`${api.defaults.baseURL}/hubs/admin-ai-chat?access_token=${token}`)
        .withAutomaticReconnect()
        .build();

      newConnection.start()
        .then(() => {
          console.log('Connected to AdminAiChatHub');
          setConnection(newConnection);
        })
        .catch(err => console.error('AdminAI SignalR Connection Error: ', err));
    }
  }, [isOpen]);

  const toggleOpen = () => setIsOpen(!isOpen);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isGenerating]);

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
    { label: ' Overview', message: 'Show me a platform overview with total users, products, orders, and revenue' },
    { label: ' Recent Orders', message: 'Show recent orders and their statuses' },
    { label: ' Low Stock Products', message: 'Which products are running low on stock across all stores?' },
    { label: ' Pending Approvals', message: 'Show products pending approval' },
  ];

  return (
    <div className="admin-ai-widget">
      {!isOpen && (
        <button className="admin-ai-toggle" onClick={toggleOpen} title="Admin AI Assistant">
          <span className="admin-ai-toggle-icon">🛡️</span>
          <span className="admin-ai-toggle-label">Admin AI</span>
        </button>
      )}

      {isOpen && (
        <div className="admin-ai-panel">
          <div className="admin-ai-header">
            <div className="admin-ai-header-info">
              <span className="admin-ai-header-icon">🛡️</span>
              <div>
                <h3>Admin AI Assistant</h3>
                <span className="admin-ai-status">
                  {connection ? '● Connected' : '○ Connecting...'}
                </span>
              </div>
            </div>
            <button className="admin-ai-close" onClick={toggleOpen}>✕</button>
          </div>

          {messages.length === 0 && (
            <div className="admin-ai-welcome">
              <p className="admin-ai-welcome-text">
                I can help you manage the platform — overview, orders, products, users, and more.
              </p>
              <div className="admin-ai-quick-actions">
                {quickActions.map((action, i) => (
                  <button
                    key={i}
                    className="admin-ai-quick-btn"
                    onClick={() => sendMessage(action.message)}
                    disabled={!connection || isGenerating}
                  >
                    {action.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="admin-ai-messages">
            {messages.map((msg, idx) => (
              <div key={idx} className={`admin-ai-msg ${msg.isUser ? 'admin-ai-msg-user' : 'admin-ai-msg-assistant'}`}>
                <div className="admin-ai-msg-content">
                  {msg.isUser ? msg.text : (
                    <>
                      <div className="admin-ai-msg-text" dangerouslySetInnerHTML={{
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
              <div className="admin-ai-typing">
                <span></span><span></span><span></span>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <div className="admin-ai-input-area">
            <textarea
              className="admin-ai-input"
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask about platform management..."
              disabled={!connection || isGenerating}
              rows={1}
            />
            <button
              className="admin-ai-send"
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
