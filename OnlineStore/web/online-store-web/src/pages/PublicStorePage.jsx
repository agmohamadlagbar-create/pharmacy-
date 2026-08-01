import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import './CategoryProductsPage.css'
import './PublicStorePage.css'

export default function PublicStorePage({ 
    storeUserId, 
    onBack, 
    onSelectProduct, 
    onAddToCart, 
    favorites = [], 
    onToggleFavorite 
}) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [store, setStore] = useState(null)
    const [products, setProducts] = useState([])
    const [searchQuery, setSearchQuery] = useState('')
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        loadData()
    }, [storeUserId])

    async function loadData() {
        setLoading(true)
        try {
            const storeRes = await api.get(`/api/shops/${storeUserId}`)
            setStore(storeRes.data)

            const prodRes = await api.get(`/api/products/approved?storeUserId=${storeUserId}&limit=100`)
            const prodData = prodRes.data?.data || prodRes.data
            setProducts(Array.isArray(prodData) ? prodData : [])
        } catch (err) {
            console.error('Failed to load store data', err)
        } finally {
            setLoading(false)
        }
    }

    const filteredProducts = useMemo(() => {
        if (!searchQuery.trim()) return products
        const q = searchQuery.toLowerCase().trim()
        return products.filter(p => p.name?.toLowerCase().includes(q) || p.description?.toLowerCase().includes(q))
    }, [products, searchQuery])

    if (loading) return (
        <div className="public-store-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            <header className="store-nav-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Store')}</h1>
            </header>
            <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
                <div className="spinner" style={{ width: 40, height: 40, border: '4px solid var(--outline-variant)', borderTop: '4px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
            </div>
        </div>
    )

    if (!store) return (
        <div className="public-store-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            <header className="store-nav-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Store Not Found')}</h1>
            </header>
        </div>
    )

    const avatarUrl = store.profilePictureUrl ? `${api.defaults.baseURL}${store.profilePictureUrl}` : null
    const storeInitial = store.shopName ? store.shopName.charAt(0).toUpperCase() : 'S'
    const isFav = (id) => favorites.some(f => f.id === id)

    function pImageUrl(p) {
        if (!p.imageUrl) return null
        if (p.imageUrl.startsWith('http')) return p.imageUrl
        return `${api.defaults.baseURL}${p.imageUrl}`
    }

    return (
        <div className="public-store-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Top Cover Banner */}
            <div className="store-cover-banner">
                <button className="back-btn-circular banner-back-btn" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
            </div>

            {/* Store Header Info Card */}
            <div className="store-header-container">
                <div className="card store-hero-card">
                    <div className="store-hero-top">
                        <div className="store-avatar-ring">
                            {avatarUrl ? (
                                <img src={avatarUrl} alt={store.shopName} className="store-avatar-img" />
                            ) : (
                                <span className="store-avatar-letter">{storeInitial}</span>
                            )}
                        </div>

                        <div className="store-hero-details">
                            <div className="store-title-row">
                                <h1 className="store-title-name">{store.shopName}</h1>
                            </div>

                            <div className="store-meta-row">
                                <div className="meta-badge">
                                    <span className="material-symbols-outlined" style={{ color: 'var(--tertiary)', fontSize: 16 }}>star</span>
                                    <span className="meta-text-bold">4.9</span>
                                </div>
                                <span className="meta-sep">•</span>
                                <div className="meta-badge">
                                    <span className="material-symbols-outlined" style={{ fontSize: 16 }}>location_on</span>
                                    <span>{store.city || store.location || 'Official Merchant'}</span>
                                </div>
                                {(store.openingTime && store.closingTime) && (
                                    <>
                                        <span className="meta-sep">•</span>
                                        <div className="meta-badge">
                                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>schedule</span>
                                            <span>{store.openingTime} – {store.closingTime}</span>
                                        </div>
                                    </>
                                )}
                            </div>

                            {store.description && (
                                <p className="store-description">{store.description}</p>
                            )}
                        </div>
                    </div>
                </div>
            </div>

            {/* Sticky Search & Filter Toolbar */}
            <div className="store-toolbar">
                <div className="toolbar-inner">
                    <div className="toolbar-left">
                        <h2 className="toolbar-heading">{t('Store Products')}</h2>
                        <span className="products-count-chip">{filteredProducts.length}</span>
                    </div>

                    <div className="search-field-box">
                        <span className="material-symbols-outlined">search</span>
                        <input
                            type="text"
                            placeholder={t('Search in this store...')}
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                        />
                        {searchQuery && (
                            <button className="clear-search-btn" onClick={() => setSearchQuery('')}>
                                <span className="material-symbols-outlined" style={{ fontSize: 16 }}>close</span>
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Products Grid Section */}
            <main className="store-products-main">
                {filteredProducts.length === 0 ? (
                    <div className="card empty-products-card">
                        <div className="empty-icon-bg">
                            <span className="material-symbols-outlined" style={{ fontSize: 44, color: 'var(--primary)' }}>storefront</span>
                        </div>
                        <h3>{t('No Products Found')}</h3>
                        <p>
                            {searchQuery ? t('No products match your search query.') : t('This store has no products listed at the moment.')}
                        </p>
                    </div>
                ) : (
                    <div className="products-grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))' }}>
                        {filteredProducts.map(product => (
                            <div
                                key={product.id}
                                className="product-card-web"
                                onClick={() => onSelectProduct && onSelectProduct(product)}
                            >
                                <div className="product-img-wrapper">
                                    <span className="product-price-tag">{formatPrice(product.price, product.currencyCode)}</span>
                                    <button 
                                        className={`fav-btn ${isFav(product.id) ? 'active' : ''}`}
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            onToggleFavorite && onToggleFavorite(product)
                                        }}
                                    >
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill={isFav(product.id) ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2">
                                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l8.89-8.89 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                                        </svg>
                                    </button>
                                    {pImageUrl(product) ? (
                                        <img src={pImageUrl(product)} alt={product.name} />
                                    ) : (
                                        <div className="product-img-placeholder">
                                            <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.1">
                                                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                                                <circle cx="8.5" cy="8.5" r="1.5"></circle>
                                                <polyline points="21 15 16 10 5 21"></polyline>
                                            </svg>
                                        </div>
                                    )}
                                </div>
                                <div className="product-info-web">
                                    <h3>{product.name}</h3>
                                    {product.description && <p>{product.description}</p>}

                                    <button
                                        className="add-to-cart-btn-outline"
                                        disabled={product.stockQuantity <= 0}
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            if (product.stockQuantity > 0 && onAddToCart) {
                                                onAddToCart(product.id)
                                            } else if (onSelectProduct) {
                                                onSelectProduct(product)
                                            }
                                        }}
                                    >
                                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                            <circle cx="9" cy="21" r="1"></circle>
                                            <circle cx="20" cy="21" r="1"></circle>
                                            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                                        </svg>
                                        {product.stockQuantity > 0 ? t('Add to Cart') : t('Out of Stock')}
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </main>
        </div>
    )
}
