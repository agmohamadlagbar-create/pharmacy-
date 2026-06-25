import { useState, useCallback } from 'react';
import { formatPrice as formatPriceUtil } from '../utils/formatPrice.js';
import './RecommendationCards.css';

/**
 * Section configuration — maps backend keys to customer-friendly presentation.
 * Order determines display priority.
 */
const SECTION_CONFIG = {
  BasedOnHistory: {
    icon: '✨',
    title: 'Recommended For You',
    explanation: 'Based on your shopping history, you might love these.',
  },
  FrequentlyBoughtTogether: {
    icon: '🛒',
    title: 'Customers Also Bought',
    explanation: 'Popular items often purchased together.',
    showTrendingBadge: true,
  },
  RelatedByCategory: {
    icon: '💡',
    title: 'You May Also Like',
    explanation: 'Similar products in this category.',
  },
  SmartUpsells: {
    icon: '🔥',
    title: 'Complete Your Setup',
    explanation: 'Premium alternatives to level up your experience.',
  },
};

/**
 * Validates a product for display. Filters out bad data.
 */
function isValidProduct(product) {
  if (!product) return false;
  if (!product.ProductName && !product.productName) return false;
  const name = product.ProductName || product.productName || '';
  // Filter numeric-only names
  if (/^\d+$/.test(name.trim())) return false;
  // Filter very short/test names
  if (name.trim().length < 2) return false;
  return true;
}

/**
 * Deduplicates products across all sections by ProductId.
 */
function deduplicateProducts(products, seenIds) {
  return products.filter((p) => {
    const id = p.ProductId || p.productId;
    if (seenIds.has(id)) return false;
    seenIds.add(id);
    return true;
  });
}

/**
 * Formats price with currency-specific formatting.
 */
function formatPrice(price, currencyCode) {
  return formatPriceUtil(price, currencyCode);
}

/**
 * Returns stock status info for badges.
 */
function getStockStatus(stock) {
  if (stock <= 0) return { label: 'Out of Stock', className: 'out-of-stock', urgency: true };
  if (stock <= 3) return { label: `Only ${stock} left!`, className: 'low-stock', urgency: true };
  if (stock <= 10) return { label: 'Low Stock', className: 'low-stock', urgency: false };
  return { label: 'In Stock', className: 'in-stock', urgency: false };
}

