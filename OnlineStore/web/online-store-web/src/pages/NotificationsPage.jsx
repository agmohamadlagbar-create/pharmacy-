import React from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import Skeleton from '../components/Skeleton.jsx'

export default function NotificationsPage({ onBack }) {
    const { t } = useTranslation()
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
        setItems: setNotifications,
        reset
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
            background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)',
            borderRadius: '12px', padding: '16px', display: 'flex', gap: '16px', alignItems: 'flex-start',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)', border: '1px solid var(--border)'
        }}>
            <Skeleton width="40px" height="40px" borderRadius="50%" />
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', width: '100%' }}>
                <Skeleton width="60%" height="20px" />
                <Skeleton width="40%" height="16px" />
            </div>
        </div>
    )

    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <button onClick={onBack} className="glass-btn">
                        ← {t('Back')}
                    </button>
                    <h2 style={{ margin: 0, color: 'var(--text-primary)' }}>{t('Notifications')}</h2>
                </div>
                {notifications.some(n => !n.isRead) && (
                    <button onClick={markAllRead} className="glass-btn">
                        {t('Mark all as read')}
                    </button>
                )}
            </div>

            {initialLoad ? (
                <div style={{ display: 'grid', gap: '12px' }}>
                    {Array.from({ length: 5 }).map((_, i) => <NotificationSkeleton key={i} />)}
                </div>
            ) : notifications.length === 0 && !isLoading ? (
                <div style={{ textAlign: 'center', padding: '60px', color: 'var(--muted)' }}>
                    <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3" style={{ marginBottom: '16px' }}>
                        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                        <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                    </svg>
                    <div>{t('No notifications yet.')}</div>
                </div>
            ) : (
                <div style={{ display: 'grid', gap: '12px' }}>
                    {notifications.map(n => (
                        <div
                            key={n.id}
                            onClick={() => !n.isRead && markAsRead(n.id)}
                            style={{
                                background: n.isRead ? 'var(--card)' : 'rgba(255, 215, 0, 0.08)',
                                backdropFilter: 'blur(24px)',
                                WebkitBackdropFilter: 'blur(24px)',
                                borderRadius: '12px',
                                padding: '16px',
                                border: `1px solid ${n.isRead ? 'var(--border)' : 'rgba(255, 215, 0, 0.3)'}`,
                                cursor: n.isRead ? 'default' : 'pointer',
                                display: 'flex',
                                gap: '16px',
                                alignItems: 'flex-start',
                                boxShadow: '0 4px 12px rgba(0,0,0,0.15)'
                            }}
                        >
                            <div style={{
                                width: '40px', height: '40px', borderRadius: '50%',
                                background: n.isRead ? 'rgba(255,255,255,0.05)' : 'rgba(255, 215, 0, 0.15)',
                                display: 'flex', alignItems: 'center', justifyContent: 'center',
                                flexShrink: 0
                            }}>
                                <svg width="20" height="20" viewBox="0 0 24 24" fill={n.isRead ? "none" : "#FFD700"} stroke={n.isRead ? "var(--muted)" : "none"} strokeWidth={n.isRead ? "2" : "0"}>
                                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                                    <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                                </svg>
                            </div>
                            <div>
                                <div style={{ fontWeight: n.isRead ? '500' : '700', color: 'var(--text-primary)', fontSize: '15px' }}>
                                    {n.title || n.message}
                                </div>
                                {n.title && n.message && n.title !== n.message && (
                                    <div style={{ color: 'var(--muted)', fontSize: '14px', marginTop: '4px' }}>
                                        {n.message}
                                    </div>
                                )}
                                <div style={{ fontSize: '12px', color: 'var(--muted)', marginTop: '8px' }}>
                                    {new Date(n.createdAtUtc).toLocaleString()}
                                </div>
                            </div>
                        </div>
                    ))}
                    
                    {isLoading && !initialLoad && Array.from({ length: 3 }).map((_, i) => <NotificationSkeleton key={`shimmer-${i}`} />)}
                    
                    {hasMore && !initialLoad && <div ref={sentinelRef} style={{ height: '1px' }} />}
                    
                    {!hasMore && notifications.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px' }}>{t('No more notifications')}</div>
                    )}
                </div>
            )}
        </div>
    )
}
