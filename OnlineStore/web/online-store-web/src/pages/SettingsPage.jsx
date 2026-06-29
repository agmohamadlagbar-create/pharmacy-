import React from 'react'
import { useTranslation } from 'react-i18next'

const BackIcon = () => (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <line x1="19" y1="12" x2="5" y2="12"></line>
        <polyline points="12 19 5 12 12 5"></polyline>
    </svg>
)

export default function SettingsPage({ onBack, theme, toggleTheme }) {
    const { t, i18n } = useTranslation()

    const handleLanguageChange = (e) => {
        i18n.changeLanguage(e.target.value)
    }

    return (
        <div style={{ padding: '0', background: 'var(--bg)', minHeight: '100vh', color: 'var(--text-primary)', direction: i18n.language === 'ar' ? 'rtl' : 'ltr' }}>
            <header className="cat-header" style={{ justifyContent: 'flex-start', gap: '16px' }}>
                <button className="icon-btn" onClick={onBack}>
                    <BackIcon />
                </button>
                <h1 style={{ flex: 1, margin: 0, fontSize: '20px', fontWeight: 600, fontFamily: "'Playfair Display', serif" }}>{t('Settings')}</h1>
            </header>

            <main style={{ padding: '24px' }}>
                <div className="card" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '20px', marginBottom: '16px' }}>
                    <div>
                        <h2 style={{ margin: '0 0 4px 0', fontSize: '16px' }}>{t('Dark Mode')}</h2>
                        <p style={{ margin: 0, fontSize: '13px', color: 'var(--text-secondary)' }}>{t('Enable or disable dark theme')}</p>
                    </div>
                    <label className="theme-switch">
                        <input type="checkbox" checked={theme === 'dark'} onChange={toggleTheme} />
                        <span className="theme-slider round"></span>
                    </label>
                </div>

                <div className="card" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '20px' }}>
                    <div>
                        <h2 style={{ margin: '0 0 4px 0', fontSize: '16px' }}>{t('Language')}</h2>
                        <p style={{ margin: 0, fontSize: '13px', color: 'var(--text-secondary)' }}>{t('Choose your preferred language')}</p>
                    </div>
                    <select
                        value={i18n.language}
                        onChange={handleLanguageChange}
                        style={{ padding: '8px 12px', borderRadius: '8px', border: '1px solid var(--border)', background: 'var(--bg)', color: 'var(--text-primary)', outline: 'none' }}
                    >
                        <option value="en">{t('English')}</option>
                        <option value="ar">{t('Arabic')}</option>
                        <option value="tr">{t('Turkish')}</option>
                    </select>
                </div>
            </main>
        </div>
    )
}
