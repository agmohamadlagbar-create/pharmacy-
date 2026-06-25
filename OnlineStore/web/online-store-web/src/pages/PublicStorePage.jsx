import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import './PublicStorePage.css'

const MapPinIcon = () => (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="#FFB38E" stroke="none">
        <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
        <circle cx="12" cy="10" r="3" fill="#1A1A1A"></circle>
    </svg>
)

const BackIcon = () => (
    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <line x1="19" y1="12" x2="5" y2="12"></line>
        <polyline points="12 19 5 12 12 5"></polyline>
    </svg>
)

// Helper to check if currently open
function isStoreOpen(openTimeStr, closeTimeStr) {
    if (!openTimeStr || !closeTimeStr) return true; 
    try {
        const toMinutes = (t) => {
            const parts = t.trim().split(':');
            if (parts.length < 2) return 0;
            return parseInt(parts[0] || '0', 10) * 60 + parseInt((parts[1] || '0').substring(0, 2), 10);
        };
        const openMins = toMinutes(openTimeStr);
        const closeMins = toMinutes(closeTimeStr);
        const now = new Date();
        const currentMins = now.getHours() * 60 + now.getMinutes();

        if (openMins <= closeMins) {
            return currentMins >= openMins && currentMins <= closeMins;
        } else {
            return currentMins >= openMins || currentMins <= closeMins;
        }
    } catch {
        return true;
    }
}

export default function PublicStorePage({ storeUserId, onBack, onSelectProduct, userLocation = { country: null, city: null } }) {
    const { t } = useTranslation()
    const [store, setStore] = useState(null)
    const [products, setProducts] = useState([])
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        loadData()
    }, [storeUserId])

    async function loadData() {
        setLoading(true)
        try {
            const storeRes = await api.get(`/api/shops/${storeUserId}`)
            setStore(storeRes.data)

            let prodUrl = `/api/products/approved?storeUserId=${storeUserId}&limit=100`
            if (userLocation.country) prodUrl += `&country=${encodeURIComponent(userLocation.country)}`
            if (userLocation.city) prodUrl += `&city=${encodeURIComponent(userLocation.city)}`
            
            const prodRes = await api.get(prodUrl)
            const prodData = prodRes.data?.data || prodRes.data
            setProducts(Array.isArray(prodData) ? prodData : [])
        } catch (err) {
            console.error('Failed to load store data', err)
            alert(t('Failed to load store'))
            onBack()
        } finally {
            setLoading(false)
        }
    }

    if (loading) return <div className="profile-loading">{t('Loading store profile...')}</div>
    if (!store) return <div className="profile-loading">{t('Store not found')}</div>

    const avatarUrl = store.profilePictureUrl ? `${api.defaults.baseURL}${store.profilePictureUrl}` : null
    const open = isStoreOpen(store.openingTime, store.closingTime)
    const storeInitial = store.shopName ? store.shopName.charAt(0).toUpperCase() : 'S'

    return (
        <div className="store-page-container">
            {/* Banner Section */}
            <div className="store-banner">
                <div className="banner-nav">
                    <button className="icon-btn" onClick={onBack} title="Go Back">
                        <BackIcon />
                    </button>
                </div>
                <div className={`status-badge ${open ? 'open' : 'closed'}`}>
                    • {open ? t('OPEN') : t('CLOSED')}
                </div>
            </div>

            {/* Header Content (Avatar + Info) */}
            <div className="store-header-content">
                <div className="store-avatar-wrapper">
                    <div className="store-avatar-inner">
                        {avatarUrl ? (
                            <img src={avatarUrl} alt={store.shopName} className="store-avatar-img" />
                        ) : (
                            <span>{storeInitial}</span>
                        )}
                    </div>
                </div>

                <h1 className="store-name">{store.shopName}</h1>
                
                {(store.openingTime && store.closingTime) && (
                    <p className="store-hours">
                        {t('Opens')} {store.openingTime} · {t('Closes')} {store.closingTime}
                    </p>
                )}

                <div className="store-location-box">
                    <div className="location-label">
                        <MapPinIcon />
                        {t('LOCATION')}
                    </div>
                    <div className="location-value">{store.location || 'Türkiye'}</div>
                </div>

                {store.description && (
                    <p className="store-bio">{store.description}</p>
                )}

                <div className="store-divider"></div>
            </div>

            {/* Products Section */}
            <div className="products-section">
                <h2 className="section-title-minimal">{t('PRODUCTS')}</h2>
                
                {products.length === 0 ? (
                    <div style={{ textAlign: 'center', color: 'rgba(255,255,255,0.4)', padding: '60px 0' }}>
                        {userLocation.city ? (
                            t('This store does not deliver to {{city}}.', { city: userLocation.city })
                        ) : (
                            t('This store has no products yet.')
                        )}
                    </div>
                ) : (
                    <div className="products-minimal-grid">
                        {products.map(product => (
                            <div 
                                key={product.id} 
                                className="product-card-minimal" 
                                onClick={() => onSelectProduct(product)}
                            >
                                <h3 className="p-name">{product.name}</h3>
                                {product.description && <p className="p-desc">{product.description}</p>}
                                <div className="p-price">
                                    {formatPrice(product.price, product.currencyCode)}
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    )
}

