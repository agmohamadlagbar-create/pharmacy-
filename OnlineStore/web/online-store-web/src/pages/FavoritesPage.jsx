import React from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import './FavoritesPage.css'

export default function FavoritesPage({
    favorites = [],
    onBack,
    onSelectProduct,
    onToggleFavorite,
    onAddToCart,
    onNavigateCart,
    cartCount = 0
}) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n?.language === 'ar'

    function imageUrl(p) {
        if (!p.imageUrl) return null
        if (p.imageUrl.startsWith('http')) return p.imageUrl
        return `${api.defaults.baseURL}${p.imageUrl}`
    }

    const handleAddAllToCart = () => {
        if (!onAddToCart || favorites.length === 0) return
        favorites.forEach(product => {
            onAddToCart(product.id, 1)
        })
    }

    return (
        <div className="favorites-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Sticky Glass Header */}
            <header className="favorites-header">
                <div className="header-left">
                    <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                        <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                    </button>
                    <div>
                        <h1>{t('Your Saved Items')}</h1>
                        <p className="favorites-subtitle">
                            <span className="count-highlight">{favorites.length}</span> {t('items found in your wishlist')}
                        </p>
                    </div>
                </div>

                <div className="header-right">
                    {favorites.length > 0 && (
                        <button className="primary add-all-btn" onClick={handleAddAllToCart}>
                            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>shopping_cart</span>
                            <span>{t('Add All to Cart')}</span>
                        </button>
                    )}
                </div>
            </header>

            <main className="favorites-content">
                {favorites.length > 0 ? (
                    <div className="favorites-grid">
                        {favorites.map(product => (
                            <div
                                key={product.id}
                                className="fav-card"
                                onClick={() => onSelectProduct && onSelectProduct(product)}
                            >
                                <div className="fav-img-wrapper">
                                    {imageUrl(product) ? (
                                        <img src={imageUrl(product)} alt={product.name} className="fav-img" />
                                    ) : (
                                        <div className="fav-img-placeholder">
                                            <span className="material-symbols-outlined" style={{ fontSize: 44, color: 'var(--outline-variant)' }}>image</span>
                                        </div>
                                    )}

                                    {/* Remove button */}
                                    <button
                                        className="fav-remove-btn"
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            onToggleFavorite(product)
                                        }}
                                        title={t('Remove from favorites')}
                                    >
                                        <span className="material-symbols-outlined" style={{ fontSize: 18 }}>close</span>
                                    </button>

                                    {/* Category tag */}
                                    {product.categoryName && (
                                        <div className="fav-category-tag">
                                            <span>{product.categoryName}</span>
                                        </div>
                                    )}
                                </div>

                                <div className="fav-info">
                                    <h3 className="fav-title">{product.name}</h3>
                                    <div className="fav-price-row">
                                        <span className="fav-price">{formatPrice(product.price, product.currencyCode)}</span>
                                        {product.oldPrice && product.oldPrice > product.price && (
                                            <span className="fav-old-price">{formatPrice(product.oldPrice, product.currencyCode)}</span>
                                        )}
                                    </div>

                                    <button
                                        className="primary fav-add-cart-btn"
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            if (onAddToCart) onAddToCart(product.id, 1)
                                        }}
                                    >
                                        <span className="material-symbols-outlined" style={{ fontSize: 18 }}>shopping_cart</span>
                                        <span>{t('Add to Cart')}</span>
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                ) : (
                    <div className="empty-cart-container" style={{ margin: '60px 0', padding: '60px 24px' }}>
                        <div className="empty-cart-icon-bg" style={{ width: 80, height: 80 }}>
                            <span className="material-symbols-outlined" style={{ fontSize: 40, color: 'var(--primary)' }}>favorite</span>
                        </div>
                        <h2 className="empty-cart-title">{t('Your Wishlist is Empty')}</h2>
                        <p className="empty-cart-subtitle">{t('Save items you love by tapping the heart icon while exploring.')}</p>
                        <button className="primary" onClick={onBack} style={{ marginTop: '20px', padding: '12px 32px' }}>
                            {t('Start Shopping')}
                        </button>
                    </div>
                )}
            </main>
        </div>
    )
}
