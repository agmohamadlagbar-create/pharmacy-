import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import CartIconButton from '../CartIconButton'

describe('CartIconButton', () => {
  it('shows badge only when cart count is greater than zero', () => {
    const { rerender } = render(<CartIconButton cartCount={0} onClick={() => {}} />)

    expect(screen.queryByText('0')).not.toBeInTheDocument()

    rerender(<CartIconButton cartCount={3} onClick={() => {}} />)

    expect(screen.getByText('3')).toBeInTheDocument()
  })

  it('calls onClick when the button is pressed', async () => {
    const onClick = vi.fn()
    const user = userEvent.setup()

    render(<CartIconButton cartCount={2} onClick={onClick} />)

    await user.click(screen.getByTitle('View Cart'))

    expect(onClick).toHaveBeenCalledTimes(1)
  })
})
