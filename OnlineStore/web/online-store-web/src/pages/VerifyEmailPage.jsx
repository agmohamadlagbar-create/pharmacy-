import { useState } from 'react'
import { api, setAuthToken } from '../apiClient.js'
import { useTranslation } from 'react-i18next'
import './AuthPage.css'

export default function VerifyEmailPage({ onBack }) {
    const { t } = useTranslation()
    const [email, setEmail] = useState('')
    const [code, setCode] = useState('')
    const [loading, setLoading] = useState(false)
    const [msg, setMsg] = useState(null)

    async function handleVerify(e) {
        e.preventDefault()
        setMsg(null)

        if (!email.trim()) {
            setMsg({ type: 'error', text: t('Email is required') })
            return
        }
        if (!code.trim()) {
            setMsg({ type: 'error', text: t('Verification code is required') })
            return
        }

        setLoading(true)
        try {
            const res = await api.post('/api/auth/verify-email', { email, code })
            const { token, accountType } = res.data
            localStorage.setItem('token', token)
            localStorage.setItem('accountType', accountType)
            setAuthToken(token)
            setMsg({ type: 'ok', text: t('Email verified successfully ✅ You are now logged in.') })
        } catch (err) {
            const text =
                err?.response?.data?.error ||
                (Array.isArray(err?.response?.data?.errors) ? err.response.data.errors.join('\n') : null) ||
                err.message ||
                t('Verification failed')
            setMsg({ type: 'error', text })
        } finally {
            setLoading(false)
        }
    }

    async function handleResend() {
        if (!email.trim()) {
            setMsg({ type: 'error', text: t('Enter your email first') })
            return
        }

        setLoading(true)
        setMsg(null)
        try {
            await api.post('/api/auth/resend-verification', { email })
            setMsg({ type: 'ok', text: t('A new verification code has been sent to your email.') })
        } catch (err) {
            const text = err?.response?.data?.error || err.message || t('Failed to resend')
            setMsg({ type: 'error', text })
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="auth-page">
            <div className="auth-card-glass" style={{ maxWidth: '440px' }}>
                <div className="auth-header">
                    <span className="material-symbols-outlined" style={{ fontSize: '40px', color: 'var(--primary)', marginBottom: '8px' }}>
                        mark_email_read
                    </span>
                    <h1 className="auth-title">{t('Verify Your Email')}</h1>
                    <p className="auth-subtitle">{t('Enter the 6-digit code sent to your email')}</p>
                </div>

                <form onSubmit={handleVerify} className="auth-form" style={{ gap: '16px' }}>
                    <div className="auth-field">
                        <label>{t('Email Address')}</label>
                        <input
                            type="email"
                            className="auth-input"
                            dir="ltr"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder={t('name@example.com')}
                            required
                        />
                    </div>

                    <div className="auth-field">
                        <label>{t('Verification Code')}</label>
                        <input
                            type="text"
                            className="auth-input"
                            value={code}
                            onChange={(e) => setCode(e.target.value)}
                            placeholder={t('123456')}
                            maxLength={6}
                            required
                        />
                    </div>

                    {msg && (
                        <div className={`auth-banner ${msg.type === 'error' ? 'banner-error' : 'banner-success'}`}>
                            {msg.text}
                        </div>
                    )}

                    <button className="auth-submit-btn" disabled={loading} type="submit">
                        {loading ? t('Please wait...') : t('Verify Email')}
                    </button>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '8px' }}>
                        <button
                            type="button"
                            disabled={loading}
                            onClick={handleResend}
                            className="auth-guest-btn"
                        >
                            {t('Resend Verification Code')}
                        </button>

                        <button
                            type="button"
                            onClick={onBack}
                            style={{
                                background: 'transparent',
                                border: 'none',
                                color: 'var(--on-surface-variant)',
                                fontSize: '13px',
                                cursor: 'pointer',
                                padding: '8px'
                            }}
                        >
                            ← {t('Back to Login')}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    )
}
