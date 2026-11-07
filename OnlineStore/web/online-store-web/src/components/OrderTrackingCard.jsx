import { useEffect, useMemo, useState } from 'react'
import './OrderTrackingCard.css'
import { api } from '../apiClient'
import { formatPrice } from '../utils/formatPrice.js'
import { countReadyStores, countTotalItems, getTrackingInfo } from '../utils/orderTracking'

const STATUS_STEPS = [
    { key: 'pending', label: 'Placed' },
    { key: 'preparing', label: 'Preparing' },
    { key: 'readyforpickup', label: 'Ready' },
    { key: 'assigned', label: 'Assigned' },
    { key: 'pickedup', label: 'Picked up' },
    { key: 'ontheway', label: 'On way' },
    { key: 'delivered', label: 'Delivered!' }
]

function StatusBar({ currentStatus }) {
    const normalized = (currentStatus || '').toLowerCase()
    if (normalized === 'cancelled') return null

    const currentIndex = STATUS_STEPS.findIndex(step => step.key === normalized)

    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '16px 18px 24px', overflowX: 'auto', gap: '8px' }}>
            {STATUS_STEPS.map((step, index) => (
                <div key={step.key} style={{ textAlign: 'center', fontSize: 11, flex: 1, minWidth: 54 }}>
                    <div style={{
                        width: 14,
                        height: 14,
                        borderRadius: '50%',
                        background: index <= currentIndex ? 'var(--primary, #3B8BD4)' : 'var(--muted, #ccc)',
                        margin: '0 auto 8px',
                        transform: index === currentIndex ? 'scale(1.3)' : 'scale(1)',
                        transition: 'all 0.3s ease',
                        boxShadow: index === currentIndex ? '0 0 8px rgba(59, 139, 212, 0.5)' : 'none'
                    }} />
                    <div style={{
                        color: index <= currentIndex ? 'var(--text-primary)' : 'var(--muted)',
                        fontWeight: index === currentIndex ? 'bold' : 'normal',
                        whiteSpace: 'nowrap'
                    }}>
                        {step.label}
                    </div>
                </div>
            ))}
        </div>
    )
}

export default function OrderTrackingCard({ order, onDismiss }) {
    const stores = order.stores || []
    const [trackingStatus, setTrackingStatus] = useState((order.status || '').toLowerCase())
    const [isCancelling, setIsCancelling] = useState(false)
    const [showCancelModal, setShowCancelModal] = useState(false)

    useEffect(() => {
        setTrackingStatus((order.status || '').toLowerCase())
    }, [order.status, order.orderGroupId])

    const readyStoreCount = useMemo(() => countReadyStores(stores), [stores])

    const totalItems = useMemo(() => countTotalItems(stores), [stores])

    const info = useMemo(
        () => getTrackingInfo(trackingStatus, readyStoreCount, stores.length),
        [readyStoreCount, stores.length, trackingStatus]
    )

    useEffect(() => {
        const status = (trackingStatus || '').toLowerCase()
        if ((status === 'delivered' || status === 'cancelled') && onDismiss) {
            const delay = status === 'cancelled' ? 2000 : 10000
            const timer = setTimeout(() => onDismiss(order.orderGroupId), delay)
            return () => clearTimeout(timer)
        }
    }, [onDismiss, order.orderGroupId, trackingStatus])

    async function confirmCancelOrder() {
        setIsCancelling(true)
        try {
            await api.put(`/api/orders/${order.representativeOrderId}/cancel`)
            setTrackingStatus('cancelled')
            setShowCancelModal(false)
        } catch (error) {
            alert(error.response?.data?.error || 'Failed to cancel order')
        } finally {
            setIsCancelling(false)
        }
    }

    return (
        <div className="order-tracking-card">
            <div className="tracking-header">
                <div className="header-top">
                    <div />
                    <span className="eta-label">{info.eta}</span>
                </div>
                <div className="header-bottom">
                    <span className="status-text">{info.label}</span>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        {(trackingStatus === 'pending' || trackingStatus === 'preparing') && (
                            <button
                                onClick={() => setShowCancelModal(true)}
                                disabled={isCancelling}
                                style={{
                                    background: 'transparent',
                                    border: '1px solid #E8593C',
                                    color: '#E8593C',
                                    borderRadius: 6,
                                    padding: '4px 8px',
                                    fontSize: 12,
                                    cursor: isCancelling ? 'not-allowed' : 'pointer'
                                }}
                            >
                                {isCancelling ? 'Cancelling...' : 'Cancel Order'}
                            </button>
                        )}
                        <span
                            className="status-dot"
                            style={{
                                backgroundColor: info.color,
                                animation: trackingStatus === 'delivered' ? 'none' : 'tracking-pulse 1.2s infinite'
                            }}
                        />
                    </div>
                </div>
            </div>

            <StatusBar currentStatus={trackingStatus} />

            <div style={{
                display: 'grid',
                gap: 10,
                margin: '0 18px 18px',
                padding: 14,
                borderRadius: 14,
                background: 'rgba(255,255,255,0.04)',
                border: '1px solid var(--border)'
            }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                    <strong style={{ color: 'var(--text-primary)' }}>{stores.length} stores, {totalItems} items</strong>
                    <strong style={{ color: 'var(--text-primary)' }}>{formatPrice(order.totalAmount || 0, stores[0]?.currencyCode || 'USD')}</strong>
                </div>
                <div style={{ fontSize: 12, color: 'var(--muted)' }}>{order.deliveryAddress}</div>
                <div style={{ display: 'grid', gap: 8 }}>
                    {stores.map(store => (
                        <div
                            key={store.orderId}
                            style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                gap: 12,
                                alignItems: 'center',
                                padding: '10px 12px',
                                borderRadius: 10,
                                background: 'rgba(255,255,255,0.03)'
                            }}
                        >
                            <div>
                                <div style={{ color: 'var(--text-primary)', fontWeight: 600 }}>
                                    {(store.items || []).map(item => `x${item.quantity} ${item.productName}`).join(', ')}
                                </div>
                            </div>
                            <span style={{
                                fontSize: 11,
                                fontWeight: 700,
                                color: 'var(--text-primary)',
                                padding: '4px 8px',
                                borderRadius: 999,
                                background: 'rgba(255,255,255,0.08)'
                            }}>
                                {store.status}
                            </span>
                        </div>
                    ))}
                </div>
            </div>

            {showCancelModal && (
                <div className="cancel-modal-overlay">
                    <div className="cancel-modal-content">
                        <div className="cancel-icon-wrapper">
                            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#E8593C" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <circle cx="12" cy="12" r="10" />
                                <line x1="15" y1="9" x2="9" y2="15" />
                                <line x1="9" y1="9" x2="15" y2="15" />
                            </svg>
                        </div>
                        <h4 className="cancel-modal-title">Cancel Grouped Order?</h4>
                        <p className="cancel-modal-text">
                            This will cancel every store order inside #{order.orderGroupId}.
                        </p>
                        <div className="cancel-modal-actions">
                            <button className="btn-cancel-keep" onClick={() => setShowCancelModal(false)} disabled={isCancelling}>
                                No, Keep It
                            </button>
                            <button className="btn-cancel-confirm" onClick={confirmCancelOrder} disabled={isCancelling}>
                                {isCancelling ? 'Cancelling...' : 'Yes, Cancel'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
