import { act, fireEvent, render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import ErrorOverlay from '../ErrorOverlay'
import { ErrorProvider, triggerGlobalError } from '../../context/ErrorContext'

function renderWithProvider() {
  return render(
    <ErrorProvider>
      <ErrorOverlay />
    </ErrorProvider>
  )
}

describe('ErrorOverlay', () => {
  it('renders and dismisses banner errors', async () => {
    renderWithProvider()

    await act(async () => {
      triggerGlobalError('banner', {
        title: 'Request Failed',
        message: 'Could not load categories.'
      })
    })

    expect(await screen.findByText('Request Failed')).toBeInTheDocument()
    expect(screen.getByText('Could not load categories.')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button'))

    expect(screen.queryByText('Request Failed')).not.toBeInTheDocument()
  })

  it('executes modal action callbacks', async () => {
    const onAction = vi.fn()
    renderWithProvider()

    await act(async () => {
      triggerGlobalError('modal', {
        title: 'Delete Product',
        message: 'This action cannot be undone.',
        actionText: 'Delete',
        onAction
      })
    })

    expect(await screen.findByText('Delete Product')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }))

    expect(onAction).toHaveBeenCalledTimes(1)
  })
})
