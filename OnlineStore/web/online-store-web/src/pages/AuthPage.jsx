import { useMemo, useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api, setAuthToken } from '../apiClient.js'
import CountryAutocomplete from '../components/CountryAutocomplete.jsx'


function Typewriter({ text, speed = 80, delay = 2500 }) {
  const [displayedText, setDisplayedText] = useState('')
  const [index, setIndex] = useState(0)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    let timeout
    if (!isDeleting && index < text.length) {
      timeout = setTimeout(() => {
        setDisplayedText(prev => prev + text.charAt(index))
        setIndex(index + 1)
      }, speed)
    } else if (!isDeleting && index === text.length) {
      timeout = setTimeout(() => setIsDeleting(true), delay)
    } else if (isDeleting && index > 0) {
      timeout = setTimeout(() => {
        setDisplayedText(prev => prev.slice(0, -1))
        setIndex(index - 1)
      }, speed / 2)
    } else if (isDeleting && index === 0) {
      setIsDeleting(false)
    }
    return () => clearTimeout(timeout)
  }, [index, isDeleting, text, speed, delay])

  return (
    <div style={{ minHeight: '24px', marginBottom: '16px', fontWeight: '700', color: 'var(--primary)', textAlign: 'center', fontSize: '14px', letterSpacing: '0.02em' }}>
      <style>{`
        @keyframes cursor-blink {
          0%, 100% { opacity: 1; }
          50% { opacity: 0; }
        }
      `}</style>
      {displayedText}
      <span style={{ animation: 'cursor-blink 1s step-end infinite', opacity: index === text.length ? 1 : 0.8 }}>|</span>
    </div>
  )
}

const ACCOUNT = { USER: 'User', SHOP: 'Shop', DELIVERY: 'Delivery' }
const MODE = { LOGIN: 'Login', SIGNUP: 'Sign Up' }

function SideCard({ title, btnText, onClick, type, side, isPeeling }) {
  const peelClass = isPeeling ? (side === 'left' ? 'peel-left' : 'peel-right') : ''

  return (
    <div
      data-side={side}
      className={`card side-card ${peelClass}`}
      style={{
        position: 'relative',
        zIndex: 1,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '28px 20px',
        textAlign: 'center',
        background: 'var(--surface-container)',
        border: '1px solid var(--outline-variant)',
        borderRadius: 'var(--radius-xl)',
        flex: '1',
        minWidth: '200px',
        maxWidth: '280px',
        alignSelf: 'center',
        boxShadow: 'var(--shadow-md)'
      }}
    >
      <h3 style={{ margin: '0 0 16px 0', fontSize: '16px', fontWeight: '700', color: 'var(--on-surface)' }}>{title}</h3>
      <button className="glass-btn" onClick={onClick} type="button" style={{ width: '100%', justifyContent: 'center' }}>
        {btnText}
      </button>
    </div>
  )
}

