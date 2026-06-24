import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key
  })
}))

vi.mock('../../apiClient.js', () => ({
  api: {
    defaults: {
      baseURL: 'https://api.example.com'
    }
  }
}))

import FavoritesPage from '../FavoritesPage'

describe('FavoritesPage', () => {
  it('shows empty-state actions when there are no favorites', async () => {
    const onBack = vi.fn()
    const user = userEvent.setup()

    render(
      <FavoritesPage
        favorites={[]}
        onBack={onBack}
        onSelectProduct={vi.fn()}
        onToggleFavorite={vi.fn()}
        onNavigateCart={vi.fn()}
        cartCount={0}
      />
    )

    expect(screen.getByText(/added any favorites/i)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Start Shopping' }))
    expect(onBack).toHaveBeenCalledTimes(1)
  })

  it('renders products and triggers select, favorite toggle, and cart navigation callbacks', async () => {
    const onSelectProduct = vi.fn()
    const onToggleFavorite = vi.fn()
    const onNavigateCart = vi.fn()
    const user = userEvent.setup()

    const favoriteProduct = {
      id: 12,
      name: 'Organic Honey',
      description: 'Raw mountain honey',
      price: 8.5,
      imageUrl: '/uploads/honey.png'
    }

    const { container } = render(
      <FavoritesPage
        favorites={[favoriteProduct]}
        onBack={vi.fn()}
        onSelectProduct={onSelectProduct}
        onToggleFavorite={onToggleFavorite}
        onNavigateCart={onNavigateCart}
        cartCount={2}
      />
    )

    const productImage = screen.getByRole('img', { name: 'Organic Honey' })
    expect(productImage).toHaveAttribute('src', 'https://api.example.com/uploads/honey.png')

    await user.click(screen.getByText('Organic Honey'))
    expect(onSelectProduct).toHaveBeenCalledWith(favoriteProduct)

    await user.click(screen.getByTitle('View Cart'))
    expect(onNavigateCart).toHaveBeenCalledTimes(1)

    const favoriteButton = container.querySelector('.fav-btn')
    expect(favoriteButton).not.toBeNull()
    await user.click(favoriteButton)
    expect(onToggleFavorite).toHaveBeenCalledWith(favoriteProduct)
  })
})
