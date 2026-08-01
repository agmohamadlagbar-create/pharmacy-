import React, { useCallback, useState, useMemo } from 'react'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import Skeleton from '../components/Skeleton.jsx'
import { useTranslation } from 'react-i18next'
import './MyShopsPage.css'

export default function MyShopsPage({ onBack, onNavigateStore, onAddToCart, onNavigateCart }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [isReordering, setIsReordering] = useState(null)
    const [expandedOrderId, setExpandedOrderId] = useState(null)
    const [searchQuery, setSearchQuery] = useState('')

    const fetchOrders = useCallback(async (page, limit) => {
        const { data } = await api.get(`/api/orders?status=Delivered&page=${page}&limit=${limit}`)
        return data
    }, [])

    const { items: orders, isLoading, hasMore, error, sentinelRef, initialLoad } = useInfiniteScroll(fetchOrders, {
        limit: 12
    })

    const groups = useMemo(() => {
        const result = []
        orders.forEach(o => {
            const gid = o.orderGroupId || `S-${o.id}`
            let g = result.find(x => x.id === gid)
            if (!g) {
                g = { id: gid, date: o.createdAtUtc, subOrders: [], total: 0, status: o.status }
                result.push(g)
            }
            g.subOrders.push(o)
            g.total += o.totalAmount
        })
        if (!searchQuery.trim()) return result
        const q = searchQuery.toLowerCase().trim()
        return result.filter(g => 
            g.id.toString().toLowerCase().includes(q) ||
            g.subOrders.some(so => so.storeName?.toLowerCase().includes(q) || so.items?.some(it => it.productName?.toLowerCase().includes(q)))
        )
    }, [orders, searchQuery])

    const statusColor = (s) => ({ Pending: 'var(--amber)', Preparing: 'var(--info)', ReadyForPickup: 'var(--secondary)', PickedUp: 'var(--purple)', OnTheWay: 'var(--info)', Delivered: 'var(--secondary)', Cancelled: 'var(--error)' }[s] || 'var(--on-surface-variant)')

    return (
        <div className="my-shops-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Sticky Header */}
            <header className="shops-header">
                <div className="header-top-row">
                    <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                        <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                    </button>
                    <div>
                        <h1>{t('My Shops & Delivered Orders')}</h1>
                        <p className="shops-subtitle">{t('Explore your past marketplace stores and reorder your favorite items.')}</p>
                    </div>
                </div>

                <div className="shops-search-bar">
                    <span className="material-symbols-outlined">search</span>
                    <input
                        type="text"
                        placeholder={t('Search store or order #...')}
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                    />
                    {searchQuery && (
                        <button className="clear-btn" onClick={() => setSearchQuery('')}>
                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>close</span>
                        </button>
                    )}
                </div>
            </header>

            {/* Main Content */}
            <main className="shops-main-content">
                {error && <div className="shops-error-alert">{error}</div>}

                {initialLoad ? (
                    <div className="shops-grid">
                        {Array.from({ length: 6 }).map((_, i) => (
                            <div key={i} className="card shop-skeleton-card">
                                <Skeleton width="100%" height="140px" borderRadius="var(--radius-lg)" />
                                <Skeleton width="70%" height="20px" style={{ marginTop: 12 }} />
                                <Skeleton width="40%" height="16px" style={{ marginTop: 8 }} />
                            </div>
                        ))}
                    </div>
                ) : groups.length > 0 ? (
                    <div className="shops-grid">
                        {groups.map(g => (
                            <div
                                key={g.id}
                                className={`card shop-order-card ${expandedOrderId === g.id ? 'expanded' : ''}`}
                                onClick={() => setExpandedOrderId(expandedOrderId === g.id ? null : g.id)}
                            >
                                <div className="shop-card-header">
                                    <div className="shop-card-title-box">
                                        <div className="store-avatar-mini">
                                            <span className="material-symbols-outlined" style={{ color: 'var(--primary)', fontSize: 24 }}>storefront</span>
                                        </div>
                                        <div>
                                            <h3 className="shop-card-title">{t('Order #{{id}}', { id: g.id })}</h3>
                                            <span className="shop-card-date">
                                                {new Date(g.date).toLocaleDateString()} • {t('{{count}} stores', { count: g.subOrders.length })}
                                            </span>
                                        </div>
                                    </div>
                                    <div className="shop-card-price-badge">
                                        <span className="status-tag" style={{ color: statusColor(g.status), background: `${statusColor(g.status)}15`, borderColor: `${statusColor(g.status)}30` }}>
                                            {g.status}
                                        </span>
                                        <span className="shop-card-price">
                                            {formatPrice(g.total, g.subOrders[0]?.currencyCode || 'USD')}
                                        </span>
                                    </div>
                                </div>

                                {/* List of Stores in Order */}
                                <div className="stores-list-preview">
                                    {g.subOrders.map(so => (
                                        <div 
                                            key={so.id} 
                                            className="store-preview-chip"
                                            onClick={(e) => {
                                                e.stopPropagation()
                                                if (onNavigateStore && so.storeUserId) onNavigateStore(so.storeUserId)
                                            }}
                                        >
                                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>store</span>
                                            <span>{so.storeName || t('Store')}</span>
                                            <span className="material-symbols-outlined arrow-icon" style={{ fontSize: 14 }}>arrow_forward</span>
                                        </div>
                                    ))}
                                </div>

                                {/* Expanded Details Drawer */}
                                {expandedOrderId === g.id && (
                                    <div className="shop-expanded-details" onClick={(e) => e.stopPropagation()}>
                                        <div className="details-divider" />
                                        {g.subOrders.map(so => (
                                            <div key={so.id} className="suborder-item-box">
                                                <div className="suborder-header">
                                                    <span 
                                                        className="suborder-store-link" 
                                                        onClick={() => onNavigateStore && so.storeUserId && onNavigateStore(so.storeUserId)}
                                                    >
                                                        {so.storeName || t('Unknown Store')}
                                                    </span>
                                                    <span className="suborder-part-id">{t('Part #{{id}}', { id: so.id })}</span>
                                                </div>

                                                <div className="suborder-items-list">
                                                    {so.items.map(item => (
                                                        <div key={item.id} className="suborder-item-row">
                                                            <span className="item-name-qty">{item.productName} × {item.quantity}</span>
                                                            <span className="item-price">{formatPrice(item.unitPrice * item.quantity, item.currencyCode)}</span>
                                                        </div>
                                                    ))}
                                                </div>
                                            </div>
                                        ))}

                                        <div className="reorder-action-bar">
                                            <button
                                                className="primary reorder-btn"
                                                disabled={isReordering === g.id}
                                                onClick={async (e) => {
                                                    e.stopPropagation()
                                                    if (isReordering) return
                                                    setIsReordering(g.id)
                                                    for (const so of g.subOrders) {
                                                        for (const item of so.items) {
                                                            if (onAddToCart) await onAddToCart(item.productId, item.quantity)
                                                        }
                                                    }
                                                    setIsReordering(null)
                                                    if (onNavigateCart) onNavigateCart()
                                                }}
                                            >
                                                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>shopping_cart_checkout</span>
                                                <span>{isReordering === g.id ? t('Adding to Cart...') : t('Order All Again')}</span>
                                            </button>
                                        </div>
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="card empty-shops-card">
                        <div className="empty-icon-bg">
                            <span className="material-symbols-outlined" style={{ fontSize: 44, color: 'var(--primary)' }}>storefront</span>
                        </div>
                        <h2>{t('No Delivered Orders Yet')}</h2>
                        <p>{t('Explore marketplace stores and place your first order!')}</p>
                        <button className="primary" onClick={onBack} style={{ marginTop: 16 }}>
                            {t('Start Shopping')}
                        </button>
                    </div>
                )}

                {hasMore && !initialLoad && (
                    <div ref={sentinelRef} style={{ padding: '20px', textAlign: 'center' }}>
                        {isLoading && <div className="spinner" style={{ width: 28, height: 28, border: '3px solid var(--outline-variant)', borderTop: '3px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />}
                    </div>
                )}
            </main>
        </div>
    )
}
