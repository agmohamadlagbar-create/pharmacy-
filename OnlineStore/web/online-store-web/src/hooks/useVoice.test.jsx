import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useVoice } from './useVoice'

let recognitionInstance

class MockSpeechRecognition {
  constructor() {
    recognitionInstance = this
    this.continuous = true
    this.interimResults = true
    this.lang = ''
    this.start = vi.fn()
    this.stop = vi.fn()
  }
}

class MockSpeechSynthesisUtterance {
  constructor(text) {
    this.text = text
    this.lang = ''
    this.rate = 0
    this.pitch = 0
  }
}

function VoiceHarness() {
  const voice = useVoice()

  return (
    <div>
      <span data-testid="listening">{voice.isListening ? 'yes' : 'no'}</span>
      <span data-testid="recognized">{voice.recognizedText}</span>
      <button onClick={voice.startListening}>start</button>
      <button onClick={voice.stopListening}>stop</button>
      <button onClick={() => voice.speak('Hello shopper')}>speak</button>
      <button onClick={voice.stopSpeaking}>cancel</button>
    </div>
  )
}

describe('useVoice', () => {
  beforeEach(() => {
    recognitionInstance = undefined
    window.SpeechRecognition = MockSpeechRecognition
    window.webkitSpeechRecognition = undefined
    window.SpeechSynthesisUtterance = MockSpeechSynthesisUtterance
    globalThis.SpeechSynthesisUtterance = MockSpeechSynthesisUtterance
    window.speechSynthesis = {
      speak: vi.fn(),
      cancel: vi.fn()
    }
  })

  it('starts and stops speech recognition', async () => {
    const user = userEvent.setup()
    render(<VoiceHarness />)

    await user.click(screen.getByRole('button', { name: 'start' }))

    expect(screen.getByTestId('listening')).toHaveTextContent('yes')
    expect(recognitionInstance.start).toHaveBeenCalledTimes(1)

    await user.click(screen.getByRole('button', { name: 'stop' }))

    expect(screen.getByTestId('listening')).toHaveTextContent('no')
    expect(recognitionInstance.stop).toHaveBeenCalledTimes(1)
  })

  it('stores recognized text when speech recognition returns a result', async () => {
    const user = userEvent.setup()
    render(<VoiceHarness />)

    await user.click(screen.getByRole('button', { name: 'start' }))

    act(() => {
      recognitionInstance.onresult({
        results: [[{ transcript: 'find fresh bread' }]]
      })
    })

    expect(screen.getByTestId('recognized')).toHaveTextContent('find fresh bread')
    expect(screen.getByTestId('listening')).toHaveTextContent('no')
  })

  it('speaks and cancels synthesized speech', async () => {
    const user = userEvent.setup()
    render(<VoiceHarness />)

    await user.click(screen.getByRole('button', { name: 'speak' }))
    await user.click(screen.getByRole('button', { name: 'cancel' }))

    expect(window.speechSynthesis.speak).toHaveBeenCalledTimes(1)
    expect(window.speechSynthesis.speak.mock.calls[0][0].text).toBe('Hello shopper')
    expect(window.speechSynthesis.cancel).toHaveBeenCalledTimes(1)
  })
})
