import React from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient.js';
import { formatPrice } from '../utils/formatPrice.js';
import CartIconButton from '../components/CartIconButton';
import '../pages/CategoryProductsPage.css'; // Reusing the same grid styles or we'll refine them

const FavoritesPage = ({ favorites, onBack, onSelectProduct, onToggleFavorite, onNavigateCart, cartCount }) => {
    const { t } = useTranslation();
    
    function imageUrl(p) {
        if (!p.imageUrl) return null;
        if (p.imageUrl.startsWith('http')) return p.imageUrl;
        return `${api.defaults.baseURL}${p.imageUrl}`;
    }

    return (
        <div className="category-products-container">
            <header className="category-page-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{t('Favorites')}</h1>
                <CartIconButton cartCount={cartCount} onClick={onNavigateCart} />
            </header>

            <main className="products-grid">
                {favorites.length > 0 ? (
                    favorites.map(product => (
                        <div key={product.id} className="product-card-web" onClick={() => onSelectProduct && onSelectProduct(product)}>
                            <div className="product-img-wrapper">
                                <button 
                                    className="fav-btn active" 
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        onToggleFavorite(product);
                                    }}
                                >
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="#E06C38" stroke="#E06C38" strokeWidth="2">
                                        <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l8.89-8.89 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
                                    </svg>
                                </button>
                                <span className="product-price-tag">{formatPrice(product.price, product.currencyCode)}</span>
                                {imageUrl(product) ? (
                                    <img src={imageUrl(product)} alt={product.name} />
                                ) : (
                                    <div className="product-img-placeholder">
                                        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3">
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
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        // Need to pass onAddToCart if we want it here
                                    }}
                                >
                                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                        <circle cx="9" cy="21" r="1"></circle>
                                        <circle cx="20" cy="21" r="1"></circle>
                                        <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
                                    </svg>
                                    {t('Add to Cart')}
                                </button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-favorites">
                        <p>{t("You haven't added any favorites yet.")}</p>
                        <button className="primary" onClick={onBack} style={{ marginTop: '20px' }}>{t('Start Shopping')}</button>
                    </div>
                )}
            </main>
        </div>
    );
};

export default FavoritesPage;