export default function AuthPage({ onLoginSuccess, onGuestLogin, theme, toggleTheme, initialMode }) {
  const { t, i18n } = useTranslation()
  const isRTL = i18n.language === 'ar'
  const navigate = useNavigate()
  const [accountType, setAccountType] = useState(ACCOUNT.USER)
  const [mode, setMode] = useState(initialMode === 'Sign Up' ? MODE.SIGNUP : MODE.LOGIN)
  const [showPassword, setShowPassword] = useState(false)

  useEffect(() => {
    if (initialMode) {
      setMode(initialMode === 'Sign Up' ? MODE.SIGNUP : MODE.LOGIN)
    }
  }, [initialMode])

  const [activeCard, setActiveCard] = useState(null)
  const [isRotating, setIsRotating] = useState(false)
  const [isPeeling, setIsPeeling] = useState(false)
  const [showEnterAnim, setShowEnterAnim] = useState(false)
  const [showSideCards, setShowSideCards] = useState(true)

  const handleMainFormClick = () => {
    if (mode === MODE.SIGNUP && showSideCards && !isRotating && !isPeeling) {
      setIsPeeling(true)
      setTimeout(() => {
        setIsPeeling(false)
        setShowSideCards(false)
        setShowEnterAnim(true)
        setTimeout(() => setShowEnterAnim(false), 500)
      }, 700)
    }
  }

  const handleSideClick = (side, type) => {
    setActiveCard(side)
    setIsRotating(true)

    setTimeout(() => {
      setAccountType(type)
      setMsg(null)
      setIsRotating(false)
      setActiveCard(null)
      setShowSideCards(false)

      setShowEnterAnim(true)
      setTimeout(() => setShowEnterAnim(false), 500)
    }, 800)
  }

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [shopName, setShopName] = useState('')
  const [country, setCountry] = useState({ name: '', code: '' })
  const [storeType, setStoreType] = useState('Restaurant')
  const [code, setCode] = useState('')
  const [phone, setPhone] = useState('')
  const [vehicleInfo, setVehicleInfo] = useState('')

  const [loading, setLoading] = useState(false)
  const [codeSending, setCodeSending] = useState(false)
  const [codeSent, setCodeSent] = useState(false)
  const [msg, setMsg] = useState(null)

  // Forgot password state
  const [showForgot, setShowForgot] = useState(false)
  const [resetEmail, setResetEmail] = useState('')
  const [resetCode, setResetCode] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [resetCodeSent, setResetCodeSent] = useState(false)

  const isShop = accountType === ACCOUNT.SHOP
  const isDelivery = accountType === ACCOUNT.DELIVERY
  const isSignup = mode === MODE.SIGNUP

  const title = useMemo(() => mode === MODE.LOGIN ? mode : `${accountType} • ${mode}`, [accountType, mode])

  function validate() {
    if (!email.trim()) return t('Email is required')
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) return t('Please enter a valid email address')
    if (!password.trim()) return t('Password is required')
    if (password.length < 8) return t('Password must be at least 8 characters')

    if (isSignup) {
      if (!code.trim()) return t("Verification code is required. Click 'Get Code' first.")
      if (!country.code) return t('Please select a valid country from the list.')
      if (isShop) {
        if (!shopName.trim()) return t('Shop name is required')
      }
    }
    return null
  }

  async function sendCode() {
    if (!email.trim()) {
      setMsg({ type: 'error', text: t('Enter your email first') })
      return
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setMsg({ type: 'error', text: t('Please enter a valid email address') })
      return
    }

    setCodeSending(true)
    setMsg(null)
    try {
      await api.post('/api/auth/send-code', { email })
      setCodeSent(true)
      setMsg({ type: 'ok', text: `✅ ${t('Code sent! Check your email.')}` })
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message || t('Failed to send code') })
    } finally {
      setCodeSending(false)
    }
  }

  async function submit(e) {
    e.preventDefault()
    setMsg(null)

    const error = validate()
    if (error) {
      setMsg({ type: 'error', text: error })
      return
    }

    setLoading(true)
    try {
      let res

      if (mode === MODE.LOGIN) {
        res = await api.post('/api/auth/login', { email, password })
      } else {
        if (isShop) {
          res = await api.post('/api/auth/register-shop', { email, password, shopName, storeType, country: country.name, code })
        } else if (isDelivery) {
          res = await api.post('/api/auth/register-delivery', { email, password, fullName: fullName || null, country: country.name, phone: phone || null, vehicleInfo: vehicleInfo || null, code })
        } else {
          res = await api.post('/api/auth/register-user', { email, password, fullName: fullName || null, country: country.name, code })
        }
      }

      const { token, refreshToken: rt, accountType: at } = res.data
      localStorage.setItem('token', token)
      localStorage.setItem('refreshToken', rt)
      localStorage.setItem('accountType', at)
      setAuthToken(token)
      setMsg({ type: 'ok', text: mode === MODE.LOGIN ? `${t('Login successful')} ✅` : `${t('Account created')} ✅` })
      if (onLoginSuccess) setTimeout(() => onLoginSuccess(), 600)
    } catch (err) {
      const text =
        err?.response?.data?.error ||
        (Array.isArray(err?.response?.data?.errors) ? err.response.data.errors.join('\n') : null) ||
        err.message ||
        'Request failed'
      setMsg({ type: 'error', text })
    } finally {
      setLoading(false)
    }
  }

  // Reset password
  async function sendResetCode() {
    if (!resetEmail.trim()) {
      setMsg({ type: 'error', text: t('Enter your email first') })
      return
    }
    setCodeSending(true)
    setMsg(null)
    try {
      await api.post('/api/auth/forgot-password', { email: resetEmail })
      setResetCodeSent(true)
      setMsg({ type: 'ok', text: '✅ Code sent!' })
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
    } finally {
      setCodeSending(false)
    }
  }

  async function handleResetPassword(e) {
    e.preventDefault()
    if (!resetCode.trim()) { setMsg({ type: 'error', text: 'Enter code' }); return }
    if (!newPassword.trim() || newPassword.length < 8) { setMsg({ type: 'error', text: 'Password must be at least 8 chars' }); return }

    setLoading(true)
    setMsg(null)
    try {
      await api.post('/api/auth/reset-password', { email: resetEmail, code: resetCode, newPassword })
      setMsg({ type: 'ok', text: '✅ Password reset! Log in now.' })
      setTimeout(() => { setShowForgot(false); setMsg(null) }, 2000)
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
    } finally {
      setLoading(false)
    }
  }

  // Reset Password Modal / View
  if (showForgot) {
    return (
      <div className="card" style={{ maxWidth: '440px', margin: '40px auto', width: '100%' }} dir={isRTL ? 'rtl' : 'ltr'}>
        <div className="header" style={{ justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2 style={{ fontSize: '20px', fontWeight: 700 }}>{t('Reset Password')}</h2>
          <button type="button" className="icon-btn" onClick={toggleTheme} title="Toggle Theme">
            <span className="material-symbols-outlined">{theme === 'dark' ? 'light_mode' : 'dark_mode'}</span>
          </button>
        </div>

        <form onSubmit={handleResetPassword} className="form">
          <div className="field">
            <label>{t('Email')}</label>
            <div className="field-with-btn">
              <input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder="name@example.com" className="admin-input" />
              <button type="button" className="field-btn" disabled={codeSending} onClick={sendResetCode}>
                {codeSending ? '...' : t('Send Code')}
              </button>
            </div>
          </div>

          {resetCodeSent && (
            <>
              <div className="field">
                <label>Reset Code</label>
                <input value={resetCode} onChange={(e) => setResetCode(e.target.value)} placeholder="123456" maxLength={6} className="admin-input"
                  style={{ textAlign: 'center', fontSize: '20px', letterSpacing: '4px', fontWeight: '700' }} />
              </div>
              <div className="field">
                <label>{t('New Password')}</label>
                <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="Min 8 chars" />
              </div>
            </>
          )}

          {msg && <div className={`msg ${msg.type}`}>{msg.text}</div>}

          {resetCodeSent && (
            <button className="primary" disabled={loading} type="submit">
              {loading ? '...' : t('Reset Password')}
            </button>
          )}

          <button type="button" onClick={() => { setShowForgot(false); setMsg(null) }}
            className="glass-btn" style={{ marginTop: '12px', width: '100%', justifyContent: 'center' }}>
            {t('← Back to Login')}
          </button>
        </form>
      </div>
    )
  }

  return (
    <>
      {/* Ultra-Vibrant Animated Colorful Background */}
      <div className="auth-background-wrapper" style={{ position: 'fixed', inset: 0, zIndex: 0, overflow: 'hidden', pointerEvents: 'none', background: 'var(--background)' }}>
        <style>{`
          @keyframes meshRotate {
            0% { transform: rotate(0deg) scale(1); }
            50% { transform: rotate(180deg) scale(1.15); }
            100% { transform: rotate(360deg) scale(1); }
          }
          @keyframes pulseOrb1 {
            0% { transform: translate(0, 0) scale(1); opacity: 0.8; }
            50% { transform: translate(80px, 60px) scale(1.25); opacity: 0.95; }
            100% { transform: translate(-40px, 90px) scale(0.9); opacity: 0.75; }
          }
          @keyframes pulseOrb2 {
            0% { transform: translate(0, 0) scale(1); opacity: 0.7; }
            50% { transform: translate(-70px, -80px) scale(1.3); opacity: 0.9; }
            100% { transform: translate(50px, -40px) scale(0.85); opacity: 0.65; }
          }
          @keyframes pulseOrb3 {
            0% { transform: translate(0, 0) scale(0.9); opacity: 0.75; }
            50% { transform: translate(60px, -50px) scale(1.2); opacity: 0.95; }
            100% { transform: translate(-50px, 50px) scale(1); opacity: 0.8; }
          }
          @keyframes pulseOrb4 {
            0% { transform: translate(0, 0) scale(1); opacity: 0.65; }
            50% { transform: translate(-60px, 70px) scale(1.2); opacity: 0.85; }
            100% { transform: translate(60px, -60px) scale(0.95); opacity: 0.7; }
          }
        `}</style>

        {/* Rotating Mesh Gradient Background Base */}
        <div style={{
          position: 'absolute',
          inset: '-20%',
          background: 'radial-gradient(ellipse at 50% 50%, rgba(99, 102, 241, 0.15), rgba(168, 85, 247, 0.1), rgba(15, 23, 42, 0.95))',
          animation: 'meshRotate 35s linear infinite',
          transformOrigin: 'center center'
        }} />

        {/* Orb 1: Electric Indigo / Purple */}
        <div style={{
          position: 'absolute',
          top: '-10%',
          left: '-5%',
          width: '55vw',
          height: '55vw',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(99, 102, 241, 0.55) 0%, rgba(139, 92, 246, 0.3) 45%, transparent 70%)',
          filter: 'blur(70px)',
          animation: 'pulseOrb1 14s ease-in-out infinite alternate'
        }} />

        {/* Orb 2: Neon Cyan / Emerald */}
        <div style={{
          position: 'absolute',
          bottom: '-10%',
          right: '-5%',
          width: '50vw',
          height: '50vw',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(6, 182, 212, 0.45) 0%, rgba(16, 185, 129, 0.25) 45%, transparent 70%)',
          filter: 'blur(80px)',
          animation: 'pulseOrb2 16s ease-in-out infinite alternate'
        }} />

        {/* Orb 3: Rose / Magenta */}
        <div style={{
          position: 'absolute',
          top: '20%',
          right: '-10%',
          width: '45vw',
          height: '45vw',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(236, 72, 153, 0.4) 0%, rgba(244, 63, 94, 0.2) 45%, transparent 70%)',
          filter: 'blur(75px)',
          animation: 'pulseOrb3 12s ease-in-out infinite alternate'
        }} />

        {/* Orb 4: Warm Gold / Amber Sunset */}
        <div style={{
          position: 'absolute',
          bottom: '10%',
          left: '10%',
          width: '40vw',
          height: '40vw',
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(245, 158, 11, 0.35) 0%, rgba(217, 119, 6, 0.15) 45%, transparent 70%)',
          filter: 'blur(85px)',
          animation: 'pulseOrb4 18s ease-in-out infinite alternate'
        }} />

        {/* Modern Dot Matrix Grid Overlay for texture & depth */}
        <div style={{
          position: 'absolute',
          inset: 0,
          backgroundImage: 'radial-gradient(rgba(255, 255, 255, 0.12) 1px, transparent 1px)',
          backgroundSize: '32px 32px',
          opacity: 0.6
        }} />
      </div>

      <div
        className={`app ${isRotating ? (activeCard === 'left' ? 'group-rotate-left-clicked' : 'group-rotate-right-clicked') : ''}`}
        style={mode === MODE.SIGNUP ? { width: 'min(1200px, 100%)', display: 'flex', gap: '24px', alignItems: 'center', justifyContent: 'center', flexWrap: 'wrap', position: 'relative', zIndex: 10 } : { width: 'min(500px, 100%)', margin: '0 auto', position: 'relative', zIndex: 10 }}
        dir={isRTL ? 'rtl' : 'ltr'}
      >
        {/* Left Side Card for Signup */}
        {mode === MODE.SIGNUP && showSideCards && accountType !== ACCOUNT.DELIVERY && (
          <SideCard isPeeling={isPeeling} side="left" type="Delivery" title={t('Are you a delivery driver?')} btnText={t('Sign up as delivery')} onClick={() => handleSideClick('left', ACCOUNT.DELIVERY)} />
        )}
        {mode === MODE.SIGNUP && showSideCards && accountType === ACCOUNT.DELIVERY && (
          <SideCard isPeeling={isPeeling} side="left" type="User" title={t('Looking to buy?')} btnText={t('Sign up as customer')} onClick={() => handleSideClick('left', ACCOUNT.USER)} />
        )}

        {/* Main Auth Form Card */}
        <div
          className={`card main-form-card ${showEnterAnim ? 'form-enter' : ''}`}
          style={{
            position: 'relative',
            zIndex: 1,
            flex: mode === MODE.SIGNUP ? '2' : 'none',
            minWidth: '320px',
            maxWidth: '500px',
            width: '100%',
            background: 'var(--surface-container)',
            backdropFilter: 'blur(16px)',
            border: '1px solid var(--outline-variant)',
            borderRadius: 'var(--radius-xl)',
            boxShadow: 'var(--shadow-lg)'
          }}
          onClick={handleMainFormClick}
        >
          {/* Top Bar with Brand & Theme Toggle */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
            <h1 className="navbar-logo" style={{ fontSize: '22px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <img src="/lagbarshop-icon.svg" alt="LagbarShop Logo" style={{ width: '32px', height: '32px', objectFit: 'contain' }} />
              LagbarShop
            </h1>
            <button type="button" className="icon-btn" onClick={toggleTheme} title="Toggle Theme">
              <span className="material-symbols-outlined">{theme === 'dark' ? 'light_mode' : 'dark_mode'}</span>
            </button>
          </div>

          {/* Mode Switcher (Login / Sign Up) */}
          <div className="toggles" style={{ justifyContent: 'center', marginBottom: '24px' }}>
            <div className="seg" style={{ width: '100%', maxWidth: '300px' }}>
              <button
                className={mode === MODE.LOGIN ? 'active' : ''}
                onClick={() => { navigate('/login'); setMsg(null); setShowEnterAnim(true); setTimeout(() => setShowEnterAnim(false), 500); }}
                type="button"
              >
                {t('Login')}
              </button>
              <button
                className={mode === MODE.SIGNUP ? 'active' : ''}
                onClick={() => { navigate('/signup'); setMsg(null); setShowSideCards(true); setAccountType(ACCOUNT.USER); setShowEnterAnim(true); setTimeout(() => setShowEnterAnim(false), 500); }}
                type="button"
              >
                {t('Sign Up')}
              </button>
            </div>
          </div>

          {mode === MODE.SIGNUP && (
            <h2 style={{ textAlign: 'center', marginBottom: '16px', fontSize: '20px', fontWeight: 700 }}>
              {title}
            </h2>
          )}

          {/* Typewriter Banner */}
          {mode === MODE.LOGIN ? (
            <Typewriter key="login-tw" text={t('WELCOME TO LAGBAR SHOP')} />
          ) : (
            <Typewriter key="signup-tw" text={t('EVERYTHING YOU NEED IN ONE PLACE')} />
          )}

          {/* Auth Form */}
          <form onSubmit={submit} className="form">
            {/* Customer Signup Fields */}
            {isSignup && !isShop && !isDelivery && (
              <>
                <div className="field">
                  <label>{t('Full name (optional)')}</label>
                  <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder={t('John Doe')} />
                </div>
                <div className="field">
                  <label>{t('Country *')}</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder={t('e.g. Iraq, Turkey...')} required />
                </div>
              </>
            )}

            {/* Shop Signup Fields */}
            {isSignup && isShop && (
              <>
                <div className="field">
                  <label>{t('Shop Name *')}</label>
                  <input value={shopName} onChange={(e) => setShopName(e.target.value)} placeholder={t('My Store')} />
                </div>
                <div className="field">
                  <label>{t('Country *')}</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder={t('e.g. Iraq, Turkey...')} required />
                </div>
                <div className="field">
                  <label>{t('Store Type')}</label>
                  <select value={storeType} onChange={(e) => setStoreType(e.target.value)} className="admin-input">
                    <option value="Restaurant">{t('Restaurant')}</option>
                    <option value="Market">{t('Market')}</option>
                    <option value="Pharmacy">{t('Pharmacy')}</option>
                  </select>
                </div>
              </>
            )}

            {/* Delivery Driver Signup Fields */}
            {isSignup && isDelivery && (
              <>
                <div className="field">
                  <label>{t('Full Name')}</label>
                  <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder={t('Your full name')} />
                </div>
                <div className="field">
                  <label>{t('Country *')}</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder={t('e.g. Iraq, Turkey...')} required />
                </div>
                <div className="field">
                  <label>{t('Phone')}</label>
                  <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+1234567890" />
                </div>
                <div className="field">
                  <label>{t('Vehicle Info')}</label>
                  <input value={vehicleInfo} onChange={(e) => setVehicleInfo(e.target.value)} placeholder={t('Motorcycle, Car...')} />
                </div>
              </>
            )}

            {/* Email Field */}
            <div className="field">
              <label>{t('Email Address')}</label>
              <div style={{ position: 'relative' }}>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder={t('name@example.com')}
                />
                <span className="material-symbols-outlined" style={{ position: 'absolute', right: '14px', insetInlineEnd: '14px', insetInlineStart: 'auto', top: '50%', transform: 'translateY(-50%)', color: 'var(--outline)', fontSize: '20px', pointerEvents: 'none' }}>
                  alternate_email
                </span>
              </div>
            </div>

            {/* Password Field */}
            <div className="field">
              <label>{t('Password')}</label>
              <div style={{ position: 'relative' }}>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  style={{ position: 'absolute', right: '14px', insetInlineEnd: '14px', insetInlineStart: 'auto', top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', color: 'var(--outline)', cursor: 'pointer', padding: 0, display: 'flex' }}
                >
                  <span className="material-symbols-outlined" style={{ fontSize: '20px' }}>
                    {showPassword ? 'visibility_off' : 'visibility'}
                  </span>
                </button>
              </div>
            </div>

            {/* Verification Code for Signup */}
            {isSignup && (
              <div className="field">
                <label>{t('Verification Code')}</label>
                <div className="field-with-btn">
                  <input
                    className="admin-input"
                    style={{ textAlign: 'center', fontSize: '20px', letterSpacing: '4px', fontWeight: '700' }}
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                    placeholder="123456"
                    maxLength={6}
                  />
                  <button type="button" className="field-btn" disabled={codeSending} onClick={sendCode}>
                    {codeSending ? '...' : codeSent ? t('Resend') : t('Get Code')}
                  </button>
                </div>
              </div>
            )}

            {/* Status Message */}
            {msg && <div className={`msg ${msg.type}`}>{msg.text}</div>}

            {/* Submit Button */}
            <button className="primary" disabled={loading} type="submit" style={{ marginTop: '8px' }}>
              <span>{loading ? t('Please wait...') : mode === MODE.LOGIN ? t('Sign In') : t('Create Account')}</span>
              <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>arrow_forward</span>
            </button>
          </form>

          {/* Forgot Password Link */}
          {!isSignup && (
            <div style={{ marginTop: '16px', textAlign: 'center' }}>
              <button
                type="button"
                onClick={() => { setShowForgot(true); setMsg(null) }}
                style={{ background: 'none', border: 'none', color: 'var(--primary)', fontSize: '13px', fontWeight: 600, cursor: 'pointer' }}
              >
                {t('Forgot Password?')}
              </button>
            </div>
          )}

          {/* Guest Login Option */}
          {!isSignup && (
            <div style={{ marginTop: '20px', textAlign: 'center' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', margin: '0 0 16px' }}>
                <div style={{ flex: 1, height: '1px', background: 'var(--outline-variant)' }} />
                <span style={{ color: 'var(--outline)', fontSize: '12px', whiteSpace: 'nowrap', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{t('OR')}</span>
                <div style={{ flex: 1, height: '1px', background: 'var(--outline-variant)' }} />
              </div>
              <button
                type="button"
                onClick={onGuestLogin}
                className="glass-btn"
                style={{ width: '100%', justifyContent: 'center', padding: '12px' }}
              >
                <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>visibility</span>
                <span>{t('Continue as Guest')}</span>
              </button>
            </div>
          )}
        </div>

        {/* Right Side Card for Signup */}
        {mode === MODE.SIGNUP && showSideCards && accountType !== ACCOUNT.SHOP && (
          <SideCard isPeeling={isPeeling} side="right" type="Shop" title={t('Are you a shop owner?')} btnText={t('Sign up as a shop')} onClick={() => handleSideClick('right', ACCOUNT.SHOP)} />
        )}
        {mode === MODE.SIGNUP && showSideCards && accountType === ACCOUNT.SHOP && (
          <SideCard isPeeling={isPeeling} side="right" type="User" title={t('Looking to buy?')} btnText={t('Sign up as customer')} onClick={() => handleSideClick('right', ACCOUNT.USER)} />
        )}
      </div>
    </>
  )
}
