import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import RecommendationCards from '../RecommendationCards'

describe('RecommendationCards', () => {
  it('renders valid recommendations, filters invalid products, and handles actions', async () => {
    const onAddToCart = vi.fn()
    const onNavigateProduct = vi.fn()
    const user = userEvent.setup()

    render(
      <RecommendationCards
        data={{
          BasedOnHistory: [
            {
              ProductId: 1,
              ProductName: 'Olive Oil',
              Price: '12.5',
              Stock: 5,
              StoreName: 'Fresh Market'
            },
            {
              ProductId: 2,
              ProductName: '1',
              Price: 4,
              Stock: 8
            }
          ],
          FrequentlyBoughtTogether: [
            {
              ProductId: 1,
              ProductName: 'Duplicate Olive Oil',
              Price: 12.5,
              Stock: 5
            }
          ]
        }}
        onAddToCart={onAddToCart}
        onNavigateProduct={onNavigateProduct}
      />
    )

    expect(screen.getByText('Recommended For You')).toBeInTheDocument()
    expect(screen.getByText('Olive Oil')).toBeInTheDocument()
    expect(screen.getByText('Fresh Market')).toBeInTheDocument()
    expect(screen.getByText('$12.50')).toBeInTheDocument()
    expect(screen.queryByText('Duplicate Olive Oil')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /add/i }))
    expect(onAddToCart).toHaveBeenCalledWith(1, 1)

    await user.click(screen.getByText('Olive Oil'))
    expect(onNavigateProduct).toHaveBeenCalledWith(1)
  })

  it('shows skeleton cards while recommendations are loading', () => {
    const { container } = render(<RecommendationCards isLoading />)

    expect(container.querySelectorAll('.rec-skeleton-card')).toHaveLength(3)
  })
})
