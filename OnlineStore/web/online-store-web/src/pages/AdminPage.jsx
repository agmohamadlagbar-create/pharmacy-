import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import Skeleton from '../components/Skeleton.jsx'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import AiDashboardTab from '../components/AiDashboardTab.jsx'

export default function AdminPage({ onBack }) {
    const { t } = useTranslation()
    const [tab, setTab] = useState('products')
    const [aiSubTab, setAiSubTab] = useState('dashboard')
    const [categories, setCategories] = useState([])
    const [subcategories, setSubcategories] = useState({})
    const [ads, setAds] = useState([])
    const [loadingOther, setLoadingOther] = useState(false)
    const [statusFilter, setStatusFilter] = useState('Pending')

    // Create Category Form State
    const [newCatName, setNewCatName] = useState('')
    const [newCatCode, setNewCatCode] = useState('')
    const [newCatImageFile, setNewCatImageFile] = useState(null)
    const [newCatImagePreview, setNewCatImagePreview] = useState(null)
    const [uploading, setUploading] = useState(false)
    const [newCatParentId, setNewCatParentId] = useState('')

    // Ad Banner Form State
    const [adImageFile, setAdImageFile] = useState(null)
    const [adImagePreview, setAdImagePreview] = useState(null)
    const [adTitle, setAdTitle] = useState('')
    const [adLinkUrl, setAdLinkUrl] = useState('')
    const [adSortOrder, setAdSortOrder] = useState(0)
    const [adUploading, setAdUploading] = useState(false)
    const [refreshKey, setRefreshKey] = useState(0)

    // Product Form State
    const [showProductForm, setShowProductForm] = useState(false)
    const [editingProduct, setEditingProduct] = useState(null)
    const [productForm, setProductForm] = useState({ name: '', description: '', price: '', images: [], categoryId: '', storeName: '', stockQuantity: '0' })
    const [productImageFile, setProductImageFile] = useState(null)
    const [productImagePreview, setProductImagePreview] = useState(null)
    const [productUploading, setProductUploading] = useState(false)

    // Store autocomplete state
    const [storeSuggestions, setStoreSuggestions] = useState([])
    const [showStoreSuggestions, setShowStoreSuggestions] = useState(false)
    const [storeSearchLoading, setStoreSearchLoading] = useState(false)

    // ── Infinite scroll for Products tab ──
    const fetchAdminProducts = async (page, limit) => {
        const { data } = await api.get(`/api/admin/products?status=${statusFilter}&page=${page}&limit=${limit}&_=${Date.now()}`)
        return data
    }
    const {
        items: products,
        isLoading: productsLoading,
        hasMore: productsHasMore,
        error: productsError,
        sentinelRef: productsSentinelRef,
        retry: productsRetry,
        reset: productsReset,
        initialLoad: productsInitialLoad
    } = useInfiniteScroll(fetchAdminProducts, { limit: 9, deps: [statusFilter], enabled: tab === 'products' })

    // ── Infinite scroll for Orders tab ──
    const fetchAdminOrders = async (page, limit) => {
        const { data } = await api.get(`/api/admin/orders?page=${page}&limit=${limit}&_=${Date.now()}`)
        return data
    }
    const {
        items: orders,
        isLoading: ordersLoading,
        hasMore: ordersHasMore,
        sentinelRef: ordersSentinelRef,
        initialLoad: ordersInitialLoad,
        reset: ordersReset
    } = useInfiniteScroll(fetchAdminOrders, { limit: 9, deps: [], enabled: tab === 'orders' })

    // ── Infinite scroll for Users tab ──
    const fetchAdminUsers = async (page, limit) => {
        const { data } = await api.get(`/api/admin/users?page=${page}&limit=${limit}&_=${Date.now()}`)
        return data
    }
    const {
        items: users,
        isLoading: usersLoading,
        hasMore: usersHasMore,
        sentinelRef: usersSentinelRef,
        initialLoad: usersInitialLoad,
        reset: usersReset
    } = useInfiniteScroll(fetchAdminUsers, { limit: 9, deps: [], enabled: tab === 'users' })

    // ── Infinite scroll for Categories tab ──
    const fetchAdminCategories = async (page, limit) => {
        const { data } = await api.get(`/api/categories?page=${page}&limit=${limit}&_=${Date.now()}`)
        // Also fetch subcategories for each category
        const catData = Array.isArray(data) ? data : (data.data || data.items || [])
        // Re-construct the response to fit what useInfiniteScroll expects
        const response = {
            data: catData,
            page: data.page || page,
            limit: data.limit || limit,
            total: data.total || catData.length,
            hasMore: data.hasMore ?? (catData.length === limit)
        }
        const subs = { ...subcategories }
        for (const cat of catData) {
            if (!subs[cat.id]) {
                try {
                    const subRes = await api.get(`/api/categories/${cat.id}/subcategories?_=${Date.now()}`)
                    subs[cat.id] = Array.isArray(subRes.data) ? subRes.data : []
                } catch { /* ignore */ }
            }
        }
        setSubcategories(subs)
        return response
    }
    const {
        items: paginatedCategories,
        isLoading: categoriesLoading,
        hasMore: categoriesHasMore,
        sentinelRef: categoriesSentinelRef,
        initialLoad: categoriesInitialLoad,
        reset: categoriesReset
    } = useInfiniteScroll(fetchAdminCategories, { limit: 9, deps: [], enabled: tab === 'categories' })

    // Merge paginated categories into the full categories state for forms
    useEffect(() => {
        if (paginatedCategories.length > 0) {
            setCategories(prev => {
                const merged = [...prev]
                paginatedCategories.forEach(cat => {
                    if (!merged.find(c => c.id === cat.id)) {
                        merged.push(cat)
                    }
                })
                return merged
            })
        }
    }, [paginatedCategories])

    // Flattened categories for dropdown
    const allCategoriesList = useMemo(() => {
        let list = []
        categories.forEach(cat => {
            list.push({ id: cat.id, name: cat.name })
            if (subcategories[cat.id]) {
                subcategories[cat.id].forEach(sub => {
                    list.push({ id: sub.id, name: `— ${sub.name}` })
                })
            }
        })
        return list
    }, [categories, subcategories])

    // Load ads (non-paginated tab)
    useEffect(() => {
        if (tab === 'ads') loadAds()
    }, [tab])

    // Load categories if we are in products tab but categories are empty (needed for the form)
    useEffect(() => {
        if (tab === 'products' && categories.length === 0) {
            (async () => {
                try {
                    const { data } = await api.get(`/api/categories?_=${Date.now()}`)
                    const catData = Array.isArray(data) ? data : []
                    setCategories(catData)
                    const subs = {}
                    for (const cat of catData) {
                        try {
                            const subRes = await api.get(`/api/categories/${cat.id}/subcategories?_=${Date.now()}`)
                            subs[cat.id] = Array.isArray(subRes.data) ? subRes.data : []
                        } catch { /* ignore */ }
                    }
                    setSubcategories(subs)
                } catch { /* ignore */ }
            })()
        }
    }, [tab])

    async function loadAds() {
        setLoadingOther(true)
        try {
            const { data } = await api.get(`/api/ads?_=${Date.now()}`)
            setAds(Array.isArray(data) ? data : [])
        } catch { /* ignore */ }
        setLoadingOther(false)
    }

    // For actions that modify data, we need to reset the infinite scroll
    function reloadCurrentTab() {
        if (tab === 'products') productsReset()
        else if (tab === 'categories') { categoriesReset(); setSubcategories({}) }
        else if (tab === 'orders') ordersReset()
        else if (tab === 'ads') loadAds()
    }

    // ── Product actions ──
    async function approveProduct(id) { await api.put(`/api/admin/products/${id}/approve`); productsReset() }
    async function rejectProduct(id) {
        const reason = prompt(t('Enter rejection reason:'))
        if (!reason) return
        await api.put(`/api/admin/products/${id}/reject`, { reason })
        productsReset()
    }
    async function deleteProduct(id) {
        if (!confirm(t('Delete this product permanently?'))) return
        try {
            await api.delete(`/api/admin/products/${id}`)
            productsReset()
        } catch (err) {
            alert(err?.response?.data?.error || 'Failed to delete')
        }
    }
    function startEditProduct(p) {
        const productImages = []
        if (p.imageUrl) productImages.push(p.imageUrl)
        if (p.additionalImageUrls) productImages.push(...p.additionalImageUrls)

        setProductForm({
            name: p.name,
            description: p.description || '',
            price: p.price.toString(),
            images: productImages,
            categoryId: p.categoryId.toString(),
            storeName: p.storeName || '',
            stockQuantity: (p.stockQuantity || 0).toString()
        })
        setEditingProduct(p.id)
        setShowProductForm(true)
    }
    async function handleProductImageUpload(e) {
        const files = Array.from(e.target.files)
        if (!files.length) return

        const currentCount = productForm.images.length
        const remaining = 5 - currentCount
        if (remaining <= 0) { alert(t('Maximum 5 images allowed')); return }

        const toUpload = files.slice(0, remaining)
        setProductUploading(true)

        try {
            for (const file of toUpload) {
                const formData = new FormData()
                formData.append('file', file)
                const { data } = await api.post('/api/admin/products/upload-image', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
                setProductForm(prev => ({ ...prev, images: [...prev.images, data.imageUrl] }))
            }
        } catch { alert(t('Failed to upload some images')) }
        setProductUploading(false)
    }
    // Store name autocomplete search
    async function searchStores(query) {
        if (!query || query.length < 1) { setStoreSuggestions([]); setShowStoreSuggestions(false); return }
        setStoreSearchLoading(true)
        try {
            const { data } = await api.get(`/api/admin/stores?q=${encodeURIComponent(query)}`)
            setStoreSuggestions(Array.isArray(data) ? data : [])
            setShowStoreSuggestions(true)
        } catch { setStoreSuggestions([]) }
        setStoreSearchLoading(false)
    }
    function handleStoreNameChange(e) {
        const val = e.target.value
        setProductForm(prev => ({ ...prev, storeName: val }))
        searchStores(val)
    }
    function selectStore(store) {
        setProductForm(prev => ({ ...prev, storeName: store.shopName }))
        setShowStoreSuggestions(false)
    }

    async function handleProductSubmit(e) {
        e.preventDefault()
        try {
            const mainImage = productForm.images[0] || null
            const additionalImages = productForm.images.slice(1)

            const body = {
                name: productForm.name,
                description: productForm.description || null,
                price: parseFloat(productForm.price),
                imageUrl: mainImage,
                additionalImageUrls: additionalImages,
                categoryId: parseInt(productForm.categoryId),
                stockQuantity: parseInt(productForm.stockQuantity || '0')
            }

            if (editingProduct) {
                await api.put(`/api/admin/products/${editingProduct}`, body)
            } else {
                await api.post('/api/admin/products', {
                    ...body,
                    storeName: productForm.storeName
                })
            }
            setShowProductForm(false)
            setEditingProduct(null)
            setProductForm({ name: '', description: '', price: '', images: [], categoryId: '', storeName: '', stockQuantity: '0' })
            productsReset()
        } catch (err) { alert(err?.response?.data?.error || 'Failed') }
    }

    // ── User actions ──
    async function toggleUser(id, isDisabled) {
        if (isDisabled) await api.put(`/api/admin/users/${id}/enable`)
        else await api.delete(`/api/admin/users/${id}`)
        usersReset()
    }

    // ── Category actions ──
    async function handleCreateCategory(e) {
        e.preventDefault()
        if (!newCatName) return
        setUploading(true)
        try {
            let imageUrl = null
            if (newCatImageFile) {
                const formData = new FormData()
                formData.append('file', newCatImageFile)
                const uploadRes = await api.post('/api/categories/upload-image', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
                imageUrl = uploadRes.data.imageUrl
            }
            await api.post('/api/categories', { name: newCatName, code: newCatCode || null, imageUrl, parentId: newCatParentId ? parseInt(newCatParentId) : null })
            setNewCatName(''); setNewCatCode(''); setNewCatImageFile(null); setNewCatImagePreview(null); setNewCatParentId('')
            categoriesReset()
            setSubcategories({})
        } catch (err) { alert(err?.response?.data?.error || 'Failed to create category') }
        finally { setUploading(false) }
    }
    async function deleteCategory(id) {
        if (!confirm(t('Delete this category? Products in it will be orphaned.'))) return
        try { await api.delete(`/api/categories/${id}`); categoriesReset(); setSubcategories({}) }
        catch (err) { alert(err?.response?.data?.error || 'Failed to delete') }
    }
    async function changeCategoryImage(catId) {
        const input = document.createElement('input')
        input.type = 'file'; input.accept = 'image/*'
        input.onchange = async (e) => {
            const file = e.target.files[0]
            if (!file) return
            try {
                const formData = new FormData()
                formData.append('file', file)
                const { data } = await api.post('/api/categories/upload-image', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
                const cat = categories.find(c => c.id === catId) || paginatedCategories.find(c => c.id === catId)
                await api.put(`/api/categories/${catId}`, { name: cat?.name, imageUrl: data.imageUrl })
                categoriesReset()
                setSubcategories({})
            } catch { alert(t('Failed to upload image')) }
        }
        input.click()
    }
    async function removeCategoryImage(catId) {
        const cat = categories.find(c => c.id === catId) || paginatedCategories.find(c => c.id === catId)
        if (!cat) return
        try { await api.put(`/api/categories/${catId}`, { name: cat.name, imageUrl: null }); categoriesReset(); setSubcategories({}) }
        catch { alert('Failed') }
    }

    // ── Ad Banner actions ──
    async function handleCreateAd(e) {
        e.preventDefault()
        if (!adImageFile) { alert(t('Please select an image')); return }
        setAdUploading(true)
        try {
            const formData = new FormData()
            formData.append('file', adImageFile)
            const { data } = await api.post('/api/ads/upload-image', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
            await api.post('/api/ads', { imageUrl: data.imageUrl, title: adTitle || null, linkUrl: adLinkUrl || null, sortOrder: adSortOrder || 0 })
            setAdImageFile(null); setAdImagePreview(null); setAdTitle(''); setAdLinkUrl(''); setAdSortOrder(0)
            setRefreshKey(k => k + 1)
            loadAds()
        } catch (err) { alert(err?.response?.data?.error || 'Failed to create ad') }
        finally { setAdUploading(false) }
    }
    async function deleteAd(id) {
        if (!confirm(t('Delete this ad banner?'))) return
        try { await api.delete(`/api/ads/${id}`); setRefreshKey(k => k + 1); loadAds() }
        catch { alert('Failed to delete ad') }
    }

    function getImageUrl(url) {
        if (!url) return null
        return url.startsWith('http') ? url : `${api.defaults.baseURL}${url}`
    }
    const statusColor = (s) => ({ Pending: '#f59e0b', Approved: '#4ade80', Rejected: '#f87171' }[s] || '#888')
    const cardStyle = { background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }

    // Skeleton row for admin list items
    const AdminSkeleton = () => (
        <div style={{ ...cardStyle, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', width: '60%' }}>
                <Skeleton width="50%" height="20px" />
                <Skeleton width="80%" height="16px" />
            </div>
            <div style={{ display: 'flex', gap: '8px' }}>
                <Skeleton width="70px" height="32px" borderRadius="8px" />
                <Skeleton width="70px" height="32px" borderRadius="8px" />
            </div>
        </div>
    )

    // Show loading state
    const isTabLoading = (tab === 'products' && productsInitialLoad) ||
        (tab === 'users' && usersInitialLoad) ||
        (tab === 'categories' && categoriesInitialLoad) ||
        (tab === 'orders' && ordersInitialLoad) ||
        (tab === 'ads' && loadingOther)

    return (
        <div style={{ padding: '20px', maxWidth: '900px', margin: '0 auto', color: 'var(--text)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <button onClick={onBack} className="glass-btn">← {t('Back')}</button>
                <h2 style={{ margin: 0, color: 'var(--text-primary)' }}>{t('Admin Panel')}</h2>
                <div />
            </div>

            <div className="seg" style={{ marginBottom: '20px' }}>
                <button className={tab === 'products' ? 'active' : ''} onClick={() => setTab('products')}>{t('Products')}</button>
                <button className={tab === 'users' ? 'active' : ''} onClick={() => setTab('users')}>{t('Users')}</button>
                <button className={tab === 'orders' ? 'active' : ''} onClick={() => setTab('orders')}>{t('Orders')}</button>
                <button className={tab === 'categories' ? 'active' : ''} onClick={() => setTab('categories')}>{t('Categories')}</button>
                <button className={tab === 'ads' ? 'active' : ''} onClick={() => setTab('ads')}>{t('Ads')}</button>
                <button className={tab === 'ai' ? 'active' : ''} onClick={() => setTab('ai')}>AI Assistant</button>
            </div>

            {/* ── Products Tab ── */}
            {tab === 'products' && (
                <>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
                        {!showProductForm ? (
                            <div className="seg">
                                {['Pending', 'Approved', 'Rejected'].map(s => (
                                    <button key={s} className={statusFilter === s ? 'active' : ''} onClick={() => setStatusFilter(s)}>{t(s)}</button>
                                ))}
                            </div>
                        ) : <div />}
                        <button onClick={() => { setShowProductForm(!showProductForm); setEditingProduct(null); setProductForm({ name: '', description: '', price: '', images: [], categoryId: '', storeName: '', stockQuantity: '0' }); setShowStoreSuggestions(false) }}
                            className="primary" style={{ padding: '8px 16px', borderRadius: '8px', fontSize: '13px' }}>
                            {showProductForm ? t('Cancel') : '+ ' + t('Add Product')}
                        </button>
                    </div>
                    {showProductForm && (
                        <div style={{ ...cardStyle, marginBottom: '16px' }}>
                            <h3 style={{ marginTop: 0, color: 'var(--text)' }}>{editingProduct ? t('Edit Product') : t('Create Product (Admin)')}</h3>
                            <form onSubmit={handleProductSubmit}>
                                <div className="sp-form-grid">
                                    <div className="field">
                                        <label>{t('Product name')}</label>
                                        <input className="sp-input" placeholder={t('Product name')} value={productForm.name} onChange={e => setProductForm({ ...productForm, name: e.target.value })} required />
                                    </div>
                                    <div className="field">
                                        <label>{t('Price')}</label>
                                        <input className="sp-input" type="number" step="0.01" placeholder={t('Price')} value={productForm.price} onChange={e => setProductForm({ ...productForm, price: e.target.value })} required />
                                    </div>
                                    <div className="field">
                                        <label>{t('Stock Quantity')}</label>
                                        <input className="sp-input" type="number" placeholder={t('Quantity')} value={productForm.stockQuantity} onChange={e => setProductForm({ ...productForm, stockQuantity: e.target.value })} required />
                                    </div>
                                    <div className="field" style={{ gridColumn: '1 / -1' }}>
                                        <label>{t('Description')}</label>
                                        <textarea className="sp-input" placeholder={t('Description')} value={productForm.description} onChange={e => setProductForm({ ...productForm, description: e.target.value })} rows={3} />
                                    </div>
                                    <div className="field" style={{ gridColumn: '1 / -1' }}>
                                        <label>{t('Product Images')} ({productForm.images.length}/5)</label>
                                        <div className="sp-image-upload-area" style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', border: 'none', padding: 0 }}>
                                            {productForm.images.map((img, idx) => (
                                                <div key={idx} className="sp-image-preview-wrapper" style={{ width: '100px', height: '100px' }}>
                                                    <img src={getImageUrl(img)} alt="Preview" className="sp-image-preview" style={{ borderRadius: '8px' }} />
                                                    <button type="button" className="sp-image-remove-btn" onClick={() => setProductForm(prev => ({ ...prev, images: prev.images.filter((_, i) => i !== idx) }))}>✕</button>
                                                </div>
                                            ))}
                                            {productForm.images.length < 5 && (
                                                <label className="sp-image-upload-label" style={{ width: '100px', height: '100px', margin: 0 }}>
                                                    <input type="file" accept="image/*" multiple onChange={handleProductImageUpload} style={{ display: 'none' }} />
                                                    <div className="sp-image-upload-placeholder" style={{ padding: '10px' }}>
                                                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="12" y1="8" x2="12" y2="16"></line><line x1="8" y1="12" x2="16" y2="12"></line></svg>
                                                        <span style={{ fontSize: '10px' }}>{productUploading ? t('...') : t('Add')}</span>
                                                    </div>
                                                </label>
                                            )}
                                        </div>
                                        {productForm.images.length === 0 && <div style={{ fontSize: '12px', color: '#f87171', marginTop: '5px' }}>{t('At least 1 image is required.')}</div>}
                                    </div>
                                    <div className="field">
                                        <label>{t('Select category')}</label>
                                        <select className="sp-input" value={productForm.categoryId} onChange={e => setProductForm({ ...productForm, categoryId: e.target.value })} required>
                                            <option value="">{t('Select category')}</option>
                                            {allCategoriesList.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                        </select>
                                    </div>
                                    {!editingProduct && (
                                        <div className="field" style={{ position: 'relative' }}>
                                            <label>{t('Store Name')}</label>
                                            <input
                                                className="sp-input"
                                                placeholder={t('Type store name...')}
                                                value={productForm.storeName}
                                                onChange={handleStoreNameChange}
                                                onFocus={() => { if (productForm.storeName) searchStores(productForm.storeName) }}
                                                onBlur={() => setTimeout(() => setShowStoreSuggestions(false), 200)}
                                                autoComplete="off"
                                                required
                                            />
                                            {showStoreSuggestions && storeSuggestions.length > 0 && (
                                                <div style={{
                                                    position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 50,
                                                    background: 'var(--card)', border: '1px solid var(--border)',
                                                    borderRadius: '8px', marginTop: '4px', maxHeight: '200px', overflowY: 'auto',
                                                    boxShadow: '0 8px 24px rgba(0,0,0,0.3)', backdropFilter: 'blur(16px)'
                                                }}>
                                                    {storeSuggestions.map(store => (
                                                        <div key={store.userId}
                                                            onMouseDown={() => selectStore(store)}
                                                            style={{
                                                                padding: '10px 14px', cursor: 'pointer',
                                                                borderBottom: '1px solid var(--border)',
                                                                transition: 'background 0.15s'
                                                            }}
                                                            onMouseEnter={e => e.currentTarget.style.background = 'var(--bg)'}
                                                            onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                                                        >
                                                            <div style={{ fontWeight: '600', color: 'var(--text)', fontSize: '14px' }}>{store.shopName}</div>
                                                            <div style={{ fontSize: '11px', color: 'var(--muted)' }}>{store.storeType} {store.location ? `• ${store.location}` : ''}</div>
                                                        </div>
                                                    ))}
                                                </div>
                                            )}
                                            {showStoreSuggestions && storeSuggestions.length === 0 && productForm.storeName && !storeSearchLoading && (
                                                <div style={{
                                                    position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 50,
                                                    background: 'var(--card)', border: '1px solid var(--border)',
                                                    borderRadius: '8px', marginTop: '4px', padding: '10px 14px',
                                                    color: 'var(--muted)', fontSize: '13px',
                                                    boxShadow: '0 8px 24px rgba(0,0,0,0.3)'
                                                }}>
                                                    {t('No stores found')}
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>
                                <button type="submit" className="sp-btn-gradient" style={{ marginTop: '20px', width: '100%', justifyContent: 'center' }}>
                                    {editingProduct ? t('Update Product') : t('Create Product')}
                                </button>
                            </form>
                        </div>
                    )}
                </>
            )}

            {/* ── Products List (Infinite Scroll) ── */}
            {tab === 'products' && (
                <>
                    {productsInitialLoad ? (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                        </div>
                    ) : (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {products.map(p => (
                                <div key={p.id} style={cardStyle}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <div>
                                            <div style={{ fontWeight: '600', color: 'var(--text)' }}>{p.name}</div>
                                            <div style={{ fontSize: '13px', color: 'var(--muted)' }}>{formatPrice(p.price, p.currencyCode)} • {p.categoryName} • {t('by')} {p.storeName || p.storeUserId}</div>
                                            <span style={{ fontSize: '11px', fontWeight: '600', color: statusColor(p.status), background: `${statusColor(p.status)}15`, padding: '2px 8px', borderRadius: '12px' }}>{t(p.status)}</span>
                                        </div>
                                        <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                                            {p.status !== 'Approved' && <button onClick={() => approveProduct(p.id)} className="admin-btn-success">{t('Approve')}</button>}
                                            {p.status !== 'Rejected' && <button onClick={() => rejectProduct(p.id)} className="admin-btn-danger">{t('Reject')}</button>}
                                            <button onClick={() => startEditProduct(p)} className="admin-btn-action">{t('Edit')}</button>
                                            <button onClick={() => deleteProduct(p.id)} className="admin-btn-danger">{t('Delete')}</button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                            {products.length === 0 && !productsLoading && <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '20px' }}>{t('No products found for status', { status: t(statusFilter).toLowerCase() })}</div>}
                            {productsLoading && !productsInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                        </div>
                    )}
                    {productsHasMore && !productsInitialLoad && <div ref={productsSentinelRef} style={{ height: '1px' }} />}
                    {!productsHasMore && products.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px' }}>
                            <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--border)' }} />
                            {t('No more products')}
                            <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--border)' }} />
                        </div>
                    )}
                    {productsError && products.length > 0 && (
                        <div style={{ textAlign: 'center', padding: '16px', color: 'var(--muted)' }}>
                            <p>{t('Failed to load more.')}</p>
                            <button className="primary" onClick={productsRetry} style={{ padding: '6px 16px', borderRadius: '8px', fontSize: '13px' }}>{t('Retry')}</button>
                        </div>
                    )}
                </>
            )}

            {/* ── Users Tab (Infinite Scroll) ── */}
            {tab === 'users' && (
                <>
                    {usersInitialLoad ? (
                        <div style={{ display: 'grid', gap: '8px' }}>
                            {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                        </div>
                    ) : (
                        <div style={{ display: 'grid', gap: '8px' }}>
                            {users.map(u => (
                                <div key={u.id} style={{ ...cardStyle, display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px' }}>
                                    <div>
                                        <div style={{ fontWeight: '500', color: 'var(--text)' }}>{u.email}</div>
                                        <div style={{ fontSize: '12px', color: 'var(--muted)' }}>{u.accountType} {u.fullName ? `• ${u.fullName}` : ''} {u.isDisabled ? `• ⛔ ${t('Disabled')}` : ''}</div>
                                    </div>
                                    <button onClick={() => toggleUser(u.id, u.isDisabled)}
                                        className={u.isDisabled ? "admin-btn-success" : "admin-btn-danger"}>
                                        {u.isDisabled ? t('Enable') : t('Disable')}
                                    </button>
                                </div>
                            ))}
                            {usersLoading && !usersInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                        </div>
                    )}
                    {usersHasMore && !usersInitialLoad && <div ref={usersSentinelRef} style={{ height: '1px' }} />}
                    {!usersHasMore && users.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px' }}>{t('End of users list')}</div>
                    )}
                </>
            )}

            {/* ── Orders Tab (Infinite Scroll) ── */}
            {tab === 'orders' && (
                <>
                    {ordersInitialLoad ? (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                        </div>
                    ) : (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {orders.map(o => (
                                <div key={o.id} style={cardStyle}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <strong style={{ color: 'var(--text)' }}>{t('Order')} #{o.id}</strong>
                                        <span style={{ fontSize: '12px', fontWeight: '600', color: '#888' }}>{t(o.status)}</span>
                                    </div>
                                    <div style={{ fontSize: '13px', color: 'var(--muted)', marginTop: '4px' }}>
                                        {t('User:')} {o.userId} • {t('Store:')} {o.storeName || o.storeUserId} • {t('Total:')} {formatPrice(o.totalAmount, o.currencyCode)}
                                    </div>
                                </div>
                            ))}
                            {orders.length === 0 && <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '20px' }}>{t('No orders.')}</div>}
                            {ordersLoading && !ordersInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                        </div>
                    )}
                    {ordersHasMore && !ordersInitialLoad && <div ref={ordersSentinelRef} style={{ height: '1px' }} />}
                    {!ordersHasMore && orders.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px' }}>{t('End of orders list')}</div>
                    )}
                </>
            )}

            {/* ── Categories Tab (Infinite Scroll) ── */}
            {tab === 'categories' && (
                <div>
                    <div style={{ ...cardStyle, marginBottom: '24px' }}>
                        <h3 style={{ marginTop: 0, color: 'var(--text)' }}>{t('Create New Category')}</h3>
                        <form onSubmit={handleCreateCategory} style={{ display: 'grid', gap: '12px' }}>
                            <input value={newCatName} onChange={e => setNewCatName(e.target.value)} placeholder={t('Category name')} className="admin-input" required />
                            <input value={newCatCode} onChange={e => setNewCatCode(e.target.value)} placeholder={t('Unique Code (optional)')} className="admin-input" />
                            {!newCatParentId && (
                                <div>
                                    <label style={{ display: 'block', fontSize: '13px', marginBottom: '4px', color: 'var(--muted)' }}>{t('Category Image')}</label>
                                    {newCatImagePreview && <img src={newCatImagePreview} alt="" style={{ width: '64px', height: '64px', borderRadius: '8px', objectFit: 'cover', marginBottom: '8px' }} />}
                                    <input type="file" accept="image/*" onChange={e => { const f = e.target.files[0]; if (f) { setNewCatImageFile(f); setNewCatImagePreview(URL.createObjectURL(f)) } }} className="admin-input" />
                                </div>
                            )}
                            <select value={newCatParentId} onChange={e => { setNewCatParentId(e.target.value); if (e.target.value) { setNewCatImageFile(null); setNewCatImagePreview(null) } }} className="admin-input">
                                <option value="">{t('None (Top-level Category)')}</option>
                                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                            </select>
                            <button type="submit" className="primary" disabled={uploading}>{uploading ? t('Creating...') : t('Create Category')}</button>
                        </form>
                    </div>

                    <h3 style={{ color: 'var(--text)' }}>{t('Existing Categories')}</h3>
                    {categoriesInitialLoad ? (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                        </div>
                    ) : (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {paginatedCategories.map(c => (
                                <div key={c.id} style={cardStyle}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                            {c.imageUrl ? (
                                                <img src={getImageUrl(c.imageUrl)} alt={c.name} style={{ width: '48px', height: '48px', borderRadius: '8px', objectFit: 'cover' }} />
                                            ) : (
                                                <div style={{ width: '48px', height: '48px', borderRadius: '8px', background: 'var(--bg)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--muted)' }}>📷</div>
                                            )}
                                            <div>
                                                <div style={{ fontWeight: '600', color: 'var(--text)' }}>{c.name} <span style={{ fontSize: '11px', color: 'var(--muted)' }}>({c.code})</span></div>
                                            </div>
                                        </div>
                                        <div style={{ display: 'flex', gap: '6px' }}>
                                            <button onClick={() => changeCategoryImage(c.id)} className="admin-btn-action">{t('Change Image')}</button>
                                            {c.imageUrl && <button onClick={() => removeCategoryImage(c.id)} className="admin-btn-danger">{t('Remove Image')}</button>}
                                            <button onClick={() => deleteCategory(c.id)} className="admin-btn-danger">{t('Delete')}</button>
                                        </div>
                                    </div>
                                    {subcategories[c.id] && subcategories[c.id].length > 0 && (
                                        <div style={{ marginTop: '12px', paddingLeft: '60px', display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                                            {subcategories[c.id].map(sub => (
                                                <span key={sub.id} style={{ background: 'var(--bg)', border: '1px solid var(--border)', padding: '4px 10px', borderRadius: '16px', fontSize: '13px', color: 'var(--muted)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                                                    {sub.name}
                                                    <button onClick={() => deleteCategory(sub.id)} style={{ background: 'none', border: 'none', color: '#ef4444', cursor: 'pointer', fontSize: '13px', padding: 0, lineHeight: 1 }}>✕</button>
                                                </span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            ))}
                            {paginatedCategories.length === 0 && !categoriesLoading && <div style={{ color: 'var(--muted)' }}>{t('No categories found.')}</div>}
                            {categoriesLoading && !categoriesInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                        </div>
                    )}
                    {categoriesHasMore && !categoriesInitialLoad && <div ref={categoriesSentinelRef} style={{ height: '1px' }} />}
                    {!categoriesHasMore && paginatedCategories.length > 0 && (
                        <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '24px 0', fontSize: '13px' }}>{t('End of categories list')}</div>
                    )}
                </div>
            )}

            {/* ── AI Assistant Tab ── */}
            {tab === 'ai' && (
                <AiDashboardTab />
            )}

            {/* ── Ads Tab (no pagination per user request) ── */}
            {tab === 'ads' && (
                <div>
                    <div style={{ ...cardStyle, marginBottom: '24px' }}>
                        <h3 style={{ marginTop: 0, color: 'var(--text)' }}>{t('Add New Ad Banner')}</h3>
                        <form onSubmit={handleCreateAd} style={{ display: 'grid', gap: '12px' }}>
                            <input value={adTitle} onChange={e => setAdTitle(e.target.value)} placeholder={t('Title (optional)')} className="admin-input" />
                            <input value={adLinkUrl} onChange={e => setAdLinkUrl(e.target.value)} placeholder={t('Link URL (opens when clicked)')} className="admin-input" />
                            <input type="number" value={adSortOrder} onChange={e => setAdSortOrder(parseInt(e.target.value) || 0)} placeholder={t('Sort Order')} className="admin-input" />
                            <div>
                                <label style={{ display: 'block', fontSize: '13px', marginBottom: '4px', color: 'var(--muted)' }}>{t('Banner Image')} *</label>
                                {adImagePreview && <img src={adImagePreview} alt="" style={{ width: '100%', maxHeight: '120px', objectFit: 'cover', borderRadius: '8px', marginBottom: '8px' }} />}
                                <input type="file" accept="image/*" onChange={e => { const f = e.target.files[0]; if (f) { setAdImageFile(f); setAdImagePreview(URL.createObjectURL(f)) } }} className="admin-input" required />
                            </div>
                            <button type="submit" className="primary" disabled={adUploading}>{adUploading ? t('Uploading...') : t('Create Ad Banner')}</button>
                        </form>
                    </div>

                    <h3 style={{ color: 'var(--text)' }}>{t('Existing Ad Banners')}</h3>
                    {loadingOther ? (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={i} />)}
                        </div>
                    ) : (
                        <div style={{ display: 'grid', gap: '12px' }}>
                            {ads.map(ad => (
                                <div key={ad.id} style={{ ...cardStyle, display: 'flex', alignItems: 'center', gap: '16px' }}>
                                    <img src={`${getImageUrl(ad.imageUrl)}?r=${refreshKey}`} alt={ad.title || 'Ad'} style={{ width: '120px', height: '60px', borderRadius: '8px', objectFit: 'cover' }} />
                                    <div style={{ flex: 1 }}>
                                        <div style={{ fontWeight: '600', color: 'var(--text)' }}>{ad.title || t('(No title)')}</div>
                                        {ad.linkUrl && <div style={{ fontSize: '12px', color: 'var(--primary)' }}>{ad.linkUrl}</div>}
                                        <div style={{ fontSize: '11px', color: 'var(--muted)' }}>{t('Order:')} {ad.sortOrder}</div>
                                    </div>
                                    <button onClick={() => deleteAd(ad.id)} className="admin-btn-danger">{t('Delete')}</button>
                                </div>
                            ))}
                            {ads.length === 0 && <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '20px' }}>{t('No ad banners yet.')}</div>}
                        </div>
                    )}
                </div>
            )}
        </div>
    )
}
