import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import Skeleton from '../components/Skeleton.jsx'

export default function StoreOrdersPage({ onBack }) {
    const { t } = useTranslation()
    const fetchOrders = async (page, limit) => {
        const { data } = await api.get(`/api/orders?page=${page}&limit=${limit}&_=${Date.now()}`)
        return data
    }

    const {
        items: orders,
        isLoading,
        hasMore,
        sentinelRef,
        initialLoad,
        reset
    } = useInfiniteScroll(fetchOrders, { limit: 9 })

    async function updateStatus(orderId, newStatus) {
        try {
            await api.put(`/api/orders/${orderId}/status`, { status: newStatus })
            reset()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        }
    }

    const statusColor = (s) => ({ Pending: '#f59e0b', Preparing: '#3b82f6', ReadyForPickup: '#22c55e', PickedUp: '#8b5cf6', OnTheWay: '#06b6d4', Delivered: '#22c55e', Cancelled: '#ef4444' }[s] || '#888')
    const nextAction = (s) => ({ Pending: 'Preparing', Preparing: 'ReadyForPickup' }[s])

    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <button onClick={onBack} className="glass-btn">← {t('Back')}</button>
                <h2 style={{ margin: 0, color: 'var(--text-primary)' }}>{t('Store Orders')}</h2>
                <button onClick={reset} className="glass-btn">↻ {t('Refresh')}</button>
            </div>

            {initialLoad ? (
                <div style={{ display: 'grid', gap: '12px' }}>
                    {Array.from({ length: 5 }).map((_, i) => (
                        <div key={i} style={{ background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                                <Skeleton width="100px" height="20px" />
                                <Skeleton width="60px" height="24px" borderRadius="12px" />
                            </div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                <Skeleton width="80%" height="16px" />
                                <Skeleton width="60%" height="16px" />
                            </div>
                        </div>
                    ))}
                </div>
            ) : orders.length === 0 && !isLoading ? (
                <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '40px' }}>{t('No orders yet.')}</div>
            ) : (
                <div style={{ display: 'grid', gap: '12px' }}>
                    {orders.map(o => (
                        <div key={o.id} style={{ background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                                <strong style={{ color: 'var(--text-primary)' }}>{t('Order')} #{o.id}</strong>
                                <span style={{ fontSize: '12px', fontWeight: '600', color: statusColor(o.status), background: `${statusColor(o.status)}15`, padding: '4px 10px', borderRadius: '12px' }}>{t(o.status)}</span>
                            </div>
                            <div style={{ fontSize: '13px', color: 'var(--muted)' }}>
                                {o.items.map(i => <div key={i.id}>{i.productName} × {i.quantity} @ {formatPrice(i.unitPrice, i.currencyCode)}</div>)}
                            </div>
                            <div style={{ marginTop: '8px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <span style={{ fontWeight: '600', color: 'var(--text-primary)' }}>{t('Total:')} {formatPrice(o.totalAmount, o.currencyCode)}</span>
                                {nextAction(o.status) && (
                                    <button onClick={() => updateStatus(o.id, nextAction(o.status))}
                                        className="primary">
                                        {t('Mark as')} {t(nextAction(o.status))}
                                    </button>
                                )}
                            </div>
                            <div style={{ fontSize: '11px', color: 'var(--muted)', marginTop: '6px' }}>{new Date(o.createdAtUtc).toLocaleString()}</div>
                        </div>
                    ))}
                    
                    {isLoading && !initialLoad && Array.from({ length: 3 }).map((_, i) => (
                        <div key={`more-${i}`} style={{ background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                                <Skeleton width="100px" height="20px" />
                                <Skeleton width="60px" height="24px" borderRadius="12px" />
                            </div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                <Skeleton width="80%" height="16px" />
                                <Skeleton width="60%" height="16px" />
                            </div>
                        </div>
                    ))}
                    
                    {hasMore && !initialLoad && <div ref={sentinelRef} style={{ height: '1px' }} />}
                    
                    {!hasMore && orders.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px' }}>{t('End of orders')}</div>
                    )}
                </div>
            )}
        </div>
    )
}
