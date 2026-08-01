import React from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import Skeleton from '../components/Skeleton.jsx'

export default function NotificationsPage({ onBack }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'

    const fetchNotifications = async (page, limit) => {
        const { data } = await api.get(`/api/notifications?page=${page}&limit=${limit}&_=${Date.now()}`)
        return data
    }

    const {
        items: notifications,
        isLoading,
        hasMore,
        sentinelRef,
        initialLoad,
        setItems: setNotifications
    } = useInfiniteScroll(fetchNotifications, { limit: 10 })

    async function markAsRead(id) {
        try {
            await api.put(`/api/notifications/${id}/read`)
            setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n))
        } catch { /* ignore */ }
    }

    async function markAllRead() {
        try {
            await api.put('/api/notifications/read-all')
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
        } catch { /* ignore */ }
    }

    const NotificationSkeleton = () => (
        <div style={{
            background: 'var(--surface-container)',
            borderRadius: 'var(--radius-xl)',
            padding: '20px',
            display: 'flex',
            gap: '16px',
            alignItems: 'center',
            border: '1px solid var(--outline-variant)'
        }}>
            <Skeleton width="44px" height="44px" borderRadius="50%" />
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', width: '100%' }}>
                <Skeleton width="50%" height="18px" />
                <Skeleton width="75%" height="14px" />
            </div>
        </div>
    )

    return (
        <div className="page-enter" style={{ minHeight: '100vh', background: 'var(--background)', color: 'var(--on-surface)', fontFamily: "'Plus Jakarta Sans', sans-serif", paddingBottom: '60px' }} dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Header */}
            <header style={{ position: 'sticky', top: 0, zIndex: 40, background: 'var(--surface-glass)', backdropFilter: 'blur(16px)', WebkitBackdropFilter: 'blur(16px)', borderBottom: '1px solid var(--outline-variant)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px var(--screen-pad)' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                        <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                    </button>
                    <h1 style={{ margin: 0, fontSize: '20px', fontWeight: 700 }}>{t('Notifications')}</h1>
                </div>

                {notifications.some(n => !n.isRead) && (
                    <button 
                        onClick={markAllRead} 
                        style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '6px',
                            background: 'var(--surface-container-high, rgba(255,255,255,0.06))',
                            color: 'var(--primary, #bac3ff)',
                            border: '1px solid var(--outline-variant, rgba(255,255,255,0.12))',
                            borderRadius: '20px',
                            padding: '6px 12px',
                            fontSize: '12px',
                            fontWeight: 600,
                            cursor: 'pointer',
                            transition: 'all 0.2s ease',
                            boxShadow: 'none'
                        }}
                        onMouseEnter={(e) => {
                            e.currentTarget.style.background = 'var(--primary-container, rgba(63, 81, 181, 0.2))';
                        }}
                        onMouseLeave={(e) => {
                            e.currentTarget.style.background = 'var(--surface-container-high, rgba(255,255,255,0.06))';
                        }}
                    >
                        <span className="material-symbols-outlined" style={{ fontSize: '16px' }}>done_all</span>
                        {t('Mark all as read')}
                    </button>
                )}
            </header>

            <main style={{ maxWidth: '700px', margin: '0 auto', padding: '24px var(--screen-pad)' }}>
                {initialLoad ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
                        {Array.from({ length: 5 }).map((_, i) => <NotificationSkeleton key={i} />)}
                    </div>
                ) : notifications.length === 0 && !isLoading ? (
                    <div className="empty-cart-container" style={{ margin: '40px 0', padding: '60px 24px' }}>
                        <div className="empty-cart-icon-bg" style={{ width: 80, height: 80 }}>
                            <span className="material-symbols-outlined" style={{ fontSize: 40 }}>notifications_off</span>
                        </div>
                        <h2 className="empty-cart-title">{t('No Notifications')}</h2>
                        <p className="empty-cart-subtitle">{t('You are all caught up! Updates and alerts will appear here.')}</p>
                    </div>
                ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
                        {notifications.map(n => {
                            const isUnread = !n.isRead
                            return (
                                <div
                                    key={n.id}
                                    onClick={() => isUnread && markAsRead(n.id)}
                                    className="card"
                                    style={{
                                        background: isUnread ? 'var(--surface-container-high)' : 'var(--surface-container)',
                                        border: `1px solid ${isUnread ? 'var(--primary)' : 'var(--outline-variant)'}`,
                                        borderRadius: 'var(--radius-xl)',
                                        padding: '18px 20px',
                                        cursor: isUnread ? 'pointer' : 'default',
                                        display: 'flex',
                                        gap: '16px',
                                        alignItems: 'flex-start',
                                        transition: 'all 0.2s ease',
                                        position: 'relative'
                                    }}
                                >
                                    {isUnread && (
                                        <div style={{ position: 'absolute', top: 18, right: isRTL ? 'auto' : 18, left: isRTL ? 18 : 'auto', width: 8, height: 8, borderRadius: '50%', background: 'var(--primary)' }} />
                                    )}

                                    <div style={{
                                        width: 44,
                                        height: 44,
                                        borderRadius: 'var(--radius-md)',
                                        background: isUnread ? 'var(--primary-container)' : 'var(--surface-container-low)',
                                        color: isUnread ? 'var(--on-primary-container)' : 'var(--on-surface-variant)',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        flexShrink: 0
                                    }}>
                                        <span className="material-symbols-outlined">
                                            {n.title?.toLowerCase().includes('order') ? 'local_shipping' 
                                             : n.title?.toLowerCase().includes('offer') ? 'local_offer' 
                                             : 'notifications'}
                                        </span>
                                    </div>

                                    <div style={{ flex: 1 }}>
                                        <div style={{ fontWeight: isUnread ? '700' : '600', color: 'var(--on-surface)', fontSize: '15px', marginBottom: '4px' }}>
                                            {n.title || n.message}
                                        </div>
                                        {n.title && n.message && n.title !== n.message && (
                                            <div style={{ color: 'var(--on-surface-variant)', fontSize: '14px', lineHeight: 1.5, marginBottom: '6px' }}>
                                                {n.message}
                                            </div>
                                        )}
                                        <div style={{ fontSize: '12px', color: 'var(--on-surface-variant)', fontWeight: 500 }}>
                                            {new Date(n.createdAtUtc).toLocaleString()}
                                        </div>
                                    </div>
                                </div>
                            )
                        })}

                        {isLoading && !initialLoad && Array.from({ length: 3 }).map((_, i) => <NotificationSkeleton key={`shimmer-${i}`} />)}

                        {hasMore && !initialLoad && <div ref={sentinelRef} style={{ height: '1px' }} />}

                        {!hasMore && notifications.length > 0 && (
                            <div style={{ textAlign: 'center', color: 'var(--on-surface-variant)', padding: '24px 0', fontSize: '13px' }}>
                                {t('No more notifications')}
                            </div>
                        )}
                    </div>
                )}
            </main>
        </div>
    )
}
