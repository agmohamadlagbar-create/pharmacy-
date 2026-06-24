import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AiChatWidget from '../AiChatWidget'

const mocks = vi.hoisted(() => {
  const voice = {
    isListening: false,
    recognizedText: '',
    startListening: vi.fn(),
    stopListening: vi.fn(),
    speak: vi.fn(),
    stopSpeaking: vi.fn(),
    setRecognizedText: vi.fn()
  }

  const connection = {
    start: vi.fn(),
    stream: vi.fn()
  }

  return {
    voice,
    connection,
    withUrl: vi.fn(),
    withAutomaticReconnect: vi.fn(),
    build: vi.fn()
  }
})

vi.mock('../../hooks/useVoice', () => ({
  useVoice: () => mocks.voice
}))

vi.mock('../../apiClient', () => ({
  api: {
    defaults: {
      baseURL: 'https://api.test'
    }
  }
}))

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl(url) {
      mocks.withUrl(url)
      return this
    }

    withAutomaticReconnect() {
      mocks.withAutomaticReconnect()
      return this
    }

    build() {
      mocks.build()
      return mocks.connection
    }
  }
}))

describe('AiChatWidget', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
    Element.prototype.scrollIntoView = vi.fn()
    mocks.voice.isListening = false
    mocks.voice.recognizedText = ''
    mocks.connection.start.mockResolvedValue()
    mocks.connection.stream.mockReturnValue({
      subscribe: ({ next, complete }) => {
        next({ eventType: 'conversation_started', conversationId: 'conv-1' })
        next({ eventType: 'token', token: 'Fresh ' })
        next({ eventType: 'token', token: 'bread' })
        next({ eventType: 'completed', fullResponse: 'Fresh bread' })
        complete()
      }
    })
  })

  it('opens the assistant and connects to SignalR when a token exists', async () => {
    localStorage.setItem('token', 'abc123')
    const user = userEvent.setup()

    render(<AiChatWidget />)
    await user.click(screen.getByRole('button'))

    expect(await screen.findByText('AI Assistant')).toBeInTheDocument()
    expect(screen.getByText('Hi! How can I help you today?')).toBeInTheDocument()

    await waitFor(() => {
      expect(mocks.connection.start).toHaveBeenCalledTimes(1)
    })

    expect(mocks.withUrl).toHaveBeenCalledWith(
      'https://api.test/hubs/ai-chat?access_token=abc123'
    )
  })

  it('streams a user message and speaks the completed assistant response', async () => {
    localStorage.setItem('token', 'abc123')
    const user = userEvent.setup()

    render(<AiChatWidget />)
    await user.click(screen.getByRole('button'))

    await waitFor(() => {
      expect(mocks.connection.start).toHaveBeenCalledTimes(1)
    })

    await user.type(screen.getByPlaceholderText('Type a message...'), 'Need bread')
    await user.click(document.querySelector('.ai-send-btn'))

    expect(await screen.findByText('Need bread')).toBeInTheDocument()
    expect(await screen.findByText('Fresh bread')).toBeInTheDocument()
    expect(mocks.voice.stopSpeaking).toHaveBeenCalledTimes(1)
    expect(mocks.voice.speak).toHaveBeenCalledWith('Fresh bread')
    expect(mocks.connection.stream).toHaveBeenCalledWith('StreamChat', {
      message: 'Need bread',
      conversationId: null
    })
  })
})
