import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  get: vi.fn()
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key
  })
}))

vi.mock('../../apiClient.js', () => ({
  api: {
    get: mocks.get,
    defaults: {
      baseURL: 'https://api.example.com'
    }
  }
}))

vi.mock('../../components/ProfileRippleAvatar.jsx', () => ({
  default: ({ src }) => <div data-testid="profile-avatar" data-src={src || ''} />
}))

import ProfilePage from '../ProfilePage'

describe('ProfilePage', () => {
  beforeEach(() => {
    mocks.get.mockReset()
  })

  it('loads and displays profile details from the API', async () => {
    mocks.get.mockResolvedValueOnce({
      data: {
        fullName: 'Lina Baran',
        email: 'lina@example.com',
        accountType: 'User',
        city: 'Istanbul',
        street: 'Istiklal St',
        buildingNumber: '42',
        postalCode: '34000',
        latitude: 41.0082,
        longitude: 28.9784,
        profilePictureUrl: '/uploads/profile.png'
      }
    })

    render(<ProfilePage onBack={vi.fn()} onEdit={vi.fn()} />)

    expect(screen.getByText('Loading profile...')).toBeInTheDocument()

    await waitFor(() => {
      expect(mocks.get).toHaveBeenCalledWith('/api/me')
    })

    expect(await screen.findByText('My Profile')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Lina Baran' })).toBeInTheDocument()
    expect(screen.getByText('lina@example.com')).toBeInTheDocument()
    expect(screen.getByText('41.0082, 28.9784')).toBeInTheDocument()
    expect(screen.getByTestId('profile-avatar')).toHaveAttribute(
      'data-src',
      'https://api.example.com/uploads/profile.png'
    )
  })

  it('fires back and edit callbacks', async () => {
    const onBack = vi.fn()
    const onEdit = vi.fn()
    const user = userEvent.setup()

    mocks.get.mockResolvedValueOnce({
      data: {
        fullName: 'Sam User',
        email: 'sam@example.com',
        accountType: 'User'
      }
    })

    render(<ProfilePage onBack={onBack} onEdit={onEdit} />)
    await screen.findByText('My Profile')

    const buttons = screen.getAllByRole('button')
    await user.click(buttons[0])
    expect(onBack).toHaveBeenCalledTimes(1)

    await user.click(screen.getByRole('button', { name: 'Edit Profile' }))
    expect(onEdit).toHaveBeenCalledTimes(1)
  })
})
