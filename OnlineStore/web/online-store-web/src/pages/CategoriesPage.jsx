import { useState, useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import NotificationBell from '../components/NotificationBell.jsx'
import Skeleton from '../components/Skeleton.jsx'
import OrderTrackingCard from '../components/OrderTrackingCard.jsx'
import * as signalR from '@microsoft/signalr'

export default function CategoriesPage({
    accountType,
    isGuest,
    onSelectCategory
}) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [adIndex, setAdIndex] = useState(0)
    const timerRef = useRef(null)

    const [categories, setCategories] = useState([])
    const [ads, setAds] = useState([])
    const [loadingCategories, setLoadingCategories] = useState(true)
    const [loadingAds, setLoadingAds] = useState(true)
    const [fetchTimestamp] = useState(Date.now())

    const connRef = useRef(null)
    const trackingConnRef = useRef(null)
    const [hubConnection, setHubConnection] = useState(null)
    const [trackingConnection, setTrackingConnection] = useState(null)
    const [activeOrders, setActiveOrders] = useState([])

    useEffect(() => {
        if (!isGuest) {
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

    // Drag state for ads
    const dragRef = useRef({ startX: 0, dragging: false })

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
        e?.stopPropagation()
        goToAd((adIndex - 1 + ads.length) % ads.length)
    }

    function goToNextAd(e) {
        if (ads.length === 0) return
        e?.stopPropagation()
        goToAd((adIndex + 1) % ads.length)
    }

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

    function handleAvatarClick() {
        if (fileInputRef.current) fileInputRef.current.click()
    }

    async function handleFileChange(e) {
        const file = e.target.files?.[0]
        if (file) {
            const localUrl = URL.createObjectURL(file)
            setAvatarUrl(localUrl)

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
            }
        }
    }

    return (
        <div className="categories-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Main Content Area */}
            <main style={{ maxWidth: 'var(--container-max)', margin: '0 auto', padding: '16px var(--screen-pad) 40px' }}>
                
                {/* Active Orders Section */}
                {!isGuest && activeOrders.length > 0 && (
                    <section style={{ marginBottom: '24px' }}>
                        <div className="orders-scroll-container" style={{ 
                            display: 'flex', 
                            overflowX: 'auto', 
                            gap: '16px',
                            paddingBottom: '8px',
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
                    </section>
                )}

                {/* Ads Carousel / Featured Hero Banner */}
                <section style={{ marginBottom: '32px' }}>
                    <div
                        className="ads-carousel"
                        onPointerDown={onPointerDown}
                        onPointerUp={onPointerUp}
                        style={{ touchAction: 'pan-y', cursor: 'grab', position: 'relative' }}
                    >
                        {loadingAds ? (
                            <Skeleton width="100%" height="100%" borderRadius="var(--radius-xl)" />
                        ) : ads.length > 0 ? (
                            <>
                                {ads.map((ad, i) => {
                                    if (!ad || !ad.imageUrl) return null
                                    const rawPath = ad.imageUrl.replace(/\\/g, '/')
                                    const base = api.defaults.baseURL || ''
                                    const imgUrl = rawPath.startsWith('http') ? rawPath : `${base}${rawPath.startsWith('/') ? '' : '/'}${rawPath}`
                                    const content = (
                                        <img
                                            src={imgUrl}
                                            alt={ad.title || `Ad ${i + 1}`}
                                            className={`ad-slide ${i === adIndex ? 'active' : ''}`}
                                            draggable="false"
                                        />
                                    )
                                    return (
                                        <div key={ad.id || i} style={{ display: i === adIndex ? 'block' : 'none', height: '100%' }}>
                                            {ad.linkUrl ? <a href={ad.linkUrl}>{content}</a> : content}
                                        </div>
                                    )
                                })}

                                {ads.length > 1 && (
                                    <div style={{ position: 'absolute', bottom: '16px', right: '16px', insetInlineEnd: '16px', insetInlineStart: 'auto', display: 'flex', gap: '8px', zIndex: 10 }}>
                                        <button 
                                            className="icon-btn" 
                                            onClick={goToPrevAd} 
                                            title="Previous"
                                            style={{ background: 'var(--surface-glass)', backdropFilter: 'blur(8px)' }}
                                        >
                                            <span className="material-symbols-outlined">chevron_left</span>
                                        </button>
                                        <button 
                                            className="icon-btn" 
                                            onClick={goToNextAd} 
                                            title="Next"
                                            style={{ background: 'var(--surface-glass)', backdropFilter: 'blur(8px)' }}
                                        >
                                            <span className="material-symbols-outlined">chevron_right</span>
                                        </button>
                                    </div>
                                )}
                            </>
                        ) : (
                            <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'var(--surface-container)' }}>
                                <span style={{ color: 'var(--outline)' }}>{t('No ads available')}</span>
                            </div>
                        )}
                    </div>
                </section>

                {/* Categories Grid */}
                <section>
                    <h2 className="section-title" style={{ textAlign: isRTL ? 'right' : 'left', marginBottom: '20px' }}>
                        {t('Shop by Category')}
                    </h2>

                    <div className="cat-grid" style={{ padding: 0 }}>
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
                                        <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'var(--surface-container-high)' }}>
                                            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="var(--outline)" strokeWidth="1" opacity="0.5"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg>
                                        </div>
                                    )}
                                </div>
                                <span className="cat-name">{cat.name}</span>
                            </div>
                        ))}
                    </div>
                </section>
            </main>
        </div>
    )
}
