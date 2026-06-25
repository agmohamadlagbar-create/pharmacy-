import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient.js';
import { formatPrice } from '../utils/formatPrice.js';
import Skeleton from '../components/Skeleton.jsx';
import './ProductInfoPage.css';
import CartIconButton from '../components/CartIconButton';

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
    const { t } = useTranslation();
    const [productDetails, setProductDetails] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [activeTab, setActiveTab] = useState('statistics');
    const [activeImageUrl, setActiveImageUrl] = useState(null);
    const [reviewForm, setReviewForm] = useState({ rating: 5, comment: '' });
    const [submittingReview, setSubmittingReview] = useState(false);

    useEffect(() => {
        fetchProductDetails();
    }, [productId]);

    useEffect(() => {
        if (productDetails && currentUserId !== productDetails.product.storeUserId && activeTab === 'statistics') {
            setActiveTab('reviews');
        }
    }, [productDetails, currentUserId, activeTab]);

    async function fetchProductDetails() {
        setLoading(true);
        setError(null);
        try {
            const { data } = await api.get(`/api/products/${productId}/details`);
            console.log('Product Details Payload:', data);
            if (data.dailySales) console.log('Daily Sales Data:', data.dailySales);
            setProductDetails(data);
            if (data.product?.imageUrl) setActiveImageUrl(data.product.imageUrl);
        } catch (err) {
            console.error('Failed to fetch product details', err);
            setError('Failed to load product details.');
        } finally {
            setLoading(false);
        }
    }

    async function handleReviewSubmit(e) {
        e.preventDefault();
        if (!reviewForm.comment.trim()) return;
        setSubmittingReview(true);
        try {
            await api.post(`/api/products/${productId}/reviews`, {
                rating: parseInt(reviewForm.rating),
                comment: reviewForm.comment
            });
            setReviewForm({ rating: 5, comment: '' });
            fetchProductDetails(); // Refresh to show the new review
        } catch (err) {
            alert(t(err?.response?.data?.error || 'Failed to post review'));
        } finally {
            setSubmittingReview(false);
        }
    }

    function getImageUrl(url) {
        if (!url) return null;
        if (url.startsWith('http')) return url;
        return `${api.defaults.baseURL}${url}`;
    }

    if (loading) {
        return (
            <div className="product-info-page">
            <header className="product-info-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{t('Product Info')}</h1>
            </header>
                <div className="loading-state">
                    <Skeleton width="100%" height="300px" borderRadius="12px" />
                    <Skeleton width="60%" height="32px" style={{ marginTop: '16px' }} />
                    <Skeleton width="40%" height="24px" style={{ marginTop: '8px' }} />
                </div>
            </div>
        );
    }

    if (error || !productDetails) {
        return (
            <div className="product-info-page">
                <header className="product-info-header">
                    <button className="back-btn-circular" onClick={onBack}>
                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                            <polyline points="15 18 9 12 15 6"></polyline>
                        </svg>
                    </button>
                    <h1>{t('Error')}</h1>
                </header>
                <div className="error-message">{t(error || 'Product not found')}</div>
            </div>
        );
    }

    const { product, totalSales, inStock, averageRating, totalReviews, latestReviews, relatedProducts, dailySales } = productDetails;
    const isOwner = currentUserId === product.storeUserId;
    const availabilityPercentage = product.originalQuantity > 0 
        ? Math.round((inStock / product.originalQuantity) * 100) 
        : 0;

    const isFav = favorites.some(p => p.id === productId);

    return (
        <div className="product-info-page">
            <header className="product-info-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{t('Product Info')}</h1>
                <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <button 
                        className={`fav-btn ${isFav ? 'active' : ''}`}
                        onClick={() => onToggleFavorite && onToggleFavorite(product)}
                        style={{ position: 'relative', top: 0, right: 0 }}
                    >
                        <svg width="22" height="22" viewBox="0 0 24 24" fill={isFav ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2">
                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l8.89-8.89 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                        </svg>
                    </button>
                    <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
                </div>
            </header>

            <div className="product-top-section">
                <div className="product-image-gallery">
                    <div className="main-image">
                        {getImageUrl(activeImageUrl) ? (
                            <img src={getImageUrl(activeImageUrl)} alt={product.name} />
                        ) : (
                            <div className="placeholder-image">{t('No Image')}</div>
                        )}
                        <div className="dots">
                            <span className={`dot ${activeImageUrl === product.imageUrl ? 'active' : ''}`} onClick={() => setActiveImageUrl(product.imageUrl)}></span>
                            {product.additionalImageUrls?.map((url, i) => (
                                <span key={i} className={`dot ${activeImageUrl === url ? 'active' : ''}`} onClick={() => setActiveImageUrl(url)}></span>
                            ))}
                        </div>
                    </div>
                    {product.additionalImageUrls && product.additionalImageUrls.length > 0 && (
                        <div className="thumbnail-list">
                            <div className={`thumb ${activeImageUrl === product.imageUrl ? 'active' : ''}`} onClick={() => setActiveImageUrl(product.imageUrl)}>
                                <img src={getImageUrl(product.imageUrl)} alt="" />
                            </div>
                            {product.additionalImageUrls.map((url, i) => (
                                <div key={i} className={`thumb ${activeImageUrl === url ? 'active' : ''}`} onClick={() => setActiveImageUrl(url)}>
                                    <img src={getImageUrl(url)} alt="" />
                                </div>
                            ))}
                        </div>
                    )}
                </div>

                <div className="product-titles">
                    <h2>{product.name}</h2>
                    <p className="price">{formatPrice(product.price, product.currencyCode)}</p>
                    {product.storeName && (
                        <p 
                            style={{ fontSize: '14px', color: 'var(--muted)', margin: '6px 0', display: 'inline-flex', alignItems: 'center', gap: '6px', cursor: 'pointer' }}
                            onClick={() => { if (onNavigateStore) onNavigateStore(product.storeUserId); }}
                        >
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                                <polyline points="9 22 9 12 15 12 15 22"></polyline>
                            </svg>
                            {t('Sold by')} <strong style={{ color: '#FFB38E', textDecoration: 'underline' }}>{product.storeName}</strong>
                        </p>
                    )}
                    <p className="description">{product.description}</p>
                </div>
            </div>

            <div className="tabs-container">
                <div className="tabs-header">
                    {isOwner && <button className={activeTab === 'statistics' ? 'active' : ''} onClick={() => setActiveTab('statistics')}>{t('Statistics')}</button>}
                    <button className={activeTab === 'reviews' ? 'active' : ''} onClick={() => setActiveTab('reviews')}>{t('Reviews')}</button>
                    <button className={activeTab === 'related' ? 'active' : ''} onClick={() => setActiveTab('related')}>{t('Related Products')}</button>
                </div>

                <div className="tab-content">
                    {activeTab === 'statistics' && isOwner && (
                        <div className="statistics-tab">
                            <div className="stat-card">
                                <h3>{t('Sales')}: {totalSales >= 1000 ? (totalSales/1000).toFixed(1) + 'K' : totalSales} {t('Sold')}</h3>
                                <div className="bar-chart">
                                    {dailySales && dailySales.length > 0 ? dailySales.map((s, i) => {
                                        const count = s.count !== undefined ? s.count : (s.Count !== undefined ? s.Count : 0);
                                        const maxCount = Math.max(...dailySales.map(d => d.count || d.Count || 0), 1);
                                        const height = Math.max((count / maxCount) * 100, 5); 
                                        return (
                                            <div key={i} className="bar">
                                                <div className="fill" style={{ height: `${height}%` }}>
                                                    {count > 0 && <span className="count-hint">{count}</span>}
                                                </div>
                                                <span className="bar-label">{s.day || s.Day}</span>
                                            </div>
                                        );
                                    }) : (
                                        <div className="no-stats-msg">{t('No sales data available yet.')}</div>
                                    )}
                                </div>
                            </div>
                            <div className="stat-card">
                                <h3>{t('Availability')}: <span className={inStock > 0 ? 'status-in-stock' : 'status-out-stock'}>{inStock > 0 ? t('In Stock') : t('Out of Stock')}</span></h3>
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

                    {activeTab === 'reviews' && (
                        <div className="reviews-tab">
                            <div className="reviews-summary">
                                <h3>{averageRating.toFixed(1)} / 5</h3>
                                <div className="stars">
                                    {'★'.repeat(Math.round(averageRating))}{'☆'.repeat(5 - Math.round(averageRating))}
                                </div>
                                <span className="count">({totalReviews} {t('Reviews')})</span>
                            </div>

                            <form onSubmit={handleReviewSubmit} className="add-review-form">
                                <h4>{t('Add a Review')}</h4>
                                <div className="star-rating-input">
                                    {[1, 2, 3, 4, 5].map(s => (
                                        <span key={s} 
                                            className={`star ${reviewForm.rating >= s ? 'filled' : ''}`}
                                            onClick={() => setReviewForm({ ...reviewForm, rating: s })}>
                                            ★
                                        </span>
                                    ))}
                                </div>
                                <textarea 
                                    placeholder={t('Write your comment here...')}
                                    className="review-textarea"
                                    value={reviewForm.comment}
                                    onChange={e => setReviewForm({ ...reviewForm, comment: e.target.value })}
                                    required
                                />
                                <button type="submit" className="submit-review-btn" disabled={submittingReview}>
                                    {submittingReview ? t('Posting...') : t('Submit Review')}
                                </button>
                            </form>

                            <ul className="review-list">
                                {latestReviews.length > 0 ? latestReviews.map(r => (
                                    <li key={r.id} className="review-item">
                                        <div className="review-header">
                                            <strong>{r.userName}</strong>
                                            <div className="stars">{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</div>
                                        </div>
                                        <p className="comment">"{r.comment}"</p>
                                    </li>
                                )) : <li className="no-reviews">{t('No reviews yet.')}</li>}
                            </ul>
                        </div>
                    )}

                    {activeTab === 'related' && (
                        <div className="related-tab">
                            <div className="related-products-grid">
                                {relatedProducts.length > 0 ? relatedProducts.map(rp => (
                                    <div key={rp.id} className="related-card" onClick={() => onSelectRelatedProduct && onSelectRelatedProduct(rp)}>
                                        <div className="related-img">
                                            {getImageUrl(rp.imageUrl) ? <img src={getImageUrl(rp.imageUrl)} alt={rp.name} /> : t('No Image')}
                                        </div>
                                        <p>{rp.name}</p>
                                    </div>
                                )) : <p>{t('No related products found.')}</p>}
                            </div>
                        </div>
                    )}
                </div>
            </div>
            
            <div className="bottom-action">
                <button
                    className="primary-btn add-to-cart-large"
                    disabled={inStock <= 0}
                    style={inStock <= 0 ? { backgroundColor: 'gray', cursor: 'not-allowed', opacity: 0.7 } : {}}
                    onClick={() => {
                        if (inStock > 0) {
                            onAddToCart(productId);
                        } else {
                            alert(t('This product is out of stock.'));
                        }
                    }}
                >
                    {inStock > 0 ? t('Add to Cart') : t('Out of Stock')}
                </button>
            </div>
        </div>
    );
};

export default ProductInfoPage;
