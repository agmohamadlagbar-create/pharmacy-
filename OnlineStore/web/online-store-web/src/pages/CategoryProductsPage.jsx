import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient.js';
import { formatPrice } from '../utils/formatPrice.js';
import Skeleton from '../components/Skeleton.jsx';
import CartIconButton from '../components/CartIconButton';
import useInfiniteScroll from '../hooks/useInfiniteScroll.js';
import './CategoryProductsPage.css';

const CategoryProductsPage = ({ 
    categoryId, 
    categoryName, 
    onBack, 
    onSelectProduct, 
    onNavigateCart, 
    cartCount, 
    onAddToCart, 
    onNavigateStore,
    favorites = [],
    onToggleFavorite,
    userLocation = { country: null, city: null }
}) => {
    const { t } = useTranslation();
    const [subcategories, setSubcategories] = useState([]);
    const [loadingSubcategories, setLoadingSubcategories] = useState(true);
    const [selectedSubcategoryId, setSelectedSubcategoryId] = useState(null);

    const targetCatId = selectedSubcategoryId || categoryId;

    const fetchProducts = async (page, limit) => {
        let url = `/api/products/approved?categoryId=${targetCatId}&page=${page}&limit=${limit}&_=${Date.now()}`;
        if (userLocation.country) url += `&country=${encodeURIComponent(userLocation.country)}`;
        if (userLocation.city) url += `&city=${encodeURIComponent(userLocation.city)}`;
        
        const { data } = await api.get(url);
        return data;
    };

    const {
        items: products,
        isLoading,
        hasMore,
        error,
        sentinelRef,
        retry,
        initialLoad
    } = useInfiniteScroll(fetchProducts, {
        limit: 9,
        deps: [categoryId, selectedSubcategoryId]
    });

    useEffect(() => {
        fetchSubcategories();
    }, [categoryId]);

    async function fetchSubcategories() {
        try {
            const { data } = await api.get(`/api/categories/${categoryId}/subcategories?_=${Date.now()}`);
            setSubcategories(Array.isArray(data) ? data : []);
        } catch (err) {
            console.error('Failed to fetch subcategories', err);
        } finally {
            setLoadingSubcategories(false);
        }
    }

    function imageUrl(p) {
        if (!p.imageUrl) return null;
        if (p.imageUrl.startsWith('http')) return p.imageUrl;
        return `${api.defaults.baseURL}${p.imageUrl}`;
    }

    const isFavorited = (productId) => favorites.some(p => p.id === productId);

    // Skeleton card that matches exact product card layout
    const SkeletonCard = () => (
        <div className="product-card-web skeleton-card">
            <Skeleton width="100%" height="240px" borderRadius="24px 24px 0 0" />
            <div style={{ padding: '24px' }}>
                <Skeleton width="80px" height="20px" borderRadius="8px" style={{ marginBottom: '12px' }} />
                <Skeleton width="70%" height="22px" style={{ marginBottom: '6px' }} />
                <Skeleton width="90%" height="14px" style={{ marginBottom: '16px' }} />
                <div style={{ display: 'flex', gap: '4px', marginBottom: '16px' }}>
                    <Skeleton width="80px" height="14px" />
                    <Skeleton width="50px" height="14px" />
                </div>
                <Skeleton width="100%" height="44px" borderRadius="12px" />
            </div>
        </div>
    );

    return (
        <div className="category-products-container">
            <header className="category-page-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{categoryName || t('Products')}</h1>
                <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
            </header>

            {loadingSubcategories ? (
                <div className="subcategories-container">
                    {Array.from({ length: 4 }).map((_, i) => (
                        <Skeleton key={i} width="80px" height="36px" borderRadius="18px" />
                    ))}
                </div>
            ) : subcategories.length > 0 && (
                <div className="subcategories-container">
                    <button
                        className={`chip ${selectedSubcategoryId === null ? 'active' : ''}`}
                        onClick={() => setSelectedSubcategoryId(null)}
                    >
                        {t('All')}
                    </button>
                    {subcategories.map(sub => (
                        <button
                            key={sub.id}
                            className={`chip ${selectedSubcategoryId === sub.id ? 'active' : ''}`}
                            onClick={() => setSelectedSubcategoryId(sub.id)}
                        >
                            {sub.name}
                        </button>
                    ))}
                </div>
            )}


            <main className="products-grid">
                {initialLoad ? (
                    Array.from({ length: 9 }).map((_, i) => (
                        <SkeletonCard key={`skeleton-init-${i}`} />
                    ))
                ) : error && products.length === 0 ? (
                    <div className="no-products">
                        <p>{t(error)}</p>
                        <button className="primary" onClick={retry} style={{ marginTop: '12px' }}>{t('Retry')}</button>
                    </div>
                ) : products.length > 0 ? (
                    <>
                        {products.map(product => (
                            <div key={product.id} className="product-card-web" onClick={() => onSelectProduct && onSelectProduct(product)}>
                                <div className="product-img-wrapper">
                                    <span className="product-price-tag">{formatPrice(product.price, product.currencyCode)}</span>
                                    <button 
                                        className={`fav-btn ${isFavorited(product.id) ? 'active' : ''}`}
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            onToggleFavorite && onToggleFavorite(product);
                                        }}
                                    >
                                        <svg width="20" height="20" viewBox="0 0 24 24" fill={isFavorited(product.id) ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2">
                                            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l8.89-8.89 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                                        </svg>
                                    </button>
                                    {imageUrl(product) ? (
                                        <img src={imageUrl(product)} alt={product.name} />
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
                                    {product.storeName && (
                                        <div 
                                            className="store-badge"
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                if (onNavigateStore) onNavigateStore(product.storeUserId);
                                            }}
                                        >
                                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                                            </svg>
                                            {product.storeName}
                                        </div>
                                    )}
                                    <h3>{product.name}</h3>
                                    {product.description && <p>{product.description}</p>}
                                    
                                    <div className="rating-row">
                                        <span className="star">
                                            {'★'.repeat(Math.round(product.averageRating || 0))}
                                            {'☆'.repeat(5 - Math.round(product.averageRating || 0))}
                                        </span>
                                        <span className="rating-text">
                                            {(product.averageRating || 0).toFixed(1)} ({product.reviewCount || 0})
                                        </span>
                                    </div>

                                    <button
                                        className="add-to-cart-btn-outline"
                                        disabled={product.stockQuantity <= 0}
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            if (product.stockQuantity > 0) {
                                                onAddToCart(product.id);
                                            } else {
                                                alert(t('This product is out of stock.'));
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

                        {/* Loading more skeleton cards */}
                        {isLoading && !initialLoad && (
                            Array.from({ length: 3 }).map((_, i) => (
                                <SkeletonCard key={`skeleton-more-${i}`} />
                            ))
                        )}
                    </>
                ) : (
                    <div className="no-products">
                        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3" style={{ marginBottom: '12px' }}>
                            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                        </svg>
                        {userLocation.city ? (
                            <p>{t('No delivery in your city ({{city}}) for this category.', { city: userLocation.city })}</p>
                        ) : (
                            <p>{t('No products in {{category}} yet.', { category: categoryName })}</p>
                        )}
                    </div>
                )}
            </main>

            {/* Sentinel element for IntersectionObserver */}
            {hasMore && !initialLoad && (
                <div ref={sentinelRef} className="infinite-scroll-sentinel" />
            )}

            {/* End of list message */}
            {!hasMore && products.length > 0 && (
                <div className="infinite-scroll-end">
                    <div className="end-divider" />
                    <span>{t('No more products')}</span>
                    <div className="end-divider" />
                </div>
            )}

            {/* Error retry on subsequent loads */}
            {error && products.length > 0 && (
                <div className="infinite-scroll-error">
                    <p>{t('Failed to load more products.')}</p>
                    <button className="primary" onClick={retry}>{t('Retry')}</button>
                </div>
            )}
        </div>
    );
};

export default CategoryProductsPage;
