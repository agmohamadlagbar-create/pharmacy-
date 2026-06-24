import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
  setAuthToken: vi.fn()
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key
  })
}))

vi.mock('../../apiClient.js', () => ({
  api: {
    post: mocks.post
  },
  setAuthToken: mocks.setAuthToken
}))

import VerifyEmailPage from '../VerifyEmailPage'

describe('VerifyEmailPage', () => {
  beforeEach(() => {
    localStorage.clear()
    mocks.post.mockReset()
    mocks.setAuthToken.mockReset()
  })

  it('validates required fields before submitting', async () => {
    const user = userEvent.setup()
    render(<VerifyEmailPage onBack={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Verify Email' }))

    expect(screen.getByText('Email is required')).toBeInTheDocument()
    expect(mocks.post).not.toHaveBeenCalled()
  })

  it('verifies email successfully and stores auth data', async () => {
    const user = userEvent.setup()
    mocks.post.mockResolvedValueOnce({
      data: {
        token: 'token-123',
        accountType: 'User'
      }
    })

    render(<VerifyEmailPage onBack={vi.fn()} />)

    const [emailInput, codeInput] = screen.getAllByRole('textbox')
    await user.type(emailInput, 'test@example.com')
    await user.type(codeInput, '654321')
    await user.click(screen.getByRole('button', { name: 'Verify Email' }))

    await waitFor(() => {
      expect(mocks.post).toHaveBeenCalledWith('/api/auth/verify-email', {
        email: 'test@example.com',
        code: '654321'
      })
    })

    expect(localStorage.getItem('token')).toBe('token-123')
    expect(localStorage.getItem('accountType')).toBe('User')
    expect(mocks.setAuthToken).toHaveBeenCalledWith('token-123')
    expect(screen.getByText(/Email verified successfully/i)).toBeInTheDocument()
  })

  it('resends verification code after email is provided', async () => {
    const user = userEvent.setup()
    mocks.post.mockResolvedValueOnce({})

    render(<VerifyEmailPage onBack={vi.fn()} />)

    const [emailInput] = screen.getAllByRole('textbox')
    await user.type(emailInput, 'user@site.com')
    await user.click(screen.getByRole('button', { name: 'Resend Code' }))

    await waitFor(() => {
      expect(mocks.post).toHaveBeenCalledWith('/api/auth/resend-verification', {
        email: 'user@site.com'
      })
    })

    expect(screen.getByText(/verification code has been sent/i)).toBeInTheDocument()
  })
})
