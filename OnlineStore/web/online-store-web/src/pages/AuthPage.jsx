import { useMemo, useState, useEffect, Suspense } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api, setAuthToken } from '../apiClient.js'
import Spline from '@splinetool/react-spline'
import CountryAutocomplete from '../components/CountryAutocomplete.jsx'

function Typewriter({ text, speed = 80, delay = 2500 }) {
  const [displayedText, setDisplayedText] = useState('')
  const [index, setIndex] = useState(0)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    let timeout;
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
    <div style={{ minHeight: '24px', marginBottom: '16px', fontWeight: 'bold', color: 'var(--primary)', textAlign: 'center', fontSize: '14px' }}>
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
  const typeClass = type === 'Shop' ? 'shop-card' : type === 'Delivery' ? 'delivery-card' : 'user-card';
  const peelClass = isPeeling ? (side === 'left' ? 'peel-left' : 'peel-right') : '';

  return (
    <div data-side={side} className={`card side-card ${typeClass} ${peelClass}`} style={{
      position: 'relative', zIndex: 1, display: 'flex', flexDirection: 'column',
      alignItems: 'center', justifyContent: 'center', padding: '32px 24px',
      textAlign: 'center', background: 'var(--surface)', flex: '1',
      minWidth: '220px', maxWidth: '300px', alignSelf: 'center'
    }}><h3 style={{ margin: '0 0 20px 0', fontSize: '18px', fontFamily: "'Playfair Display', serif" }}>{title}</h3>
      <button className="glass-btn" onClick={onClick} type="button" style={{ width: '100%', justifyContent: 'center' }}>
        {btnText}
      </button>
    </div>
  )
}

