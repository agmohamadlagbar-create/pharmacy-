import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import Skeleton from '../components/Skeleton.jsx'
import './StoreOrdersPage.css'

const MOCK_CUSTOMERS = [
    { name: 'Marcus Holloway', rating: '5.0 ★', orders: '42 Orders', avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=120&auto=format&fit=crop&q=80' },
    { name: 'Sarah Jenkins', rating: '4.8 ★', orders: '12 Orders', avatar: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=120&auto=format&fit=crop&q=80' },
    { name: 'Elena Rodriguez', rating: '4.9 ★', orders: '56 Orders', avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=120&auto=format&fit=crop&q=80' },
    { name: 'Alex Thompson', rating: '4.7 ★', orders: '28 Orders', avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=120&auto=format&fit=crop&q=80' }
]

function getCustomerInfo(userId) {
    if (!userId) return MOCK_CUSTOMERS[0];
    let sum = 0;
    for (let i = 0; i < userId.length; i++) sum += userId.charCodeAt(i);
    return MOCK_CUSTOMERS[sum % MOCK_CUSTOMERS.length];
}

export default function StoreOrdersPage({ onBack }) {
    const { t } = useTranslation()
    const [activeTab, setActiveTab] = useState('New')
    const [stats, setStats] = useState({
        revenue: 0,
        activeCount: 0,
        avgPrep: 'N/A',
        cancellation: '0%',
        revenueTrend: 0,
        activeTrend: 0,
        avgPrepTrend: 0,
        cancellationTrend: 0
    })

    // Helper for robust UTC date parsing
    const parseUtcDate = (dateString) => {
        if (!dateString) return null
        const utcStr = !dateString.endsWith('Z') && !dateString.includes('+') ? dateString + 'Z' : dateString
        const d = new Date(utcStr)
        return isNaN(d.getTime()) ? null : d
    }

    // Fetch dashboard stats from backend or fallback to calculation
    const fetchStats = useCallback(async () => {
        try {
            const { data } = await api.get(`/api/orders/stats?_=${Date.now()}`)
            if (data && typeof data.revenue === 'number') {
                setStats({
                    revenue: data.revenue,
                    activeCount: data.activeCount,
                    avgPrep: data.avgPrep || 'N/A',
                    cancellation: data.cancellation || '0%',
                    revenueTrend: data.revenueTrend || 0,
                    activeTrend: data.activeTrend || 0,
                    avgPrepTrend: data.avgPrepTrend || 0,
                    cancellationTrend: data.cancellationTrend || 0
                })
                return
            }
        } catch (statsErr) {
            console.warn('Backend stats endpoint unavailable, calculating client-side:', statsErr)
        }

        try {
            const { data } = await api.get(`/api/orders?page=1&limit=50&_=${Date.now()}`)
            const items = data.items || data
            if (Array.isArray(items)) {
                const todayUtcStr = new Date().toISOString().split('T')[0]
                const rev = items
                    .filter(o => {
                        if (!['Delivered', 'PickedUp', 'OnTheWay'].includes(o.status)) return false
                        const parsed = parseUtcDate(o.createdAtUtc)
                        return parsed && parsed.toISOString().split('T')[0] === todayUtcStr
                    })
                    .reduce((sum, o) => sum + o.totalAmount, 0)

                const active = items.filter(o => ['Pending', 'Preparing', 'ReadyForPickup'].includes(o.status)).length

                setStats(prev => ({
                    ...prev,
                    revenue: rev,
                    activeCount: active
                }))
            }
        } catch (err) {
            console.error('Failed to calculate orders dashboard stats:', err)
        }
    }, [])

    useEffect(() => {
        fetchStats()
        const intervalId = setInterval(() => {
            fetchStats()
        }, 60 * 60 * 1000)

        return () => clearInterval(intervalId)
    }, [fetchStats])

    const fetchOrders = useCallback(async (page, limit) => {
        let url = `/api/orders?page=${page}&limit=${limit}&_=${Date.now()}`
        if (activeTab === 'New') {
            url += '&status=Pending'
        } else if (activeTab === 'Preparing') {
            url += '&status=Preparing'
        } else if (activeTab === 'Ready') {
            url += '&status=ReadyForPickup'
        }
        const { data } = await api.get(url)
        return data
    }, [activeTab])

    const {
        items: rawOrders,
        isLoading,
        hasMore,
        sentinelRef,
        initialLoad,
        reset
    } = useInfiniteScroll(fetchOrders, {
        limit: 9,
        deps: [activeTab]
    })

    // If Completed tab, filter out active orders client-side (to display PickedUp, OnTheWay, Delivered, Cancelled)
    const orders = activeTab === 'Completed'
        ? rawOrders.filter(o => ['PickedUp', 'OnTheWay', 'Delivered', 'Cancelled'].includes(o.status))
        : rawOrders

    const [declineModalOrder, setDeclineModalOrder] = useState(null)
    const [selectedReasonOption, setSelectedReasonOption] = useState('Out of stock')
    const [customReason, setCustomReason] = useState('')
    const [isSubmittingDecline, setIsSubmittingDecline] = useState(false)

    async function updateStatus(orderId, newStatus) {
        try {
            await api.put(`/api/orders/${orderId}/status`, { status: newStatus })
            reset()
            fetchStats()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        }
    }

    function openDeclineModal(order) {
        setDeclineModalOrder(order)
        setSelectedReasonOption('Out of stock')
        setCustomReason('')
    }

    async function handleConfirmDecline() {
        if (!declineModalOrder) return
        const reason = selectedReasonOption === 'Other' ? customReason.trim() : selectedReasonOption
        if (!reason) {
            alert(t('Please provide a reason for declining.'))
            return
        }

        setIsSubmittingDecline(true)
        try {
            await api.put(`/api/orders/${declineModalOrder.id}/shop-cancel`, { cancellationReason: reason })
            setDeclineModalOrder(null)
            setSelectedReasonOption('Out of stock')
            setCustomReason('')
            reset()
            fetchStats()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        } finally {
            setIsSubmittingDecline(false)
        }
    }

    function formatRelativeTime(dateString) {
        const now = new Date()
        // Ensure UTC parsing: append 'Z' if no timezone info present
        const utcString = dateString && !dateString.endsWith('Z') && !dateString.includes('+') ? dateString + 'Z' : dateString
        const date = new Date(utcString)
        const diffMs = now - date
        const diffMins = Math.max(1, Math.floor(diffMs / 60000))

        if (diffMins < 60) {
            return t('Placed {{count}} min(s) ago', { count: diffMins })
        }
        const diffHours = Math.floor(diffMins / 60)
        if (diffHours < 24) {
            return t('Placed {{count}} hour(s) ago', { count: diffHours })
        }
        return date.toLocaleDateString()
    }

    function getImageUrl(url) {
        if (!url) return null;
        if (url.startsWith('http')) return url;
        return `${api.defaults.baseURL}${url}`;
    }

    const getStatusText = (status) => {
        return {
            Pending: t('New'),
            Preparing: t('Preparing'),
            ReadyForPickup: t('Ready'),
            PickedUp: t('Picked Up'),
            OnTheWay: t('On The Way'),
            Delivered: t('Delivered'),
            Cancelled: t('Cancelled')
        }[status] || status
    }

    // Scroll to top on mount
    useEffect(() => {
        const isJSDom = typeof window !== 'undefined' && window.navigator && window.navigator.userAgent && window.navigator.userAgent.includes('jsdom')
        if (!isJSDom && typeof window.scrollTo === 'function') {
            try {
                window.scrollTo(0, 0)
            } catch (e) {
                console.warn(e)
            }
        }
    }, [])

    return (
        <div className="store-orders-page">
            <div className="so-container">
                {/* Header */}
                <div className="so-header">
                    <button onClick={onBack} className="so-back-btn">
                        <span className="material-symbols-outlined">arrow_back</span>
                        {t('Back')}
                    </button>
                    <h2 className="so-title">{t('Orders Management')}</h2>
                    <button onClick={() => { reset(); fetchStats(); }} className="so-refresh-btn">
                        <span className="material-symbols-outlined">refresh</span>
                        {t('Refresh')}
                    </button>
                </div>

                {/* Bento Stats Grid */}
                <section className="so-bento-grid">
                    <div className="so-bento-card">
                        <span className="so-bento-label">{t("Today's Revenue")}</span>
                        <div className="so-bento-value highlight">
                            {formatPrice(stats.revenue, 'USD')}
                        </div>
                        <span className={`so-bento-subtext ${stats.revenueTrend > 0 ? '' : stats.revenueTrend < 0 ? 'danger' : 'muted'}`}>
                            {stats.revenueTrend > 0 ? `+${stats.revenueTrend}% ${t('vs prev 30d')}` : stats.revenueTrend < 0 ? `${stats.revenueTrend}% ${t('vs prev 30d')}` : t('Last 30d baseline')}
                        </span>
                    </div>
                    <div className="so-bento-card">
                        <span className="so-bento-label">{t('Active Orders')}</span>
                        <div className="so-bento-value">
                            {stats.activeCount}
                        </div>
                        <span className={`so-bento-subtext ${stats.activeTrend > 0 ? '' : stats.activeTrend < 0 ? 'danger' : 'muted'}`}>
                            {stats.activeTrend > 0 ? `+${stats.activeTrend}% ${t('vs prev 30d')}` : stats.activeTrend < 0 ? `${stats.activeTrend}% ${t('vs prev 30d')}` : t('In queue')}
                        </span>
                    </div>
                    <div className="so-bento-card emerald-border">
                        <span className="so-bento-label">{t('Avg. Prep Time')}</span>
                        <div className="so-bento-value">
                            {stats.avgPrep}
                        </div>
                        <span className={`so-bento-subtext ${stats.avgPrepTrend < 0 ? '' : stats.avgPrepTrend > 0 ? 'danger' : 'muted'}`}>
                            {stats.avgPrepTrend < 0 ? `${stats.avgPrepTrend}% ${t('faster')}` : stats.avgPrepTrend > 0 ? `+${stats.avgPrepTrend}% ${t('slower')}` : t('30-day average')}
                        </span>
                    </div>
                    <div className="so-bento-card">
                        <span className="so-bento-label">{t('Cancellation')}</span>
                        <div className="so-bento-value">
                            {stats.cancellation}
                        </div>
                        <span className={`so-bento-subtext ${stats.cancellation === '0%' || stats.cancellationTrend < 0 ? '' : stats.cancellationTrend > 0 ? 'danger' : 'muted'}`}>
                            {stats.cancellation === '0%' ? t('No cancellations') : stats.cancellationTrend < 0 ? `${stats.cancellationTrend}% ${t('improved')}` : stats.cancellationTrend > 0 ? `+${stats.cancellationTrend}% ${t('increased')}` : t('30-day rate')}
                        </span>
                    </div>
                </section>

                {/* Tab Navigation */}
                <div className="so-tabs-row">
                    <button
                        className={`so-tab-btn ${activeTab === 'New' ? 'active' : ''}`}
                        onClick={() => setActiveTab('New')}
                    >
                        <span>{t('New')}</span>
                        {activeTab === 'New' && <span className="so-tab-badge">{orders.length}</span>}
                    </button>
                    <button
                        className={`so-tab-btn ${activeTab === 'Preparing' ? 'active' : ''}`}
                        onClick={() => setActiveTab('Preparing')}
                    >
                        <span>{t('Preparing')}</span>
                        {activeTab === 'Preparing' && <span className="so-tab-badge">{orders.length}</span>}
                    </button>
                    <button
                        className={`so-tab-btn ${activeTab === 'Ready' ? 'active' : ''}`}
                        onClick={() => setActiveTab('Ready')}
                    >
                        <span>{t('Ready')}</span>
                        {activeTab === 'Ready' && <span className="so-tab-badge">{orders.length}</span>}
                    </button>
                    <button
                        className={`so-tab-btn ${activeTab === 'Completed' ? 'active' : ''}`}
                        onClick={() => setActiveTab('Completed')}
                    >
                        <span>{t('Completed')}</span>
                    </button>
                </div>

                {/* Orders Content */}
                {initialLoad ? (
                    <div className="so-orders-grid">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <div key={i} className="so-order-card">
                                <div className="so-card-header">
                                    <Skeleton width="120px" height="24px" />
                                    <Skeleton width="70px" height="24px" borderRadius="12px" />
                                </div>
                                <div className="so-customer-row" style={{ marginTop: '12px' }}>
                                    <Skeleton width="40px" height="40px" borderRadius="50%" />
                                    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', flex: 1 }}>
                                        <Skeleton width="60%" height="16px" />
                                        <Skeleton width="40%" height="12px" />
                                    </div>
                                </div>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', margin: '16px 0' }}>
                                    <Skeleton width="90%" height="14px" />
                                    <Skeleton width="80%" height="14px" />
                                </div>
                                <div style={{ borderTop: '1px solid var(--outline-variant)', paddingTop: '16px', display: 'flex', justifyContent: 'space-between' }}>
                                    <Skeleton width="80px" height="20px" />
                                    <Skeleton width="100px" height="32px" borderRadius="8px" />
                                </div>
                            </div>
                        ))}
                    </div>
                ) : orders.length === 0 && !isLoading ? (
                    <div className="so-empty-state">
                        <span className="material-symbols-outlined so-empty-icon">local_shipping</span>
                        <div className="so-empty-text">{t('No orders found in this category')}</div>
                    </div>
                ) : (
                    <div className="so-orders-grid">
                        {orders.map(o => {
                            const cust = getCustomerInfo(o.userId)
                            const customerName = o.customerName || cust.name
                            const customerAvatar = o.customerAvatarUrl ? getImageUrl(o.customerAvatarUrl) : cust.avatar
                            return (
                                <div key={o.id} className="so-order-card">
                                    {o.status === 'Pending' && <div className="so-pulse-indicator" />}
                                    <div className="so-card-header">
                                        <div>
                                            <h3 className="so-order-id">#ORD-{o.id}</h3>
                                            <span className="so-order-date">{formatRelativeTime(o.createdAtUtc)}</span>
                                        </div>
                                        <span className={`so-status-badge so-badge-${o.status.toLowerCase()}`}>
                                            {getStatusText(o.status)}
                                        </span>
                                    </div>

                                    {/* Customer profile card */}
                                    <div className="so-customer-row">
                                        {customerAvatar ? (
                                            <img src={customerAvatar} alt={customerName} className="so-customer-avatar" />
                                        ) : (
                                            <div className="so-customer-avatar-placeholder">{customerName[0]}</div>
                                        )}
                                        <div className="so-customer-info">
                                            <h4 className="so-customer-name">{customerName}</h4>
                                            <span className="so-customer-rating">{cust.rating} • {cust.orders}</span>
                                        </div>
                                    </div>

                                    {/* Items block */}
                                    <div className="so-items-list">
                                        {o.items.map(item => (
                                            <div key={item.id} className="so-item-row">
                                                <span className="so-item-name">
                                                    {item.quantity}x {item.productName}
                                                </span>
                                                <span className="so-item-price">
                                                    {formatPrice(item.unitPrice, o.currencyCode)}
                                                </span>
                                            </div>
                                        ))}
                                    </div>

                                    {/* Card Action footer */}
                                    <div className="so-card-footer">
                                        <div className="so-total-row">
                                            <span className="so-total-label">{t('Total Amount')}</span>
                                            <span className="so-total-price">
                                                {formatPrice(o.totalAmount, o.currencyCode)}
                                            </span>
                                        </div>

                                        {o.status === 'Pending' && (
                                            <div className="so-action-buttons">
                                                <button onClick={() => openDeclineModal(o)} className="so-btn-decline">
                                                    {t('Decline')}
                                                </button>
                                                <button onClick={() => updateStatus(o.id, 'Preparing')} className="so-btn-accept">
                                                    {t('Accept Order')}
                                                </button>
                                            </div>
                                        )}

                                        {o.status === 'Preparing' && (
                                            <div className="so-action-buttons">
                                                <button onClick={() => updateStatus(o.id, 'ReadyForPickup')} className="so-btn-ready" style={{ width: '100%' }}>
                                                    {t('Ready')}
                                                </button>
                                            </div>
                                        )}

                                        {o.status === 'ReadyForPickup' && (
                                            <div className="so-ready-indicator">
                                                <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>hourglass_empty</span>
                                                <span>{t('Awaiting Driver Pickup')}</span>
                                            </div>
                                        )}

                                        {['PickedUp', 'OnTheWay', 'Delivered', 'Cancelled'].includes(o.status) && (
                                            <div className="so-completed-indicator" style={{ flexDirection: 'column', alignItems: 'flex-start', gap: '4px' }}>
                                                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                                                    <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>
                                                        {o.status === 'Cancelled' ? 'cancel' : 'check_circle'}
                                                    </span>
                                                    <span>{o.status === 'Cancelled' ? t('Order Cancelled') : t('Completed')}</span>
                                                </div>
                                                {o.status === 'Cancelled' && o.cancellationReason && (
                                                    <span style={{ fontSize: '12px', color: 'var(--error, #E8593C)', fontStyle: 'italic' }}>
                                                        {t('Reason')}: {o.cancellationReason}
                                                    </span>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                </div>
                            )
                        })}

                        {isLoading && !initialLoad && Array.from({ length: 2 }).map((_, i) => (
                            <div key={`more-${i}`} className="so-order-card">
                                <div className="so-card-header">
                                    <Skeleton width="120px" height="24px" />
                                    <Skeleton width="70px" height="24px" borderRadius="12px" />
                                </div>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', margin: '24px 0' }}>
                                    <Skeleton width="95%" height="16px" />
                                    <Skeleton width="85%" height="16px" />
                                </div>
                            </div>
                        ))}

                        {hasMore && !initialLoad && <div ref={sentinelRef} style={{ height: '1px' }} />}
                    </div>
                )}
            </div>

            {declineModalOrder && (
                <div className="cancel-modal-overlay" style={{
                    position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
                    backgroundColor: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
                }}>
                    <div className="cancel-modal-content" style={{
                        background: 'var(--surface, #1e1e1e)', color: 'var(--text-primary, #fff)',
                        borderRadius: '16px', padding: '24px', maxWidth: '420px', width: '90%',
                        boxShadow: '0 8px 32px rgba(0,0,0,0.4)', border: '1px solid var(--border, #333)'
                    }}>
                        <h3 style={{ margin: '0 0 8px', fontSize: '18px', color: 'var(--error, #E8593C)' }}>
                            {t('Decline Order')} #{declineModalOrder.id}
                        </h3>
                        <p style={{ margin: '0 0 16px', fontSize: '14px', color: 'var(--muted, #aaa)' }}>
                            {t('Please select a reason for declining this order. The customer will be refunded 100% and notified immediately.')}
                        </p>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginBottom: '20px' }}>
                            {['Out of stock', 'Store is too busy', 'Item unavailable', 'Store is closing', 'Other'].map(option => (
                                <label key={option} style={{
                                    display: 'flex', alignItems: 'center', gap: '10px',
                                    padding: '10px 14px', borderRadius: '8px',
                                    background: selectedReasonOption === option ? 'rgba(232, 89, 60, 0.15)' : 'rgba(255,255,255,0.04)',
                                    border: selectedReasonOption === option ? '1px solid #E8593C' : '1px solid transparent',
                                    cursor: 'pointer', fontSize: '14px'
                                }}>
                                    <input
                                        type="radio"
                                        name="declineReason"
                                        value={option}
                                        checked={selectedReasonOption === option}
                                        onChange={() => setSelectedReasonOption(option)}
                                        style={{ accentColor: '#E8593C' }}
                                    />
                                    {t(option)}
                                </label>
                            ))}

                            {selectedReasonOption === 'Other' && (
                                <textarea
                                    value={customReason}
                                    onChange={e => setCustomReason(e.target.value)}
                                    placeholder={t('Enter specific reason...')}
                                    rows={3}
                                    style={{
                                        width: '100%', padding: '10px', borderRadius: '8px',
                                        background: 'rgba(0,0,0,0.2)', border: '1px solid var(--border, #444)',
                                        color: '#fff', fontSize: '14px', resize: 'vertical', boxSizing: 'border-box'
                                    }}
                                />
                            )}
                        </div>

                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
                            <button
                                onClick={() => setDeclineModalOrder(null)}
                                disabled={isSubmittingDecline}
                                style={{
                                    padding: '8px 16px', borderRadius: '8px', border: '1px solid var(--border, #444)',
                                    background: 'transparent', color: 'var(--text-primary, #fff)', cursor: 'pointer'
                                }}
                            >
                                {t('Cancel')}
                            </button>
                            <button
                                onClick={handleConfirmDecline}
                                disabled={isSubmittingDecline}
                                style={{
                                    padding: '8px 16px', borderRadius: '8px', border: 'none',
                                    background: '#E8593C', color: '#fff', fontWeight: 'bold', cursor: 'pointer'
                                }}
                            >
                                {isSubmittingDecline ? t('Declining...') : t('Confirm Decline')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
