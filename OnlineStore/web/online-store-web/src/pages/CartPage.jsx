import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import './CartPage.css';
import { api } from '../apiClient';

const CartPage = ({ cart, onBack, onUpdateQuantity, onRemoveItem, onCheckout, onNavigateStore }) => {
    const { t } = useTranslation();
    const [animateItems, setAnimateItems] = useState(false);

    useEffect(() => {
        if (cart.items.length > 0) {
            setAnimateItems(true);
        }
    }, [cart.items.length]);

    const getImageUrl = (url) => {
        if (!url) return null;
        if (url.startsWith('http')) return url;
        return `${api.defaults.baseURL}${url}`;
    };

    return (
        <div className="cart-page fade-in">
            <header className="cart-header glass-header">
                <button className="icon-btn back-btn" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <line x1="19" y1="12" x2="5" y2="12"></line>
                        <polyline points="12 19 5 12 12 5"></polyline>
                    </svg>
                </button>
                <h1>{t('Shopping Cart')}</h1>
                <div className="header-badge">{cart.items.length} {cart.items.length === 1 ? t('Item') : t('Items')}</div>
            </header>

            <main className="cart-content">
                {cart.items.length === 0 ? (
                    <div className="empty-cart floating">
                        <div className="empty-cart-icon">
                            <svg width="80" height="80" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" opacity="0.8">
                                <circle cx="9" cy="21" r="2"></circle>
                                <circle cx="20" cy="21" r="2"></circle>
                                <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                            </svg>
                        </div>
                        <h2 className="empty-cart-title">{t('Your cart is empty')}</h2>
                        <p className="empty-cart-text">{t("Looks like you haven't added anything yet. Explore our top categories and discover what you need!")}</p>
                        <button className="primary-glass start-shopping-btn" onClick={onBack}>
                            {t('Start Shopping')}
                        </button>
                    </div>
                ) : (
                    <div className="cart-layout">
                        <div className="cart-items-section">
                            <div className="cart-items-list">
                                {cart.items.map((item, index) => (
                                    <div 
                                        key={item.productId} 
                                        className={`cart-item-card premium-card ${animateItems ? 'slide-up' : ''}`}
                                        style={{ animationDelay: `${index * 0.08}s` }}
                                    >
                                        <div className="item-img-wrapper">
                                            {getImageUrl(item.imageUrl) ? (
                                                <img src={getImageUrl(item.imageUrl)} alt={item.productName} className="item-img-real" />
                                            ) : (
                                                <div className="img-placeholder">
                                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" opacity="0.5">
                                                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                                                        <circle cx="8.5" cy="8.5" r="1.5"></circle>
                                                        <polyline points="21 15 16 10 5 21"></polyline>
                                                    </svg>
                                                </div>
                                            )}
                                        </div>
                                        <div className="item-details">
                                            <div className="item-header-info">
                                                <h3>{item.productName}</h3>
                                                {item.storeName && (
                                                    <span 
                                                        style={{ fontSize: '11px', color: 'var(--primary)', display: 'inline-flex', alignItems: 'center', gap: '4px', marginTop: '2px', cursor: 'pointer', textDecoration: 'underline' }}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            if (onNavigateStore) onNavigateStore(item.storeUserId);
                                                        }}
                                                    >
                                                        <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                                            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                                                            <polyline points="9 22 9 12 15 12 15 22"></polyline>
                                                        </svg>
                                                        from {item.storeName}
                                                    </span>
                                                )}
                                                <p className="item-price">${item.price.toFixed(2)}</p>
                                            </div>
                                            <div className="item-actions">
                                                <div className="quantity-controls glass-controls">
                                                    <button 
                                                        className="qty-btn"
                                                        onClick={() => onUpdateQuantity(item.productId, item.quantity - 1)}
                                                        disabled={item.quantity <= 1}
                                                    >
                                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3"><line x1="5" y1="12" x2="19" y2="12"></line></svg>
                                                    </button>
                                                    <span className="qty-value">{item.quantity}</span>
                                                    <button 
                                                        className="qty-btn"
                                                        onClick={() => onUpdateQuantity(item.productId, item.quantity + 1)}
                                                    >
                                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
                                                    </button>
                                                </div>
                                                <button className="remove-btn-premium" onClick={() => onRemoveItem(item.productId)} title="Remove Item">
                                                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round">
                                                        <polyline points="3 6 5 6 21 6"></polyline>
                                                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                                                    </svg>
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="cart-summary-section fade-in" style={{ animationDelay: '0.3s' }}>
                            <div className="cart-summary-card premium-card">
                                <h2>{t('Order Summary')}</h2>
                                <div className="summary-details">
                                    <div className="summary-row">
                                        <span>{t('Subtotal')} ({cart.items.length} {cart.items.length === 1 ? t('item') : t('items')})</span>
                                        <span>${cart.totalAmount.toFixed(2)}</span>
                                    </div>
                                    <div className="summary-row">
                                        <span>{t('Delivery')}</span>
                                        <span className="free-text">{t('Free')}</span>
                                    </div>
                                    <div className="summary-row">
                                        <span>{t('Taxes')}</span>
                                        <span>$0.00</span>
                                    </div>
                                </div>
                                <div className="summary-divider"></div>
                                <div className="summary-row total-row">
                                    <span>{t('Total')}</span>
                                    <span className="total-amount">${cart.totalAmount.toFixed(2)}</span>
                                </div>
                                <button className="primary-gradient checkout-btn" onClick={onCheckout}>
                                    <span>{t('Proceed to Checkout')}</span>
                                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                        <line x1="5" y1="12" x2="19" y2="12"></line>
                                        <polyline points="12 5 19 12 12 19"></polyline>
                                    </svg>
                                </button>
                                <p className="secure-checkout-text">
                                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg>
                                    {t('Secure and encrypted checkout')}
                                </p>
                            </div>
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
};

export default CartPage;
