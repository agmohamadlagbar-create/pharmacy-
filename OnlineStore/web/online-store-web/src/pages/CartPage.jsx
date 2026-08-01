import React, { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient'
import { formatPrice } from '../utils/formatPrice.js'
import './CartPage.css'

const CartPage = ({ cart, onBack, onUpdateQuantity, onRemoveItem, onCheckout, onNavigateStore }) => {
    const { t, i18n } = useTranslation()
    const isRTL = i18n?.language === 'ar'
    const [animateItems, setAnimateItems] = useState(false)

    useEffect(() => {
        if (cart.items.length > 0) {
            setAnimateItems(true)
        }
    }, [cart.items.length])

    const getImageUrl = (url) => {
        if (!url) return null
        if (url.startsWith('http')) return url
        return `${api.defaults.baseURL}${url}`
    }

    // Group items by store for merchant section layout
    const groupedItems = cart.items.reduce((acc, item) => {
        const storeKey = item.storeName || 'LagbarShop Partner'
        if (!acc[storeKey]) acc[storeKey] = []
        acc[storeKey].push(item)
        return acc
    }, {})

    return (
        <div className="cart-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Page Header */}
            <header className="cart-header">
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Shopping Cart')}</h1>
                <div className="header-badge">
                    {cart.items.length} {cart.items.length === 1 ? t('Item') : t('Items')}
                </div>
            </header>

            <main className="cart-content">
                {cart.items.length === 0 ? (
                    /* Empty Cart State */
                    <div className="empty-cart floating">
                        <div className="empty-cart-icon">
                            <span className="material-symbols-outlined" style={{ fontSize: 48, color: 'var(--primary)' }}>
                                shopping_bag
                            </span>
                        </div>
                        <h2 className="empty-cart-title">{t('Your cart is empty')}</h2>
                        <p className="empty-cart-text">
                            {t("Looks like you haven't added anything yet. Explore our top categories and discover what you need!")}
                        </p>
                        <button className="primary" onClick={onBack} style={{ width: 'auto', padding: '14px 32px' }}>
                            <span>{t('Start Shopping')}</span>
                            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>arrow_forward</span>
                        </button>
                    </div>
                ) : (
                    /* Active Cart Layout */
                    <div className="cart-layout">
                        {/* Cart Items Column */}
                        <div className="cart-items-section">
                            {Object.entries(groupedItems).map(([storeName, items]) => (
                                <div key={storeName} className="merchant-cart-group">
                                    <div className="merchant-group-header">
                                        <span className="material-symbols-outlined" style={{ color: 'var(--primary)' }}>storefront</span>
                                        <h3>{storeName}</h3>
                                        <span className="role-badge merchant" style={{ fontSize: '10px', padding: '2px 8px', marginInlineStart: 'auto' }}>
                                            {t('Verified Partner')}
                                        </span>
                                    </div>

                                    <div className="cart-items-list">
                                        {items.map((item, index) => (
                                            <div 
                                                key={item.productId} 
                                                className={`cart-item-card ${animateItems ? 'slide-up' : ''}`}
                                                style={{ animationDelay: `${index * 0.08}s` }}
                                            >
                                                <div className="item-img-wrapper">
                                                    {getImageUrl(item.imageUrl) ? (
                                                        <img src={getImageUrl(item.imageUrl)} alt={item.productName} className="item-img-real" />
                                                    ) : (
                                                        <div className="img-placeholder">
                                                            <span className="material-symbols-outlined" style={{ fontSize: 32, opacity: 0.3 }}>image</span>
                                                        </div>
                                                    )}
                                                </div>

                                                <div className="item-details">
                                                    <div className="item-header-info">
                                                        <h3>{item.productName}</h3>
                                                        {item.storeName && (
                                                            <span 
                                                                className="store-link"
                                                                onClick={(e) => {
                                                                    e.stopPropagation()
                                                                    if (onNavigateStore) onNavigateStore(item.storeUserId)
                                                                }}
                                                            >
                                                                <span className="material-symbols-outlined" style={{ fontSize: 13 }}>store</span>
                                                                {t('Sold by')} {item.storeName}
                                                            </span>
                                                        )}
                                                        <p className="item-price">{formatPrice(item.price, item.currencyCode)}</p>
                                                    </div>

                                                    <div className="item-actions">
                                                        <div className="qty-picker">
                                                            <button 
                                                                type="button"
                                                                onClick={() => onUpdateQuantity(item.productId, item.quantity - 1)}
                                                                disabled={item.quantity <= 1}
                                                                title={t('Decrease Quantity')}
                                                            >
                                                                <span className="material-symbols-outlined" style={{ fontSize: 16 }}>remove</span>
                                                            </button>
                                                            <span>{item.quantity}</span>
                                                            <button 
                                                                type="button"
                                                                onClick={() => onUpdateQuantity(item.productId, item.quantity + 1)}
                                                                title={t('Increase Quantity')}
                                                            >
                                                                <span className="material-symbols-outlined" style={{ fontSize: 16 }}>add</span>
                                                            </button>
                                                        </div>

                                                        <button className="remove-btn-premium" onClick={() => onRemoveItem(item.productId)} title={t('Remove Item')}>
                                                            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>delete</span>
                                                            <span>{t('Remove')}</span>
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            ))}
                        </div>

                        {/* Order Summary Sidebar */}
                        <div className="cart-summary-section">
                            <div className="summary-card">
                                <h2>{t('Order Summary')}</h2>
                                <div className="summary-details">
                                    <div className="summary-row">
                                        <span>{t('Subtotal')} ({cart.items.length} {cart.items.length === 1 ? t('item') : t('items')})</span>
                                        <span>{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                                    </div>
                                    <div className="summary-row">
                                        <span>{t('Delivery')}</span>
                                        <span className="status-badge success" style={{ padding: '2px 8px', fontSize: '12px' }}>{t('Free')}</span>
                                    </div>
                                    <div className="summary-row">
                                        <span>{t('Estimated Taxes')}</span>
                                        <span>{formatPrice(0, cart.items[0]?.currencyCode || 'USD')}</span>
                                    </div>
                                </div>

                                <div className="summary-divider" />

                                <div className="summary-row total-row">
                                    <span>{t('Total')}</span>
                                    <span className="total-amount">{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                                </div>

                                <button className="primary" onClick={onCheckout} style={{ padding: '16px', fontSize: '16px' }}>
                                    <span>{t('Proceed to Checkout')}</span>
                                    <span className="material-symbols-outlined">{isRTL ? 'arrow_back' : 'arrow_forward'}</span>
                                </button>

                                <p className="secure-checkout-text">
                                    <span className="material-symbols-outlined" style={{ fontSize: 16, color: 'var(--secondary)' }}>verified_user</span>
                                    <span>{t('Secure and encrypted checkout')}</span>
                                </p>
                            </div>
                        </div>
                    </div>
                )}
            </main>
        </div>
    )
}

export default CartPage
