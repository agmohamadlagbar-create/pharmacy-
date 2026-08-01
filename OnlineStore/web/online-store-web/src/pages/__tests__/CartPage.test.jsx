import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key
  })
}))

vi.mock('../../apiClient', () => ({
  api: {
    defaults: {
      baseURL: 'https://api.example.com'
    }
  }
}))

import CartPage from '../CartPage'

describe('CartPage', () => {
  it('renders empty-cart state and allows users to go back', async () => {
    const onBack = vi.fn()
    const user = userEvent.setup()

    render(
      <CartPage
        cart={{ items: [], totalAmount: 0 }}
        onBack={onBack}
        onUpdateQuantity={vi.fn()}
        onRemoveItem={vi.fn()}
        onCheckout={vi.fn()}
        onNavigateStore={vi.fn()}
      />
    )

    expect(screen.getByText('Your cart is empty')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Start Shopping' }))
    expect(onBack).toHaveBeenCalledTimes(1)
  })

  it('handles quantity changes, item removal, store navigation, and checkout', async () => {
    const onUpdateQuantity = vi.fn()
    const onRemoveItem = vi.fn()
    const onCheckout = vi.fn()
    const onNavigateStore = vi.fn()
    const user = userEvent.setup()

    const cart = {
      items: [
        {
          productId: 101,
          productName: 'Fresh Tomatoes',
          imageUrl: '/uploads/tomatoes.png',
          storeName: 'Green Market',
          storeUserId: 'store-9',
          price: 4.75,
          quantity: 2
        }
      ],
      totalAmount: 9.5
    }

    const { container } = render(
      <CartPage
        cart={cart}
        onBack={vi.fn()}
        onUpdateQuantity={onUpdateQuantity}
        onRemoveItem={onRemoveItem}
        onCheckout={onCheckout}
        onNavigateStore={onNavigateStore}
      />
    )

    const image = screen.getByRole('img', { name: 'Fresh Tomatoes' })
    expect(image).toHaveAttribute('src', 'https://api.example.com/uploads/tomatoes.png')

    await user.click(container.querySelector('.store-link'))
    expect(onNavigateStore).toHaveBeenCalledWith('store-9')

    const quantityButtons = container.querySelectorAll('.qty-picker button')
    expect(quantityButtons).toHaveLength(2)

    await user.click(quantityButtons[0])
    expect(onUpdateQuantity).toHaveBeenCalledWith(101, 1)

    await user.click(quantityButtons[1])
    expect(onUpdateQuantity).toHaveBeenCalledWith(101, 3)

    const removeButton = container.querySelector('.remove-btn-premium')
    expect(removeButton).not.toBeNull()
    await user.click(removeButton)
    expect(onRemoveItem).toHaveBeenCalledWith(101)

    await user.click(screen.getByRole('button', { name: /Proceed to Checkout/i }))
    expect(onCheckout).toHaveBeenCalledTimes(1)
  })
})
