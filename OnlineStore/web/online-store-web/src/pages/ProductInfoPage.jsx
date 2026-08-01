import React, { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import Skeleton from '../components/Skeleton.jsx'
import CartIconButton from '../components/CartIconButton'
import './ProductInfoPage.css'

const ProductInfoPage = ({ 
    productId, 
    onBack, 
    onSelectRelatedProduct, 
    onAddToCart, 
    onNavigateCart, 
    cartCount, 
    onNavigateStore,
    favorites = [],
    onToggleFavorite,
    currentUserId
}) => {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [productDetails, setProductDetails] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [activeTab, setActiveTab] = useState('reviews')
    const [activeImageUrl, setActiveImageUrl] = useState(null)
    const [reviewForm, setReviewForm] = useState({ rating: 5, comment: '' })
    const [submittingReview, setSubmittingReview] = useState(false)
    const [quantity, setQuantity] = useState(1)

    useEffect(() => {
        fetchProductDetails()
    }, [productId])

    useEffect(() => {
        if (productDetails && currentUserId === productDetails.product.storeUserId) {
            setActiveTab('statistics')
        }
    }, [productDetails, currentUserId])

    async function fetchProductDetails() {
        setLoading(true)
        setError(null)
        try {
            const { data } = await api.get(`/api/products/${productId}/details`)
            setProductDetails(data)
            if (data.product?.imageUrl) setActiveImageUrl(data.product.imageUrl)
        } catch (err) {
            console.error('Failed to fetch product details', err)
            setError('Failed to load product details.')
        } finally {
            setLoading(false)
        }
    }

    async function handleReviewSubmit(e) {
        e.preventDefault()
        if (!reviewForm.comment.trim()) return
        setSubmittingReview(true)
        try {
            await api.post(`/api/products/${productId}/reviews`, {
                rating: parseInt(reviewForm.rating),
                comment: reviewForm.comment
            })
            setReviewForm({ rating: 5, comment: '' })
            fetchProductDetails()
        } catch (err) {
            alert(t(err?.response?.data?.error || 'Failed to post review'))
        } finally {
            setSubmittingReview(false)
        }
    }

    function getImageUrl(url) {
        if (!url) return null
        if (url.startsWith('http')) return url
        return `${api.defaults.baseURL}${url}`
    }

    if (loading) {
        return (
            <div className="product-info-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
                <header className="product-info-header">
                    <button className="back-btn-circular" onClick={onBack}>
                        <span className="material-symbols-outlined">arrow_back</span>
                    </button>
                    <h1>{t('Product Details')}</h1>
                    <div style={{ width: 44 }} />
                </header>
                <div style={{ maxWidth: 'var(--container-max)', margin: '0 auto', width: '100%', padding: '20px' }}>
                    <Skeleton width="100%" height="400px" borderRadius="var(--radius-xl)" />
                    <Skeleton width="60%" height="36px" style={{ marginTop: '24px' }} />
                    <Skeleton width="40%" height="24px" style={{ marginTop: '12px' }} />
                </div>
            </div>
        )
    }

    if (error || !productDetails) {
        return (
            <div className="product-info-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
                <header className="product-info-header">
                    <button className="back-btn-circular" onClick={onBack}>
                        <span className="material-symbols-outlined">arrow_back</span>
                    </button>
                    <h1>{t('Error')}</h1>
                    <div style={{ width: 44 }} />
                </header>
                <div className="error-message">{t(error || 'Product not found')}</div>
            </div>
        )
    }

    const { product, totalSales, inStock, averageRating, totalReviews, latestReviews, relatedProducts, dailySales } = productDetails
    const isOwner = currentUserId === product.storeUserId
    const availabilityPercentage = product.originalQuantity > 0 
        ? Math.round((inStock / product.originalQuantity) * 100) 
        : 0

    const isFav = favorites.some(p => p.id === productId)

    return (
        <div className="product-info-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Top Header */}
            <header className="product-info-header">
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Product Details')}</h1>
                <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <button 
                        className={`fav-btn ${isFav ? 'active' : ''}`}
                        onClick={() => onToggleFavorite && onToggleFavorite(product)}
                        style={{ position: 'relative', top: 0, right: 0, insetInlineEnd: 0 }}
                        title={t('Favorite')}
                    >
                        <span className="material-symbols-outlined" style={{ fontVariationSettings: isFav ? "'FILL' 1" : "'FILL' 0" }}>favorite</span>
                    </button>
                    <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
                </div>
            </header>

            {/* Main Product Showcase */}
            <main className="product-showcase-container">
                <div className="product-showcase-grid">
                    {/* Left Column: Image Gallery */}
                    <div className="product-gallery-section">
                        <div className="product-main-image-wrap">
                            {getImageUrl(activeImageUrl) ? (
                                <img src={getImageUrl(activeImageUrl)} alt={product.name} fetchPriority="high" decoding="async" />
                            ) : (
                                <div className="product-placeholder-img">
                                    <span className="material-symbols-outlined" style={{ fontSize: 64, opacity: 0.3 }}>image</span>
                                </div>
                            )}
                        </div>

                        {/* Thumbnails list */}
                        {product.additionalImageUrls && product.additionalImageUrls.length > 0 && (
                            <div className="product-thumbnails-row">
                                <div 
                                    className={`product-thumb ${activeImageUrl === product.imageUrl ? 'active' : ''}`} 
                                    onClick={() => setActiveImageUrl(product.imageUrl)}
                                >
                                    <img src={getImageUrl(product.imageUrl)} alt="" loading="lazy" decoding="async" />
                                </div>
                                {product.additionalImageUrls.map((url, i) => (
                                    <div 
                                        key={i} 
                                        className={`product-thumb ${activeImageUrl === url ? 'active' : ''}`} 
                                        onClick={() => setActiveImageUrl(url)}
                                    >
                                        <img src={getImageUrl(url)} alt="" loading="lazy" decoding="async" />
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>

                    {/* Right Column: Product Details & Buying Actions */}
                    <div className="product-info-details-section">
                        <div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '16px' }}>
                                <div>
                                    <span className={`status-badge ${inStock > 0 ? 'success' : 'error'}`}>
                                        {inStock > 0 ? t('In Stock') : t('Out of Stock')}
                                    </span>
                                    <h1 className="product-title-text">{product.name}</h1>
                                </div>
                                <div style={{ textAlign: isRTL ? 'left' : 'right' }}>
                                    <p className="product-price-text">{formatPrice(product.price, product.currencyCode)}</p>
                                    <p style={{ fontSize: '12px', color: 'var(--on-surface-variant)' }}>{t('VAT included')}</p>
                                </div>
                            </div>

                            {/* Ratings & Status */}
                            <div style={{ display: 'flex', alignItems: 'center', gap: '16px', marginTop: '12px' }}>
                                <div className="product-rating-chip">
                                    <span className="material-symbols-outlined text-amber" style={{ fontVariationSettings: "'FILL' 1", fontSize: 18, color: 'var(--tertiary)' }}>star</span>
                                    <span style={{ fontWeight: 700, fontSize: '14px' }}>{averageRating.toFixed(1)}</span>
                                    <span style={{ color: 'var(--on-surface-variant)', fontSize: '13px' }}>({totalReviews} {t('Reviews')})</span>
                                </div>
                            </div>
                        </div>

                        {/* Description */}
                        {product.description && (
                            <p className="product-desc-text">{product.description}</p>
                        )}

                        {/* Quantity Selector & Action Buttons */}
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                                <div className="qty-picker">
                                    <button onClick={() => setQuantity(Math.max(1, quantity - 1))}>
                                        <span className="material-symbols-outlined">remove</span>
                                    </button>
                                    <span>{quantity}</span>
                                    <button onClick={() => setQuantity(quantity + 1)}>
                                        <span className="material-symbols-outlined">add</span>
                                    </button>
                                </div>

                                <button
                                    className="primary"
                                    disabled={inStock <= 0}
                                    style={{ flex: 1, padding: '16px' }}
                                    onClick={() => {
                                        if (inStock > 0) {
                                            onAddToCart(productId, quantity)
                                        } else {
                                            alert(t('This product is out of stock.'))
                                        }
                                    }}
                                >
                                    <span className="material-symbols-outlined">shopping_cart</span>
                                    <span>{inStock > 0 ? t('Add to Cart') : t('Out of Stock')}</span>
                                </button>
                            </div>
                        </div>

                        {/* Seller Card */}
                        {product.storeName && (
                            <div 
                                className="product-seller-card"
                                onClick={() => { if (onNavigateStore) onNavigateStore(product.storeUserId) }}
                            >
                                <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                                    <div className="seller-avatar-icon">
                                        <span className="material-symbols-outlined">store</span>
                                    </div>
                                    <div>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                            <span style={{ fontWeight: 700, fontSize: '15px' }}>{product.storeName}</span>
                                            <span className="role-badge merchant" style={{ fontSize: '10px', padding: '2px 8px' }}>{t('Store')}</span>
                                        </div>
                                        <span style={{ fontSize: '12px', color: 'var(--primary)' }}>{t('View Merchant Showcase →')}</span>
                                    </div>
                                </div>
                                <span className="material-symbols-outlined" style={{ color: 'var(--on-surface-variant)' }}>
                                    {isRTL ? 'chevron_left' : 'chevron_right'}
                                </span>
                            </div>
                        )}

                        {/* Quick Perks */}
                        <div className="product-perks-grid">
                            <div className="perk-item">
                                <span className="material-symbols-outlined" style={{ color: 'var(--secondary)' }}>local_shipping</span>
                                <div>
                                    <p style={{ fontWeight: 700, fontSize: '13px' }}>{t('Fast Delivery')}</p>
                                    <p style={{ fontSize: '11px', color: 'var(--on-surface-variant)' }}>{t('Direct dispatch')}</p>
                                </div>
                            </div>
                            <div className="perk-item">
                                <span className="material-symbols-outlined" style={{ color: 'var(--secondary)' }}>verified_user</span>
                                <div>
                                    <p style={{ fontWeight: 700, fontSize: '13px' }}>{t('Verified Seller')}</p>
                                    <p style={{ fontSize: '11px', color: 'var(--on-surface-variant)' }}>{t('Guaranteed Quality')}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Tabs Section: Statistics / Reviews / Related */}
                <div className="product-tabs-section">
                    <div className="seg" style={{ marginBottom: '24px' }}>
                        {isOwner && (
                            <button className={activeTab === 'statistics' ? 'active' : ''} onClick={() => setActiveTab('statistics')}>
                                {t('Statistics')}
                            </button>
                        )}
                        <button className={activeTab === 'reviews' ? 'active' : ''} onClick={() => setActiveTab('reviews')}>
                            {t('Reviews')} ({totalReviews})
                        </button>
                        <button className={activeTab === 'related' ? 'active' : ''} onClick={() => setActiveTab('related')}>
                            {t('Related Products')}
                        </button>
                    </div>

                    <div className="product-tab-body">
                        {/* Statistics Tab (Merchant Only) */}
                        {activeTab === 'statistics' && isOwner && (
                            <div className="statistics-tab">
                                <div className="stat-card">
                                    <h3>{t('Sales Overview')}: {totalSales >= 1000 ? (totalSales/1000).toFixed(1) + 'K' : totalSales} {t('Sold')}</h3>
                                    <div className="bar-chart">
                                        {dailySales && dailySales.length > 0 ? dailySales.map((s, i) => {
                                            const count = s.count !== undefined ? s.count : (s.Count !== undefined ? s.Count : 0)
                                            const maxCount = Math.max(...dailySales.map(d => d.count || d.Count || 0), 1)
                                            const height = Math.max((count / maxCount) * 100, 5) 
                                            return (
                                                <div key={i} className="bar">
                                                    <div className="fill" style={{ height: `${height}%` }}>
                                                        {count > 0 && <span className="count-hint">{count}</span>}
                                                    </div>
                                                    <span className="bar-label">{s.day || s.Day}</span>
                                                </div>
                                            )
                                        }) : (
                                            <div className="no-stats-msg">{t('No sales data available yet.')}</div>
                                        )}
                                    </div>
                                </div>
                                <div className="stat-card">
                                    <h3>{t('Stock Inventory')}: <span className={inStock > 0 ? 'status-in-stock' : 'status-out-stock'}>{inStock > 0 ? t('In Stock') : t('Out of Stock')}</span></h3>
                                    <div className="donut-chart-container">
                                        <div className="donut-chart" style={{'--percentage': `${availabilityPercentage}%`}}>
                                            <div className="donut-hole">
                                                <span className="percent">{availabilityPercentage}%</span>
                                                <span className="label">{t('Remaining')}</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Reviews Tab */}
                        {activeTab === 'reviews' && (
                            <div className="reviews-tab">
                                <div className="reviews-summary-card">
                                    <span style={{ fontSize: '32px', fontWeight: 800, color: 'var(--primary)' }}>{averageRating.toFixed(1)}</span>
                                    <div>
                                        <div style={{ color: 'var(--tertiary)', fontSize: '18px' }}>
                                            {'★'.repeat(Math.round(averageRating))}{'☆'.repeat(5 - Math.round(averageRating))}
                                        </div>
                                        <span style={{ fontSize: '13px', color: 'var(--on-surface-variant)' }}>Based on {totalReviews} reviews</span>
                                    </div>
                                </div>

                                <form onSubmit={handleReviewSubmit} className="add-review-form">
                                    <h4>{t('Write a Review')}</h4>
                                    <div className="star-rating-input">
                                        {[1, 2, 3, 4, 5].map(s => (
                                            <span 
                                                key={s} 
                                                className={`star ${reviewForm.rating >= s ? 'filled' : ''}`}
                                                onClick={() => setReviewForm({ ...reviewForm, rating: s })}
                                            >
                                                ★
                                            </span>
                                        ))}
                                    </div>
                                    <textarea 
                                        placeholder={t('Share your experience with this product...')}
                                        className="review-textarea"
                                        value={reviewForm.comment}
                                        onChange={e => setReviewForm({ ...reviewForm, comment: e.target.value })}
                                        required
                                    />
                                    <button type="submit" className="primary" disabled={submittingReview} style={{ width: 'auto', padding: '10px 24px' }}>
                                        {submittingReview ? t('Posting...') : t('Submit Review')}
                                    </button>
                                </form>

                                <div className="review-list">
                                    {latestReviews.length > 0 ? latestReviews.map(r => (
                                        <div key={r.id} className="review-item">
                                            <div className="review-header">
                                                <strong>{r.userName}</strong>
                                                <div style={{ color: 'var(--tertiary)', fontSize: '14px' }}>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</div>
                                            </div>
                                            <p className="comment">"{r.comment}"</p>
                                        </div>
                                    )) : <div className="no-reviews">{t('No reviews yet.')}</div>}
                                </div>
                            </div>
                        )}

                        {/* Related Products Tab */}
                        {activeTab === 'related' && (
                            <div className="related-tab">
                                <div className="related-products-grid">
                                    {relatedProducts.length > 0 ? relatedProducts.map(rp => (
                                        <div key={rp.id} className="related-card" onClick={() => onSelectRelatedProduct && onSelectRelatedProduct(rp)}>
                                            <div className="related-img">
                                                {getImageUrl(rp.imageUrl) ? <img src={getImageUrl(rp.imageUrl)} alt={rp.name} /> : t('No Image')}
                                            </div>
                                            <p style={{ fontWeight: 600, fontSize: '13px', margin: '4px 0 2px' }}>{rp.name}</p>
                                            <p style={{ color: 'var(--primary)', fontWeight: 700, fontSize: '13px' }}>{formatPrice(rp.price, rp.currencyCode)}</p>
                                        </div>
                                    )) : <p>{t('No related products found.')}</p>}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </main>
        </div>
    )
}

export default ProductInfoPage
