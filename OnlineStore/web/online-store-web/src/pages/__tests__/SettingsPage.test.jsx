import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  changeLanguage: vi.fn()
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key,
    i18n: {
      language: 'en',
      changeLanguage: mocks.changeLanguage
    }
  })
}))

import SettingsPage from '../SettingsPage'

describe('SettingsPage', () => {
  beforeEach(() => {
    mocks.changeLanguage.mockReset()
  })

  it('supports back navigation, theme toggling, and language switching', async () => {
    const onBack = vi.fn()
    const toggleTheme = vi.fn()
    const user = userEvent.setup()

    render(<SettingsPage onBack={onBack} theme="dark" toggleTheme={toggleTheme} />)

    const [backButton] = screen.getAllByRole('button')
    await user.click(backButton)
    expect(onBack).toHaveBeenCalledTimes(1)

    const themeCheckbox = screen.getByRole('checkbox')
    expect(themeCheckbox).toBeChecked()
    await user.click(themeCheckbox)
    expect(toggleTheme).toHaveBeenCalledTimes(1)

    await user.selectOptions(screen.getByRole('combobox'), 'tr')
    expect(mocks.changeLanguage).toHaveBeenCalledWith('tr')
  })
})
