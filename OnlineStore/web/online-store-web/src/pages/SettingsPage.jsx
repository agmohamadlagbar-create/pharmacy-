import React, { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { requestNotificationPermissionAndGetToken, unregisterTokenFromBackend } from '../utils/firebase'

export default function SettingsPage({ onBack, theme, toggleTheme, onLogout }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'

    const [notificationsEnabled, setNotificationsEnabled] = useState(() => {
        const pref = localStorage.getItem('notifications_disabled');
        return !pref && 'Notification' in window && Notification.permission === 'granted';
    });

    const handleNotificationToggle = async () => {
        if (notificationsEnabled) {
            // Disable notifications
            localStorage.setItem('notifications_disabled', 'true');
            setNotificationsEnabled(false);
            await unregisterTokenFromBackend();
        } else {
            // Enable notifications
            localStorage.removeItem('notifications_disabled');
            const token = await requestNotificationPermissionAndGetToken();
            if (token || Notification.permission === 'granted') {
                setNotificationsEnabled(true);
            }
        }
    };

    const handleLanguageChange = (e) => {
        i18n.changeLanguage(e.target.value)
    }

    return (
        <div className="page-enter" style={{ minHeight: '100vh', background: 'var(--background)', color: 'var(--on-surface)', fontFamily: "'Plus Jakarta Sans', sans-serif", paddingBottom: '60px' }} dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Header */}
            <header className="product-info-header" style={{ position: 'sticky', top: 0, zIndex: 40, background: 'var(--surface-glass)', backdropFilter: 'blur(16px)', WebkitBackdropFilter: 'blur(16px)', borderBottom: '1px solid var(--outline-variant)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px var(--screen-pad)' }}>
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1 style={{ margin: 0, fontSize: '20px', fontWeight: 700 }}>{t('Settings')}</h1>
                <div style={{ width: 44 }} />
            </header>

            <main style={{ maxWidth: '600px', margin: '0 auto', padding: '24px var(--screen-pad)', display: 'flex', flexDirection: 'column', gap: '20px' }}>
                
                {/* Appearance Settings */}
                <div className="card" style={{ padding: '24px', background: 'var(--surface-container)', border: '1px solid var(--outline-variant)', borderRadius: 'var(--radius-xl)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
                        <div style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--surface-container-high)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
                            <span className="material-symbols-outlined">{theme === 'dark' ? 'dark_mode' : 'light_mode'}</span>
                        </div>
                        <div>
                            <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 700 }}>{t('Appearance')}</h2>
                            <p style={{ margin: 0, fontSize: '13px', color: 'var(--on-surface-variant)' }}>{t('Customize visual theme')}</p>
                        </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px', background: 'var(--surface-container-low)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--outline-variant)' }}>
                        <div>
                            <span style={{ fontSize: '14px', fontWeight: 600, display: 'block' }}>{t('Dark Mode')}</span>
                            <span style={{ fontSize: '12px', color: 'var(--on-surface-variant)' }}>
                                {theme === 'dark' ? t('Dark theme is currently active') : t('Light theme is currently active')}
                            </span>
                        </div>
                        <label className="theme-switch">
                            <input type="checkbox" checked={theme === 'dark'} onChange={toggleTheme} />
                            <span className="theme-slider round"></span>
                        </label>
                    </div>
                </div>

                {/* Notifications Settings */}
                <div className="card" style={{ padding: '24px', background: 'var(--surface-container)', border: '1px solid var(--outline-variant)', borderRadius: 'var(--radius-xl)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
                        <div style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--surface-container-high)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
                            <span className="material-symbols-outlined">notifications</span>
                        </div>
                        <div>
                            <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 700 }}>{t('Push Notifications')}</h2>
                            <p style={{ margin: 0, fontSize: '13px', color: 'var(--on-surface-variant)' }}>{t('Manage background updates and order tracking alerts')}</p>
                        </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px', background: 'var(--surface-container-low)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--outline-variant)' }}>
                        <div>
                            <span style={{ fontSize: '14px', fontWeight: 600, display: 'block' }}>{t('Order & System Alerts')}</span>
                            <span style={{ fontSize: '12px', color: 'var(--on-surface-variant)' }}>
                                {notificationsEnabled ? t('Push notifications enabled for this device') : t('Push notifications are currently disabled')}
                            </span>
                        </div>
                        <label className="theme-switch">
                            <input 
                                type="checkbox" 
                                checked={notificationsEnabled} 
                                onChange={handleNotificationToggle} 
                            />
                            <span className="theme-slider round"></span>
                        </label>
                    </div>
                </div>

                {/* Language Settings */}
                <div className="card" style={{ padding: '24px', background: 'var(--surface-container)', border: '1px solid var(--outline-variant)', borderRadius: 'var(--radius-xl)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '20px' }}>
                        <div style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--surface-container-high)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
                            <span className="material-symbols-outlined">translate</span>
                        </div>
                        <div>
                            <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 700 }}>{t('Language & Region')}</h2>
                            <p style={{ margin: 0, fontSize: '13px', color: 'var(--on-surface-variant)' }}>{t('Choose your preferred language')}</p>
                        </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px', background: 'var(--surface-container-low)', borderRadius: 'var(--radius-lg)', border: '1px solid var(--outline-variant)' }}>
                        <div>
                            <span style={{ fontSize: '14px', fontWeight: 600, display: 'block' }}>{t('App Language')}</span>
                            <span style={{ fontSize: '12px', color: 'var(--on-surface-variant)' }}>{t('Select display language')}</span>
                        </div>
                        <select
                            value={i18n.language}
                            onChange={handleLanguageChange}
                            className="admin-input"
                            style={{ width: 'auto', minWidth: '140px', padding: '10px 16px', borderRadius: 'var(--radius-md)', background: 'var(--surface-container)', color: 'var(--on-surface)', border: '1px solid var(--outline-variant)', fontWeight: 600, fontSize: '14px', cursor: 'pointer' }}
                        >
                            <option value="en">English</option>
                            <option value="ar">العربية (Arabic)</option>
                            <option value="tr">Türkçe (Turkish)</option>
                        </select>
                    </div>
                </div>

                {/* Account & Session */}
                {onLogout && (
                    <div className="card" style={{ padding: '24px', background: 'var(--surface-container)', border: '1px solid var(--outline-variant)', borderRadius: 'var(--radius-xl)' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
                            <div style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--err-bg)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--error)' }}>
                                <span className="material-symbols-outlined">logout</span>
                            </div>
                            <div>
                                <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 700 }}>{t('Account Session')}</h2>
                                <p style={{ margin: 0, fontSize: '13px', color: 'var(--on-surface-variant)' }}>{t('Sign out of your active account')}</p>
                            </div>
                        </div>

                        <button 
                            onClick={onLogout}
                            style={{
                                width: '100%',
                                padding: '14px',
                                borderRadius: 'var(--radius-lg)',
                                background: 'var(--err-bg)',
                                border: '1px solid var(--err-border)',
                                color: 'var(--error)',
                                fontWeight: 700,
                                fontSize: '14px',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                gap: '8px',
                                cursor: 'pointer',
                                transition: 'all 0.2s ease'
                            }}
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>logout</span>
                            <span>{t('Log Out')}</span>
                        </button>
                    </div>
                )}

                {/* System Information */}
                <div className="card" style={{ padding: '24px', background: 'var(--surface-container)', border: '1px solid var(--outline-variant)', borderRadius: 'var(--radius-xl)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        <div style={{ width: 40, height: 40, borderRadius: 'var(--radius-md)', background: 'var(--surface-container-high)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
                            <span className="material-symbols-outlined">info</span>
                        </div>
                        <div>
                            <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 700 }}>LagbarShop</h2>
                            <p style={{ margin: 0, fontSize: '12px', color: 'var(--on-surface-variant)' }}>Version 2.4.0 • Modern Commerce Nexus Edition</p>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    )
}
