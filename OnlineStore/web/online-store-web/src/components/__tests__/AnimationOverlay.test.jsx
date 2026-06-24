import { act, render } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import AnimationOverlay from '../AnimationOverlay'

vi.mock('lottie-react', () => ({
  default: () => <div data-testid="lottie-mock" />
}))

describe('AnimationOverlay', () => {
  it('renders nothing when animation data is not provided', () => {
    const { container } = render(<AnimationOverlay animationData={null} />)
    expect(container.firstChild).toBeNull()
  })

  it('fades out and calls onComplete after duration plus fade delay', () => {
    vi.useFakeTimers()
    const onComplete = vi.fn()

    try {
      const { container } = render(
        <AnimationOverlay animationData={{ frames: [] }} onComplete={onComplete} duration={1000} />
      )

      const overlay = container.querySelector('.animation-overlay')
      expect(overlay).not.toBeNull()
      expect(overlay).toHaveClass('fade-in')

      act(() => {
        vi.advanceTimersByTime(1000)
      })

      expect(overlay).toHaveClass('fade-out')
      expect(onComplete).not.toHaveBeenCalled()

      act(() => {
        vi.advanceTimersByTime(300)
      })

      expect(onComplete).toHaveBeenCalledTimes(1)
    } finally {
      vi.useRealTimers()
    }
  })
})