export default function AuthPage({ onLoginSuccess, onGuestLogin, theme, toggleTheme, initialMode }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [accountType, setAccountType] = useState(ACCOUNT.USER)
  const [mode, setMode] = useState(initialMode === 'Sign Up' ? MODE.SIGNUP : MODE.LOGIN)

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
      setIsPeeling(true);
      setTimeout(() => {
        setIsPeeling(false);
        setShowSideCards(false);
        // Only run entrance animation if they hadn't already solidified their choice
        setShowEnterAnim(true);
        setTimeout(() => setShowEnterAnim(false), 500);
      }, 700);
    }
  }

  const handleSideClick = (side, type) => {
    setActiveCard(side);
    setIsRotating(true);

    setTimeout(() => {
      setAccountType(type);
      setMsg(null);
      setIsRotating(false);
      setActiveCard(null);
      setShowSideCards(false);

      setShowEnterAnim(true);
      setTimeout(() => setShowEnterAnim(false), 500);
    }, 800);
  }

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fullName, setFullName] = useState('')
  const [shopName, setShopName] = useState('')
  const [location, setLocation] = useState('')
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
      setMsg({ type: 'error', text: 'Enter your email first' })
      return
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setMsg({ type: 'error', text: 'Please enter a valid email address' })
      return
    }

    setCodeSending(true)
    setMsg(null)
    try {
      await api.post('/api/auth/send-code', { email })
      setCodeSent(true)
      setMsg({ type: 'ok', text: '✅ Code sent! Check your email.' })
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message || 'Failed to send code' })
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
      setMsg({ type: 'ok', text: mode === MODE.LOGIN ? 'Login successful ✅' : 'Account created ✅ You are now logged in!' })
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

  // ── Forgot Password ──

  async function sendResetCode() {
    if (!resetEmail.trim()) {
      setMsg({ type: 'error', text: 'Enter your email first' })
      return
    }
    setCodeSending(true)
    setMsg(null)
    try {
      await api.post('/api/auth/forgot-password', { email: resetEmail })
      setResetCodeSent(true)
      setMsg({ type: 'ok', text: '✅ If the account exists, a reset code was sent.' })
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
    } finally {
      setCodeSending(false)
    }
  }

  async function handleResetPassword(e) {
    e.preventDefault()
    if (!resetCode.trim()) { setMsg({ type: 'error', text: 'Enter the reset code' }); return }
    if (!newPassword.trim() || newPassword.length < 8) { setMsg({ type: 'error', text: 'Password must be at least 8 characters' }); return }

    setLoading(true)
    setMsg(null)
    try {
      await api.post('/api/auth/reset-password', { email: resetEmail, code: resetCode, newPassword })
      setMsg({ type: 'ok', text: '✅ Password reset! You can now log in.' })
      setTimeout(() => { setShowForgot(false); setMsg(null) }, 2000)
    } catch (err) {
      setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
    } finally {
      setLoading(false)
    }
  }

  // ── Forgot Password UI ──
  if (showForgot) {
    return (
      <div className="card">
        <div className="header" style={{ justifyContent: 'flex-end' }}>
          <button type="button" className="icon-btn" onClick={toggleTheme} title="Toggle Theme">
            {theme === 'dark' ? (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line></svg>
            ) : (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>
            )}
          </button>
        </div>
        <h2>{t('Reset Password')}</h2>
        <form onSubmit={handleResetPassword} className="form">
          <div className="field">
            <label>{t('Email')}</label>
            <div className="field-with-btn">
              <input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder={t('name@example.com')} className="admin-input" />
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
              {loading ? 'Resetting...' : 'Reset Password'}
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

  // ── Main Auth UI ──
  return (
    <>
      {/* 3D Spline Background */}
      <div style={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        zIndex: 0,
      }}>
        <Suspense fallback={null}>
          <Spline scene="https://prod.spline.design/rZ2O9N5ze3sL1A7y/scene.splinecode" style={{ width: '100%', height: '100%' }} />
        </Suspense>
      </div>
      <div className={`app ${isRotating ? (activeCard === 'left' ? 'group-rotate-left-clicked' : 'group-rotate-right-clicked') : ''}`} style={mode === MODE.SIGNUP ? { width: 'min(1200px, 100%)', display: 'flex', gap: '24px', alignItems: 'center', justifyContent: 'center', flexWrap: 'wrap' } : { width: 'min(520px, 100%)', margin: '0 auto' }}>
        {mode === MODE.SIGNUP && showSideCards && accountType !== ACCOUNT.DELIVERY && (
          <SideCard isPeeling={isPeeling} side="left" type="Delivery" title="Are you a delivery?" btnText="Sign up as a delivery" onClick={() => handleSideClick('left', ACCOUNT.DELIVERY)} />
        )}
        {mode === MODE.SIGNUP && showSideCards && accountType === ACCOUNT.DELIVERY && (
          <SideCard isPeeling={isPeeling} side="left" type="User" title="Looking to buy?" btnText="Sign up as a user" onClick={() => handleSideClick('left', ACCOUNT.USER)} />
        )}

        <div
          className={`card main-form-card ${showEnterAnim ? 'form-enter' : ''} ${accountType === ACCOUNT.USER ? 'user-card' : accountType === ACCOUNT.SHOP ? 'shop-card' : 'delivery-card'}`}
          style={{ position: 'relative', zIndex: 1, flex: mode === MODE.SIGNUP ? '2' : 'none', minWidth: '320px', maxWidth: '520px', width: '100%' }}
          onClick={handleMainFormClick}
        >
          <div className="header" style={{ justifyContent: 'flex-end' }}>
            <button type="button" className="icon-btn" onClick={toggleTheme} title="Toggle Theme">
              {theme === 'dark' ? (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="5"></circle><line x1="12" y1="1" x2="12" y2="3"></line><line x1="12" y1="21" x2="12" y2="23"></line><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line><line x1="1" y1="12" x2="3" y2="12"></line><line x1="21" y1="12" x2="23" y2="12"></line><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line></svg>
              ) : (
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path></svg>
              )}
            </button>
          </div>

          <div className="toggles" style={{ justifyContent: 'center', marginBottom: '32px' }}>
            <div className="seg">
              <button className={mode === MODE.LOGIN ? 'active' : ''} onClick={() => { navigate('/login'); setMsg(null); setShowEnterAnim(true); setTimeout(() => setShowEnterAnim(false), 500); }} type="button">{t('Login')}</button>
              <button className={mode === MODE.SIGNUP ? 'active' : ''} onClick={() => { navigate('/signup'); setMsg(null); setShowSideCards(true); setAccountType(ACCOUNT.USER); setShowEnterAnim(true); setTimeout(() => setShowEnterAnim(false), 500); }} type="button">{t('Sign Up')}</button>
            </div>
          </div>

          {mode === MODE.SIGNUP && <h2>{title}</h2>}

          {mode === MODE.LOGIN ? (
            <Typewriter key="login-tw" text="WELCOME IN LAGBAR SHOP" />
          ) : (
            <Typewriter key="signup-tw" text="WE WILL GET YOU EVERY THING YOU NEED" />
          )}

          <form onSubmit={submit} className="form">
            {isSignup && !isShop && !isDelivery && (
              <>
                <div className="field">
                  <label>{t('Full name (optional)')}</label>
                  <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Optional" />
                </div>
                <div className="field">
                  <label>{t('Country *')}</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder={t('e.g. Iraq, Turkey...')} required />
                </div>
              </>
            )}

            {isSignup && isShop && (
              <>
                <div className="field">
                  <label>Shop name</label>
                  <input value={shopName} onChange={(e) => setShopName(e.target.value)} placeholder="My Shop" />
                </div>
                <div className="field">
                  <label>Country *</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder="e.g. Iraq, Turkey..." required />
                </div>
                <div className="field">
                  <label>Store Type</label>
                  <select value={storeType} onChange={(e) => setStoreType(e.target.value)} className="admin-input">
                    <option value="Restaurant">Restaurant</option>
                    <option value="Market">Market</option>
                    <option value="Pharmacy">Pharmacy</option>
                  </select>
                </div>
              </>
            )}

            {isSignup && isDelivery && (
              <>
                <div className="field">
                  <label>Full name (optional)</label>
                  <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Your name" />
                </div>
                <div className="field">
                  <label>Country *</label>
                  <CountryAutocomplete value={country.name} onChange={(c) => setCountry(c)} placeholder="e.g. Iraq, Turkey..." required />
                </div>
                <div className="field">
                  <label>Phone (optional)</label>
                  <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+1234567890" />
                </div>
                <div className="field">
                  <label>Vehicle Info (optional)</label>
                  <input value={vehicleInfo} onChange={(e) => setVehicleInfo(e.target.value)} placeholder="e.g. Motorcycle, Car" />
                </div>
              </>
            )}

            <div className="field">
              <label>{t('Email')}</label>
              <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder={t('name@example.com')} />
            </div>

            <div className="field">
              <label>{t('Password')}</label>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="********" />
            </div>

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

            {msg && <div className={`msg ${msg.type}`}>{msg.text}</div>}

            <button className="primary" disabled={loading} type="submit">
              {loading ? t('Please wait...') : t(mode)}
            </button>
          </form>

          {/* Forgot Password link — only on Login */}
          {!isSignup && (
            <div style={{ marginTop: '18px', textAlign: 'center' }}>
              <button type="button" onClick={() => { setShowForgot(true); setMsg(null) }}
                className="glass-btn" style={{ fontSize: '12px', padding: '6px 16px' }}>
                Forgot Password?
              </button>
            </div>
          )}

          {/* Guest login */}
          {!isSignup && (
            <div style={{ marginTop: '20px', textAlign: 'center' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', margin: '0 0 16px' }}>
                <div style={{ flex: 1, height: '1px', background: 'var(--border)' }} />
                <span style={{ color: 'var(--muted)', fontSize: '12px', whiteSpace: 'nowrap' }}>or</span>
                <div style={{ flex: 1, height: '1px', background: 'var(--border)' }} />
              </div>
              <button
                type="button"
                onClick={onGuestLogin}
                className="glass-btn"
                style={{ width: '100%', justifyContent: 'center', padding: '12px' }}
              >
                👁️ Continue as Guest
              </button>
            </div>
          )}
        </div>

        {mode === MODE.SIGNUP && showSideCards && accountType !== ACCOUNT.SHOP && (
          <SideCard isPeeling={isPeeling} side="right" type="Shop" title="Are you a shop owner?" btnText="Sign up as a shop" onClick={() => handleSideClick('right', ACCOUNT.SHOP)} />
        )}
        {mode === MODE.SIGNUP && showSideCards && accountType === ACCOUNT.SHOP && (
          <SideCard isPeeling={isPeeling} side="right" type="User" title="Looking to buy?" btnText="Sign up as a user" onClick={() => handleSideClick('right', ACCOUNT.USER)} />
        )}
      </div>
    </>
  )
}