// ── Product Card ──────────────────────────────────────────────
function ProductCard({ product, onAddToCart, onNavigateProduct, showTrendingBadge }) {
  const [addedState, setAddedState] = useState(false);

  const name = product.ProductName || product.productName || 'Product';
  const price = product.Price ?? product.price ?? 0;
  const stock = product.Stock ?? product.stock ?? 0;
  const imageUrl = product.ImageUrl || product.imageUrl;
  const productId = product.ProductId || product.productId;
  const categoryName = product.CategoryName || product.categoryName || '';
  const storeName = product.StoreName || product.storeName || '';
  const stockStatus = getStockStatus(stock);

  const handleAddToCart = useCallback((e) => {
    e.stopPropagation();
    if (stock <= 0 || addedState) return;
    if (onAddToCart) onAddToCart(productId, 1);
    setAddedState(true);
    setTimeout(() => setAddedState(false), 2000);
  }, [productId, stock, addedState, onAddToCart]);

  const handleQuickView = useCallback((e) => {
    e.stopPropagation();
    if (onNavigateProduct) onNavigateProduct(productId);
  }, [productId, onNavigateProduct]);

  const handleCardClick = useCallback(() => {
    if (onNavigateProduct) onNavigateProduct(productId);
  }, [productId, onNavigateProduct]);

  return (
    <div className="rec-card" onClick={handleCardClick}>
      {/* Image */}
      <div className="rec-card-img">
        {imageUrl ? (
          <img
            src={imageUrl}
            alt={name}
            loading="lazy"
            onError={(e) => { e.target.style.display = 'none'; e.target.nextSibling.style.display = 'flex'; }}
          />
        ) : null}
        <div
          className="rec-card-img-placeholder"
          style={imageUrl ? { display: 'none' } : undefined}
        >
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
            <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
            <circle cx="8.5" cy="8.5" r="1.5" />
            <polyline points="21 15 16 10 5 21" />
          </svg>
        </div>

        {/* Badges */}
        <div className="rec-badge-row">
          <div>
            {showTrendingBadge && <span className="rec-badge rec-badge-trending">🔥 Popular</span>}
            {stockStatus.urgency && stock > 0 && (
              <span className="rec-badge rec-badge-urgency">{stockStatus.label}</span>
            )}
            {!stockStatus.urgency && stock <= 10 && stock > 3 && (
              <span className="rec-badge rec-badge-lowstock">{stockStatus.label}</span>
            )}
          </div>
        </div>

        {/* Wishlist */}
        <button
          className="rec-wishlist-btn"
          onClick={(e) => e.stopPropagation()}
          title="Save for later"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" />
          </svg>
        </button>
      </div>

      {/* Body */}
      <div className="rec-card-body">
        {storeName && (
          <div className="rec-card-store">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
            </svg>
            {storeName}
          </div>
        )}
        <p className="rec-card-name">{name}</p>
        {categoryName && <p className="rec-card-category">{categoryName}</p>}
        <div className="rec-card-price-row">
          <span className="rec-card-price">{formatPrice(price, product.CurrencyCode || product.currencyCode)}</span>
          <span className={`rec-card-stock ${stockStatus.className}`}>{stockStatus.label}</span>
        </div>

        {/* Actions */}
        <div className="rec-card-actions">
          <button
            className={`rec-add-btn ${addedState ? 'added' : ''}`}
            onClick={handleAddToCart}
            disabled={stock <= 0}
          >
            {addedState ? (
              'Added'
            ) : (
              <>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="9" cy="21" r="1" />
                  <circle cx="20" cy="21" r="1" />
                  <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6" />
                </svg>
                Add
              </>
            )}
          </button>
          <button className="rec-quick-view-btn" onClick={handleQuickView} title="Quick View">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
              <circle cx="12" cy="12" r="3" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Skeleton Cards ────────────────────────────────────────────
function SkeletonCards() {
  return (
    <div className="rec-carousel">
      {[1, 2, 3].map((i) => (
        <div key={i} className="rec-skeleton-card">
          <div className="rec-skeleton-img" />
          <div className="rec-skeleton-body">
            <div className="rec-skeleton-line short" />
            <div className="rec-skeleton-line medium" />
            <div className="rec-skeleton-line short" />
            <div className="rec-skeleton-line tall" />
            <div className="rec-skeleton-line btn" />
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Recommendation Section ────────────────────────────────────
function RecommendationSection({ sectionKey, products, config, onAddToCart, onNavigateProduct }) {
  if (!products || products.length === 0) return null;

  return (
    <div className="rec-section">
      <div className="rec-section-header">
        <span className="rec-section-icon">{config.icon}</span>
        <h4 className="rec-section-title">{config.title}</h4>
      </div>
      <p className="rec-ai-explanation">{config.explanation}</p>
      <div className="rec-carousel">
        {products.map((product) => {
          const pid = product.ProductId || product.productId;
          return (
            <ProductCard
              key={pid}
              product={product}
              onAddToCart={onAddToCart}
              onNavigateProduct={onNavigateProduct}
              showTrendingBadge={!!config.showTrendingBadge}
            />
          );
        })}
      </div>
    </div>
  );
}

// ── Main Export ────────────────────────────────────────────────
export default function RecommendationCards({ data, onAddToCart, onNavigateProduct, isLoading }) {
  if (isLoading) return <SkeletonCards />;
  if (!data) return null;

  // Normalize keys: backend might use PascalCase or camelCase
  const normalized = {
    BasedOnHistory: data.BasedOnHistory || data.basedOnHistory || [],
    FrequentlyBoughtTogether: data.FrequentlyBoughtTogether || data.frequentlyBoughtTogether || [],
    RelatedByCategory: data.RelatedByCategory || data.relatedByCategory || [],
    SmartUpsells: data.SmartUpsells || data.smartUpsells || [],
  };

  // Validate and deduplicate across all sections
  const seenIds = new Set();
  const sections = [];

  for (const [key, config] of Object.entries(SECTION_CONFIG)) {
    const rawProducts = normalized[key] || [];
    const valid = rawProducts.filter(isValidProduct);
    const unique = deduplicateProducts(valid, seenIds);
    if (unique.length > 0) {
      sections.push({ key, config, products: unique });
    }
  }

  if (sections.length === 0) return null;

  return (
    <div className="rec-container" onClick={(e) => e.stopPropagation()}>
      {sections.map((section, idx) => (
        <div key={section.key}>
          {idx > 0 && <div className="rec-divider" />}
          <RecommendationSection
            sectionKey={section.key}
            products={section.products}
            config={section.config}
            onAddToCart={onAddToCart}
            onNavigateProduct={onNavigateProduct}
          />
        </div>
      ))}
    </div>
  );
}
