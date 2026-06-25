import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient.js';
import { formatPrice } from '../utils/formatPrice.js';
import Skeleton from '../components/Skeleton.jsx';
import CartIconButton from '../components/CartIconButton';
import './CategoryProductsPage.css';

const SearchProductsPage = ({
    initialQuery,
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
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Filters
    const [searchQuery, setSearchQuery] = useState(initialQuery || '');
    const [minPrice, setMinPrice] = useState('');
    const [maxPrice, setMaxPrice] = useState('');
    const [categoryId, setCategoryId] = useState('');

    const [categories, setCategories] = useState([]);

    useEffect(() => {
        fetchCategories();
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => {
            fetchProducts();
        }, 400);
        return () => clearTimeout(timer);
    }, [searchQuery, minPrice, maxPrice, categoryId]);

    async function fetchCategories() {
        try {
            const { data } = await api.get('/api/categories');
            setCategories(Array.isArray(data) ? data : []);
        } catch (err) {
            console.error('Failed to fetch categories', err);
        }
    }

    async function fetchProducts(e) {
        if (e) e.preventDefault();
        setLoading(true);
        setError(null);
        try {
            let url = `/api/products/approved?limit=100&_=${Date.now()}`;
            if (searchQuery) url += `&search=${encodeURIComponent(searchQuery)}`;
            if (minPrice) url += `&minPrice=${minPrice}`;
            if (maxPrice) url += `&maxPrice=${maxPrice}`;
            if (categoryId) url += `&categoryId=${categoryId}`;
            if (userLocation.country) url += `&country=${encodeURIComponent(userLocation.country)}`;
            if (userLocation.city) url += `&city=${encodeURIComponent(userLocation.city)}`;

            const { data } = await api.get(url);
            const items = data?.data || data;
            setProducts(Array.isArray(items) ? items : []);
        } catch (err) {
            console.error('Failed to fetch products', err);
            setError(t('Failed to load products. Please try again.'));
        }
        setLoading(false);
    }

    function imageUrl(p) {
        if (!p.imageUrl) return null;
        if (p.imageUrl.startsWith('http')) return p.imageUrl;
        return `${api.defaults.baseURL}${p.imageUrl}`;
    }

    const isFavorited = (productId) => favorites.some(p => p.id === productId);

    return (
        <div className="category-products-container" style={{ paddingTop: '80px' }}>
            <header className="category-page-header" style={{ position: 'fixed', top: 0, left: 0, right: 0, zIndex: 100, background: 'var(--bg)', borderBottom: '1px solid var(--border)', padding: '12px 20px', display: 'flex', alignItems: 'center', gap: '16px' }}>
                <button onClick={onBack} style={{ width: '40px', height: '40px', borderRadius: '50%', background: 'var(--card)', border: '1px solid var(--border)', display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer', flexShrink: 0, padding: 0 }}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--primary)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <form onSubmit={fetchProducts} style={{ flex: 1, position: 'relative' }}>
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--muted)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ position: 'absolute', left: '16px', top: '50%', transform: 'translateY(-50%)' }}>
                        <circle cx="11" cy="11" r="8"></circle>
                        <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                    </svg>
                    <input
                        type="search"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        placeholder={t('Search products...')}
                        autoFocus
                        style={{
                            width: '100%',
                            padding: '10px 16px 10px 42px',
                            borderRadius: '8px',
                            border: '1px solid var(--border)',
                            background: 'var(--card)',
                            color: 'var(--text)',
                            outline: 'none',
                            fontSize: '1rem'
                        }}
                    />
                </form>
                <div style={{ flexShrink: 0 }}>
                    <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
                </div>
            </header>

            <div style={{ padding: '0 20px', marginBottom: '20px', display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
                {/* Filters Label Box */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '10px 16px', borderRadius: '8px', background: 'transparent', border: '1px solid rgba(255, 120, 0, 0.3)' }}>
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--primary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <line x1="4" y1="6" x2="20" y2="6"></line>
                        <line x1="6" y1="12" x2="18" y2="12"></line>
                        <line x1="8" y1="18" x2="16" y2="18"></line>
                    </svg>
                    <span style={{ fontWeight: '600', color: 'var(--primary)', fontSize: '0.95rem' }}>{t('Filters')}</span>
                </div>

                {/* Category Select */}
                <select
                    value={categoryId}
                    onChange={(e) => setCategoryId(e.target.value)}
                    style={{
                        padding: '10px 16px', borderRadius: '8px', border: '1px solid var(--border)',
                        background: 'var(--card)', color: 'var(--text)', outline: 'none', cursor: 'pointer',
                        fontSize: '0.95rem', minWidth: '160px'
                    }}
                >
                    <option value="" style={{ background: 'var(--bg)', color: 'var(--text)' }}>{t('All Categories')}</option>
                    {categories.map(c => (
                        <option key={c.id} value={c.id} style={{ background: 'var(--bg)', color: 'var(--text)' }}>{c.name}</option>
                    ))}
                </select>

                {/* Min / Max Box */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <input
                        type="number"
                        placeholder={t('Min $')}
                        value={minPrice}
                        onChange={e => setMinPrice(e.target.value)}
                        style={{ width: '90px', padding: '10px 12px', borderRadius: '8px', border: '1px solid var(--border)', background: 'var(--card)', color: 'var(--text)', outline: 'none', fontSize: '0.95rem' }}
                    />
                    <span style={{ color: 'var(--muted)' }}>—</span>
                    <input
                        type="number"
                        placeholder={t('Max $')}
                        value={maxPrice}
                        onChange={e => setMaxPrice(e.target.value)}
                        style={{ width: '90px', padding: '10px 12px', borderRadius: '8px', border: '1px solid var(--border)', background: 'var(--card)', color: 'var(--text)', outline: 'none', fontSize: '0.95rem' }}
                    />
                </div>

                {(minPrice || maxPrice || categoryId) && (
                    <button
                        onClick={() => {
                            setMinPrice('');
                            setMaxPrice('');
                            setCategoryId('');
                        }}
                        style={{
                            padding: '10px 16px', borderRadius: '8px', border: '1px solid var(--primary)',
                            background: 'transparent', color: 'var(--primary)', cursor: 'pointer', fontWeight: '500', fontSize: '0.9rem',
                            transition: 'all 0.2s ease', marginLeft: 'auto'
                        }}
                        onMouseOver={(e) => { e.currentTarget.style.background = 'var(--primary)'; e.currentTarget.style.color = 'var(--bg)'; }}
                        onMouseOut={(e) => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--primary)'; }}
                    >
                        {t('Reset Filters')}
                    </button>
                )}
            </div>

            <main className="products-grid">
                {loading ? (
                    Array.from({ length: 4 }).map((_, i) => (
                        <div key={i} className="product-card-web skeleton-card">
                            <Skeleton width="100%" height="240px" borderRadius="24px 24px 0 0" />
                            <div style={{ padding: '24px' }}>
                                <Skeleton width="60%" height="24px" style={{ marginBottom: '12px' }} />
                                <Skeleton width="40%" height="16px" style={{ marginBottom: '16px' }} />
                                <Skeleton width="100%" height="44px" borderRadius="12px" />
                            </div>
                        </div>
                    ))
                ) : error ? (
                    <div className="no-products">
                        <p>{error}</p>
                        <button className="primary" onClick={fetchProducts} style={{ marginTop: '12px' }}>{t('Retry')}</button>
                    </div>
                ) : products.length > 0 ? (
                    products.map(product => (
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
                    ))
                ) : (
                    <div className="no-products">
                        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3" style={{ marginBottom: '12px' }}>
                            <circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                        </svg>
                        {userLocation.city ? (
                            <p>{t('No products found in your city ({{city}}) matching your search.', { city: userLocation.city })}</p>
                        ) : (
                            <p>{t('No products found matching your search.')}</p>
                        )}
                    </div>
                )}
            </main>
        </div>
    );
};

export default SearchProductsPage;
