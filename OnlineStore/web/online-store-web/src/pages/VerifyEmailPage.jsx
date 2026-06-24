import { useState } from 'react'
import { api, setAuthToken } from '../apiClient.js'
import { useTranslation } from 'react-i18next'

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
        <div className="card">
            <div className="header">
                <div>
                    <h1>{t('Verify Your Email')}</h1>
                    <p className="muted">{t('Enter the 6-digit code sent to your email')}</p>
                </div>
            </div>

            <form onSubmit={handleVerify} className="form">
                <div className="field">
                    <label>{t('Email')}</label>
                    <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder={t('name@example.com')} />
                </div>

                <div className="field">
                    <label>{t('Verification Code')}</label>
                    <input value={code} onChange={(e) => setCode(e.target.value)} placeholder={t('123456')} maxLength={6} />
                </div>

                {msg && <div className={`msg ${msg.type}`}>{msg.text}</div>}

                <button className="primary" disabled={loading} type="submit">
                    {loading ? t('Please wait...') : t('Verify Email')}
                </button>

                <button className="primary" type="button" disabled={loading} onClick={handleResend}
                    style={{ background: 'transparent', border: '1px solid var(--border)' }}>
                    {t('Resend Code')}
                </button>

                <button className="primary" type="button" onClick={onBack}
                    style={{ background: 'transparent', border: 'none', textDecoration: 'underline', fontSize: '13px' }}>
                    {t('← Back to Login')}
                </button>
            </form>
        </div>
    )
}
