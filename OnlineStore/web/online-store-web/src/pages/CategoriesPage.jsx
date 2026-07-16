import { useState, useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import RippleAvatar from '../components/RippleAvatar.jsx'
import NotificationBell from '../components/NotificationBell.jsx'
import Skeleton from '../components/Skeleton.jsx'



// Icon SVGs
const MenuIcon = () => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="3" y1="12" x2="21" y2="12"></line><line x1="3" y1="6" x2="21" y2="6"></line><line x1="3" y1="18" x2="21" y2="18"></line></svg>
const HomeIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
const UserIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
const SettingsIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33h.09a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
const LogoutIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path><polyline points="16 17 21 12 16 7"></polyline><line x1="21" y1="12" x2="9" y2="12"></line></svg>
const HeartIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l8.89-8.89 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path></svg>

import CartIconButton from '../components/CartIconButton';
import OrderTrackingCard from '../components/OrderTrackingCard.jsx';
import * as signalR from '@microsoft/signalr'

export default function CategoriesPage({ onLogout, theme, onToggleTheme, accountType, isGuest, onNavigateProfile, onNavigateStoreProducts, onNavigateStoreOrders, onNavigateStoreAnalytics, onNavigateDeliveryJobs, onNavigateAdmin, onNavigateNotifications, onNavigateSettings, onNavigateCart, onNavigateFavorites, onNavigateMyShops, onNavigateSearch, cartCount, onSelectCategory }) {
    const { t } = useTranslation()
    const [adIndex, setAdIndex] = useState(0)
    const timerRef = useRef(null)

    // Sidebar state
    const [sidebarOpen, setSidebarOpen] = useState(false)
    const [avatar, setAvatar] = useState(null)
    const [userName, setUserName] = useState('User')
    const [avatarUrl, setAvatarUrl] = useState(null)
    const fileInputRef = useRef(null)
    const [categories, setCategories] = useState([])
    const [ads, setAds] = useState([])
    const [loadingCategories, setLoadingCategories] = useState(true)
    const [loadingAds, setLoadingAds] = useState(true)
    const [fetchTimestamp] = useState(Date.now())

    useEffect(() => {
        if (!isGuest) {
            fetchProfile()
            if (accountType === 'User') {
                fetchActiveOrders()
                setupOrderHub()
                setupTrackingHub()
            }
        }
        fetchCategories()
        fetchAds()

        return () => {
            connRef.current?.stop()
            trackingConnRef.current?.stop()
        }
    }, [isGuest, accountType])

    const connRef = useRef(null)
    const trackingConnRef = useRef(null)
    const [hubConnection, setHubConnection] = useState(null)
    const [trackingConnection, setTrackingConnection] = useState(null)
    const [activeOrders, setActiveOrders] = useState([])

    async function fetchActiveOrders() {
        if (isGuest) return
        try {
            const { data } = await api.get('/api/orders/active')
            setActiveOrders(data)
        } catch (err) {
            console.error('Failed to fetch active orders', err)
        }
    }

    function setupOrderHub() {
        const token = localStorage.getItem('token')
        if (!token) return

        const baseURL = import.meta.env.VITE_API_BASE_URL
        const conn = new signalR.HubConnectionBuilder()
            .withUrl(`${baseURL}/hubs/notifications?access_token=${token}`)
            .withAutomaticReconnect()
            .build()

        conn.on('OrderStatusChanged', () => {
            fetchActiveOrders()
        })

        // Also refresh on generic notifications just in case
        conn.on('ReceiveNotification', () => {
            fetchActiveOrders()
        })

        conn.start().catch(console.error)
        connRef.current = conn
        setHubConnection(conn)
    }

    function setupTrackingHub() {
        const token = localStorage.getItem('token')
        if (!token) return

        const baseURL = import.meta.env.VITE_API_BASE_URL
        const trackingConn = new signalR.HubConnectionBuilder()
            .withUrl(`${baseURL}/hubs/tracking?access_token=${token}`)
            .withAutomaticReconnect()
            .build()

        trackingConn.start().then(() => {
            trackingConnRef.current = trackingConn
            setTrackingConnection(trackingConn)
        }).catch(err => console.error('TrackingHub connect error:', err))
    }

    const handleDismissOrder = (orderGroupId) => {
        setActiveOrders(prev => prev.filter(o => o.orderGroupId !== orderGroupId))
    }

    async function fetchProfile() {
        try {
            const res = await api.get('/api/me')
            const data = res.data
            setUserName(data.fullName || data.shopName || data.email || 'User')
            if (data.profilePictureUrl) {
                setAvatarUrl(`${api.defaults.baseURL}${data.profilePictureUrl}`)
            }
        } catch (err) {
            console.error('Failed to fetch profile', err)
        }
    }

    async function fetchCategories() {
        try {
            const res = await api.get(`/api/categories?_=${Date.now()}`)
            setCategories(res.data)
        } catch (err) {
            console.error('Failed to fetch categories', err)
        } finally {
            setLoadingCategories(false)
        }
    }

    async function fetchAds() {
        try {
            const res = await api.get(`/api/ads?_=${Date.now()}`)
            setAds(res.data)
        } catch (err) {
            console.error('Failed to fetch ads', err)
        } finally {
            setLoadingAds(false)
        }
    }

    // Drag state
    const dragRef = useRef({ startX: 0, dragging: false })

    // Auto-rotate ads every 5 seconds
    useEffect(() => {
        if (ads.length === 0) return
        timerRef.current = setInterval(() => setAdIndex(i => (i + 1) % ads.length), 5000)
        return () => clearInterval(timerRef.current)
    }, [ads.length])

    function goToAd(i) {
        setAdIndex(i)
        clearInterval(timerRef.current)
        timerRef.current = setInterval(() => setAdIndex(j => (j + 1) % ads.length), 5000)
    }

    function goToPrevAd(e) {
        if (ads.length === 0) return
        e.stopPropagation();
        goToAd((adIndex - 1 + ads.length) % ads.length);
    }

    function goToNextAd(e) {
        if (ads.length === 0) return
        e.stopPropagation();
        goToAd((adIndex + 1) % ads.length);
    }

    // Drag / swipe handlers
    function onPointerDown(e) {
        dragRef.current = { startX: e.clientX, dragging: true }
    }
    function onPointerUp(e) {
        if (!dragRef.current.dragging || ads.length === 0) return
        const dx = e.clientX - dragRef.current.startX
        if (Math.abs(dx) > 50) {
            if (dx < 0) goToAd((adIndex + 1) % ads.length)
            else goToAd((adIndex - 1 + ads.length) % ads.length)
        }
        dragRef.current.dragging = false
    }

    // Avatar upload
    function handleAvatarClick() {
        if (fileInputRef.current) fileInputRef.current.click()
    }

    async function handleFileChange(e) {
        const file = e.target.files?.[0]
        if (file) {
            // Local preview
            const localUrl = URL.createObjectURL(file)
            setAvatarUrl(localUrl)

            // Upload
            const token = localStorage.getItem('token')
            const formData = new FormData()
            formData.append('file', file)

            try {
                const res = await api.post('/api/me/profile-picture', formData, {
                    headers: { 'Content-Type': 'multipart/form-data' }
                })
                const data = res.data
                setAvatarUrl(`${api.defaults.baseURL}${data.profilePictureUrl}`)
            } catch (err) {
                console.error('Upload error', err)
                alert('Upload error')
            }
        }
    }



    return (
        <div className="categories-page">
            {/* Sidebar & Overlay */}
            <div className={`sidebar-overlay ${sidebarOpen ? 'open' : ''}`} onClick={() => setSidebarOpen(false)} />
            <aside className={`sidebar ${sidebarOpen ? 'open' : ''}`}>
                <div className="sidebar-header">
                    {isGuest ? (
                        <>
                            <div style={{ width: 80, height: 80, borderRadius: '50%', border: '2px solid var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                                <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="var(--muted)" strokeWidth="1.5"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" /></svg>
                            </div>
                            <h3 className="sidebar-name">{t('Guest')}</h3>
                            <span className="sidebar-role-badge">{t('Browse Only')}</span>
                        </>
                    ) : (
                        <>
                            <div style={{ position: 'relative', display: 'inline-block' }}>
                                <RippleAvatar
                                    src={avatarUrl}
                                    onClick={handleAvatarClick}
                                    title={t('Upload a picture')}
                                    size={80}
                                    glowThickness={3}
                                />
                                <button 
                                    onClick={handleAvatarClick}
                                    style={{
                                        position: 'absolute',
                                        bottom: 0,
                                        right: 0,
                                        background: 'var(--primary)',
                                        color: 'white',
                                        border: '2px solid white',
                                        borderRadius: '50%',
                                        width: 28,
                                        height: 28,
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        cursor: 'pointer',
                                        padding: 0,
                                        zIndex: 20,
                                        boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
                                    }}
                                    title={t('Upload a picture')}
                                >
                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
                                </button>
                            </div>
                            <input
                                type="file"
                                accept="image/*"
                                ref={fileInputRef}
                                onChange={handleFileChange}
                                style={{ display: 'none' }}
                            />
                            <h3 className="sidebar-name">{userName}</h3>
                            {accountType && <span className="sidebar-role-badge">{accountType}</span>}
                        </>
                    )}
                </div>

                <nav className="sidebar-nav">
                    <button className="sidebar-link active" onClick={() => setSidebarOpen(false)}>
                        <HomeIcon /> {t('Home')}
                    </button>

                    {!isGuest && (
                        <>
                            <button className="sidebar-link" onClick={() => {
                                setSidebarOpen(false)
                                onNavigateProfile()
                            }}>
                                <UserIcon /> {t('Profile')}
                            </button>

                            {/* Customer-only links */}
                            {accountType === 'User' && (
                                <>
                                    <button className="sidebar-link" onClick={() => {
                                        setSidebarOpen(false)
                                        onNavigateFavorites()
                                    }}>
                                        <HeartIcon /> {t('Favorites')}
                                    </button>

                                    <button className="sidebar-link" onClick={() => {
                                        setSidebarOpen(false)
                                        if(onNavigateMyShops) onNavigateMyShops()
                                    }}>
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 8h1a4 4 0 0 1 0 8h-1"></path><path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4V8z"></path><line x1="6" y1="1" x2="6" y2="4"></line><line x1="10" y1="1" x2="10" y2="4"></line><line x1="14" y1="1" x2="14" y2="4"></line></svg> 
                                        {t('Delivered Orders')}
                                    </button>
                                </>
                            )}

                            {/* Admin links */}
                            {accountType === 'Admin' && (
                                <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateAdmin() }}>
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
                                    {t('Admin Panel')}
                                </button>
                            )}

                            {/* Shop links */}
                            {accountType === 'Shop' && (
                                <>
                                    <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateStoreProducts() }}>
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path></svg>
                                        {t('My Products')}
                                    </button>
                                    <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateStoreOrders() }}>
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>
                                        {t('Store Orders')}
                                    </button>
                                    <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateStoreAnalytics() }}>
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>
                                        {t('Store Analytics')}
                                    </button>
                                </>
                            )}

                            {/* Delivery links */}
                            {accountType === 'Delivery' && (
                                <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateDeliveryJobs() }}>
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="1" y="3" width="15" height="13"></rect><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"></polygon><circle cx="5.5" cy="18.5" r="2.5"></circle><circle cx="18.5" cy="18.5" r="2.5"></circle></svg>
                                    {t('Delivery Jobs')}
                                </button>
                            )}

                            {/* Notifications â€” for all logged-in users */}
                            <div style={{ padding: '4px 0 0 0' }}>
                                <NotificationBell onClick={() => { setSidebarOpen(false); onNavigateNotifications(); }} />
                            </div>

                            <button className="sidebar-link" onClick={() => { setSidebarOpen(false); onNavigateSettings && onNavigateSettings(); }}>
                                <SettingsIcon /> {t('Settings')}
                            </button>
                        </>
                    )}
                </nav>

                <div className="sidebar-footer">
                    <button className="logout-btn" onClick={onLogout}>
                        <LogoutIcon /> {isGuest ? t('Sign In') : t('Log out')}
                    </button>
                </div>
            </aside>

            <header className="cat-header" style={{ justifyContent: 'space-between', gap: '16px', paddingBottom: '8px', alignItems: 'center' }}>
                <button className="icon-btn" onClick={() => setSidebarOpen(true)} title="Open Menu">
                    <MenuIcon />
                </button>
                <div style={{ flex: 1, cursor: 'pointer' }} onClick={() => { if (onNavigateSearch) onNavigateSearch('') }}>
                    <div style={{ position: 'relative' }}>
                        <input 
                            type="text" 
                            placeholder={t('Search for products...')} 
                            readOnly
                            style={{
                                width: '100%',
                                padding: '10px 16px 10px 40px',
                                borderRadius: '24px',
                                border: '1px solid var(--border)',
                                background: 'var(--card)',
                                color: 'var(--text)',
                                fontSize: '1rem',
                                outline: 'none',
                                boxShadow: '0 2px 8px rgba(0,0,0,0.05)',
                                cursor: 'pointer'
                            }}
                        />
                        <svg 
                            width="20" 
                            height="20" 
                            viewBox="0 0 24 24" 
                            fill="none" 
                            stroke="var(--muted)" 
                            strokeWidth="2" 
                            strokeLinecap="round" 
                            strokeLinejoin="round" 
                            style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)' }}
                        >
                            <circle cx="11" cy="11" r="8"></circle>
                            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                        </svg>
                    </div>
                </div>
                <div className="cat-header-actions">
                    {!isGuest && (
                        <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
                    )}
                </div>
            </header>

            {/* Ads carousel â€” draggable */}
            <div
                className="ads-carousel"
                onPointerDown={onPointerDown}
                onPointerUp={onPointerUp}
                style={{ touchAction: 'pan-y', cursor: 'grab' }}
            >
                {loadingAds ? (
                    <Skeleton width="100%" height="100%" borderRadius="0" />
                ) : ads.length > 0 ? (
                    <>
                        {ads.map((ad, i) => {
                    const imgUrl = ad.imageUrl.startsWith('http') ? ad.imageUrl : `${api.defaults.baseURL}${ad.imageUrl}`
                    const cacheBustedUrl = `${imgUrl}${imgUrl.includes('?') ? '&' : '?'}t=${fetchTimestamp}`
                    const content = (
                        <img
                            src={cacheBustedUrl}
                            alt={ad.title || `Ad ${i + 1}`}
                            className={`ad-slide ${i === adIndex ? 'active' : ''}`}
                            draggable="false"
                        />
                    )
                    return (
                        <div key={ad.id || i} style={{ display: i === adIndex ? 'block' : 'none' }}>
                            {ad.linkUrl ? <a href={ad.linkUrl}>{content}</a> : content}
                        </div>
                    )
                })}

                {ads.length > 1 && (
                    <>
                        <button className="ad-ctrl left" onClick={goToPrevAd} title="Previous Ad" aria-label="Previous Advertisement">
                            &#10094;
                        </button>
                        <button className="ad-ctrl right" onClick={goToNextAd} title="Next Ad" aria-label="Next Advertisement">
                            &#10095;
                        </button>
                    </>
                )}

                <div className="ad-dots">
                    {ads.map((_, i) => (
                        <span key={i} className={`dot ${i === adIndex ? 'active' : ''}`} onClick={() => goToAd(i)} />
                    ))}
                </div>
                </>
                ) : (
                    <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'var(--card)' }}>
                        <span style={{ color: 'var(--muted)' }}>{t('No ads available')}</span>
                    </div>
                )}
            </div>

            {/* Active Orders Section */}
            {!isGuest && activeOrders.length > 0 && (
                <div style={{ margin: '8px 0 24px 0' }}>
                    <div className="orders-scroll-container" style={{ 
                        display: 'flex', 
                        overflowX: 'auto', 
                        padding: '10px 18px 15px',
                        gap: '8px',
                        scrollbarWidth: 'none',
                        msOverflowStyle: 'none'
                    }}>
                        {activeOrders.map(order => (
                            <OrderTrackingCard 
                                key={order.orderGroupId} 
                                order={order} 
                                onDismiss={handleDismissOrder} 
                                hubConnection={hubConnection}
                                trackingConnection={trackingConnection}
                            />
                        ))}
                    </div>
                </div>
            )}

            {/* Category grid */}
            <h2 className="section-title">{t('Shop by Category')}</h2>
            <div className="cat-grid">
                {loadingCategories ? (
                    Array.from({ length: 8 }).map((_, i) => (
                        <div key={i} className="cat-card" style={{ padding: '20px' }}>
                            <div className="cat-img-wrap" style={{ background: 'transparent' }}>
                                <Skeleton width="100%" height="100%" borderRadius="50%" />
                            </div>
                            <Skeleton width="60%" height="16px" style={{ marginTop: '12px', alignSelf: 'center' }} />
                        </div>
                    ))
                ) : categories.map(cat => (
                    <div key={cat.id} className="cat-card" onClick={() => onSelectCategory(cat)}>
                        <div className="cat-img-wrap">
                            {cat.imageUrl ? (
                                <img src={cat.imageUrl.startsWith('http') ? cat.imageUrl : `${api.defaults.baseURL}${cat.imageUrl}`} alt={cat.name} />
                            ) : (
                                <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'var(--card)' }}>
                                    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg>
                                </div>
                            )}
                        </div>
                        <span className="cat-name">{cat.name}</span>
                    </div>
                ))}
            </div>
        </div>
    )
}
