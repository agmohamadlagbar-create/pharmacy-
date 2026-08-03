import { useState, useEffect, useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import Skeleton from '../components/Skeleton.jsx'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import AiDashboardTab from '../components/AiDashboardTab.jsx'
import './AdminPage.css'

export default function AdminPage({ onBack }) {
    const { t } = useTranslation()
    const [tab, setTab] = useState('products')
    const [stats, setStats] = useState({ totalProducts: 0, pendingProducts: 0, totalUsers: 0, totalOrders: 0, totalCategories: 0, totalAds: 0 })
    const [categories, setCategories] = useState([])
    const [subcategories, setSubcategories] = useState({})
    const [ads, setAds] = useState([])
    const [loadingOther, setLoadingOther] = useState(false)
    const [statusFilter, setStatusFilter] = useState('Pending')

    // Custom confirm modal state
    const [confirmModal, setConfirmModal] = useState({ open: false, title: '', message: '', onConfirm: null })

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
    const [productUploading, setProductUploading] = useState(false)

    // Store autocomplete state
    const [storeSuggestions, setStoreSuggestions] = useState([])
    const [showStoreSuggestions, setShowStoreSuggestions] = useState(false)
    const [storeSearchLoading, setStoreSearchLoading] = useState(false)

    // Fetch live Admin stats Overview
    const fetchAdminStats = useCallback(async () => {
        try {
            const { data } = await api.get(`/api/admin/stats?_=${Date.now()}`)
            setStats(data || { totalProducts: 0, pendingProducts: 0, totalUsers: 0, totalOrders: 0, totalCategories: 0, totalAds: 0 })
        } catch { /* ignore */ }
    }, [])

    useEffect(() => {
        fetchAdminStats()
    }, [fetchAdminStats])

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
    } = useInfiniteScroll(fetchAdminProducts, { limit: 9, deps: [statusFilter], enabled: tab === 'products' && !showProductForm })

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
        const catData = Array.isArray(data) ? data : (data.data || data.items || [])
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

    function reloadCurrentTab() {
        fetchAdminStats()
        if (tab === 'products') productsReset()
        else if (tab === 'categories') { categoriesReset(); setSubcategories({}) }
        else if (tab === 'orders') ordersReset()
        else if (tab === 'users') usersReset()
        else if (tab === 'ads') loadAds()
    }

    // ── Product actions ──
    async function approveProduct(id) { await api.put(`/api/admin/products/${id}/approve`); productsReset(); fetchAdminStats() }
    async function rejectProduct(id) {
        const reason = prompt(t('Enter rejection reason:'))
        if (!reason) return
        await api.put(`/api/admin/products/${id}/reject`, { reason })
        productsReset()
        fetchAdminStats()
    }
    function deleteProduct(id) {
        setConfirmModal({
            open: true,
            title: t('Delete Product'),
            message: t('Are you sure you want to permanently delete this product? This action cannot be undone.'),
            onConfirm: async () => {
                try {
                    await api.delete(`/api/admin/products/${id}`)
                    productsReset()
                    fetchAdminStats()
                } catch (err) {
                    alert(err?.response?.data?.error || 'Failed to delete product')
                }
            }
        })
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

    // Store autocomplete search
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
            fetchAdminStats()
        } catch (err) { alert(err?.response?.data?.error || 'Failed') }
    }

    // ── User actions ──
    async function toggleUser(id, isDisabled) {
        if (isDisabled) await api.put(`/api/admin/users/${id}/enable`)
        else await api.delete(`/api/admin/users/${id}`)
        usersReset()
        fetchAdminStats()
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
            fetchAdminStats()
        } catch (err) { alert(err?.response?.data?.error || 'Failed to create category') }
        finally { setUploading(false) }
    }
    function deleteCategory(id) {
        setConfirmModal({
            open: true,
            title: t('Delete Category'),
            message: t('Are you sure you want to delete this category? Products in it will be orphaned.'),
            onConfirm: async () => {
                try {
                    await api.delete(`/api/categories/${id}`)
                    categoriesReset()
                    setSubcategories({})
                    fetchAdminStats()
                } catch (err) {
                    alert(err?.response?.data?.error || 'Failed to delete category')
                }
            }
        })
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
            fetchAdminStats()
        } catch (err) { alert(err?.response?.data?.error || 'Failed to create ad') }
        finally { setAdUploading(false) }
    }
    function deleteAd(id) {
        setConfirmModal({
            open: true,
            title: t('Delete Ad Banner'),
            message: t('Are you sure you want to delete this ad banner?'),
            onConfirm: async () => {
                try {
                    await api.delete(`/api/ads/${id}`)
                    setRefreshKey(k => k + 1)
                    loadAds()
                    fetchAdminStats()
                } catch {
                    alert('Failed to delete ad')
                }
            }
        })
    }

    function getImageUrl(url) {
        if (!url) return null
        return url.startsWith('http') ? url : `${api.defaults.baseURL}${url}`
    }

    // Skeleton row
    const AdminSkeleton = () => (
        <div className="admin-item-card">
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', width: '60%' }}>
                <Skeleton width="50%" height="20px" />
                <Skeleton width="80%" height="16px" />
            </div>
            <div className="admin-btn-group">
                <Skeleton width="70px" height="32px" borderRadius="8px" />
                <Skeleton width="70px" height="32px" borderRadius="8px" />
            </div>
        </div>
    )

    return (
        <div className="admin-page">
            <div className="admin-container">
                {/* Header */}
                <div className="admin-header">
                    <button onClick={onBack} className="admin-back-btn">
                        <span className="material-symbols-outlined">arrow_back</span>
                        {t('Back')}
                    </button>
                    <div className="admin-title-area">
                        <span className="material-symbols-outlined admin-title-icon">admin_panel_settings</span>
                        <h2 className="admin-title">{t('Admin Control Panel')}</h2>
                    </div>
                    <button onClick={reloadCurrentTab} className="admin-refresh-btn">
                        <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>refresh</span>
                        {t('Refresh')}
                    </button>
                </div>

                {/* Bento Grid Overview */}
                <section className="admin-bento-grid">
                    <div className="admin-bento-card">
                        <span className="admin-bento-label">{t('Total Products')}</span>
                        <div className="admin-bento-value highlight">
                            {stats.totalProducts}
                        </div>
                        <span className="admin-bento-subtext">{stats.pendingProducts} {t('pending review')}</span>
                    </div>
                    <div className="admin-bento-card">
                        <span className="admin-bento-label">{t('Registered Users')}</span>
                        <div className="admin-bento-value">
                            {stats.totalUsers}
                        </div>
                        <span className="admin-bento-subtext muted">{t('Active accounts')}</span>
                    </div>
                    <div className="admin-bento-card">
                        <span className="admin-bento-label">{t('Total Orders')}</span>
                        <div className="admin-bento-value">
                            {stats.totalOrders}
                        </div>
                        <span className="admin-bento-subtext">{t('System orders')}</span>
                    </div>
                    <div className="admin-bento-card">
                        <span className="admin-bento-label">{t('Categories & Ads')}</span>
                        <div className="admin-bento-value">
                            {stats.totalCategories + stats.totalAds}
                        </div>
                        <span className="admin-bento-subtext muted">{t('Catalog items')}</span>
                    </div>
                </section>

                {/* Main Navigation Tabs */}
                <div className="admin-tabs-nav">
                    <button className={`admin-tab-btn ${tab === 'products' ? 'active' : ''}`} onClick={() => setTab('products')}>
                        <span className="material-symbols-outlined admin-tab-icon">inventory_2</span>
                        <span>{t('Products')}</span>
                    </button>
                    <button className={`admin-tab-btn ${tab === 'users' ? 'active' : ''}`} onClick={() => setTab('users')}>
                        <span className="material-symbols-outlined admin-tab-icon">group</span>
                        <span>{t('Users')}</span>
                    </button>
                    <button className={`admin-tab-btn ${tab === 'orders' ? 'active' : ''}`} onClick={() => setTab('orders')}>
                        <span className="material-symbols-outlined admin-tab-icon">receipt_long</span>
                        <span>{t('Orders')}</span>
                    </button>
                    <button className={`admin-tab-btn ${tab === 'categories' ? 'active' : ''}`} onClick={() => setTab('categories')}>
                        <span className="material-symbols-outlined admin-tab-icon">category</span>
                        <span>{t('Categories')}</span>
                    </button>
                    <button className={`admin-tab-btn ${tab === 'ads' ? 'active' : ''}`} onClick={() => setTab('ads')}>
                        <span className="material-symbols-outlined admin-tab-icon">campaign</span>
                        <span>{t('Ads')}</span>
                    </button>
                    <button className={`admin-tab-btn ${tab === 'ai' ? 'active' : ''}`} onClick={() => setTab('ai')}>
                        <span className="material-symbols-outlined admin-tab-icon">auto_awesome</span>
                        <span>{t('AI Assistant')}</span>
                    </button>
                </div>

                {/* ── Products Tab ── */}
                {tab === 'products' && (
                    <>
                        {showProductForm ? (
                            /* Single-purpose Add / Edit Product Form View (Hides product list) */
                            <div className="admin-card-box">
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                                    <h3 className="admin-card-title" style={{ margin: 0 }}>{editingProduct ? t('Edit Product') : t('Create Product (Admin)')}</h3>
                                    <button type="button" onClick={() => { setShowProductForm(false); setEditingProduct(null); }} className="admin-btn-action">
                                        <span className="material-symbols-outlined" style={{ fontSize: '18px', verticalAlign: 'middle', marginRight: '4px' }}>close</span>
                                        {t('Back to Products')}
                                    </button>
                                </div>
                                <form onSubmit={handleProductSubmit}>
                                    <div className="admin-grid-form">
                                        <div className="admin-field">
                                            <label>{t('Product name')}</label>
                                            <input className="admin-input-style" placeholder={t('Product name')} value={productForm.name} onChange={e => setProductForm({ ...productForm, name: e.target.value })} required />
                                        </div>
                                        <div className="admin-field">
                                            <label>{t('Price')}</label>
                                            <input className="admin-input-style" type="number" step="0.01" placeholder={t('Price')} value={productForm.price} onChange={e => setProductForm({ ...productForm, price: e.target.value })} required />
                                        </div>
                                        <div className="admin-field">
                                            <label>{t('Stock Quantity')}</label>
                                            <input className="admin-input-style" type="number" placeholder={t('Quantity')} value={productForm.stockQuantity} onChange={e => setProductForm({ ...productForm, stockQuantity: e.target.value })} required />
                                        </div>
                                        <div className="admin-field full-width">
                                            <label>{t('Description')}</label>
                                            <textarea className="admin-input-style" placeholder={t('Description')} value={productForm.description} onChange={e => setProductForm({ ...productForm, description: e.target.value })} rows={3} />
                                        </div>
                                        <div className="admin-field full-width">
                                            <label>{t('Product Images')} ({productForm.images.length}/5)</label>
                                            <div className="admin-file-upload-row">
                                                {productForm.images.map((img, idx) => (
                                                    <div key={idx} className="admin-file-preview-card">
                                                        <img src={getImageUrl(img)} alt={t('Preview')} className="admin-file-preview-img" />
                                                        <button type="button" onClick={() => setProductForm(prev => ({ ...prev, images: prev.images.filter((_, i) => i !== idx) }))}
                                                            className="admin-file-remove-btn">✕</button>
                                                    </div>
                                                ))}
                                                {productForm.images.length < 5 && (
                                                    <label className="admin-file-dropzone">
                                                        <input type="file" accept="image/*" multiple onChange={handleProductImageUpload} style={{ display: 'none' }} />
                                                        <span className="material-symbols-outlined">add_photo_alternate</span>
                                                        <span className="admin-file-dropzone-text">{productUploading ? t('...') : t('Add Image')}</span>
                                                    </label>
                                                )}
                                            </div>
                                            {productForm.images.length === 0 && <div style={{ fontSize: '12px', color: '#ef4444', marginTop: '4px' }}>{t('At least 1 image is required.')}</div>}
                                        </div>
                                        <div className="admin-field">
                                            <label>{t('Select category')}</label>
                                            <select className="admin-input-style" value={productForm.categoryId} onChange={e => setProductForm({ ...productForm, categoryId: e.target.value })} required>
                                                <option value="">{t('Select category')}</option>
                                                {allCategoriesList.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                            </select>
                                        </div>
                                        {!editingProduct && (
                                            <div className="admin-field" style={{ position: 'relative' }}>
                                                <label>{t('Store Name')}</label>
                                                <input
                                                    className="admin-input-style"
                                                    placeholder={t('Type store name...')}
                                                    value={productForm.storeName}
                                                    onChange={handleStoreNameChange}
                                                    onFocus={() => { if (productForm.storeName) searchStores(productForm.storeName) }}
                                                    onBlur={() => setTimeout(() => setShowStoreSuggestions(false), 200)}
                                                    autoComplete="off"
                                                    required
                                                />
                                                {showStoreSuggestions && storeSuggestions.length > 0 && (
                                                    <div className="admin-store-suggestions">
                                                        {storeSuggestions.map(store => (
                                                            <div key={store.userId}
                                                                onMouseDown={() => selectStore(store)}
                                                                className="admin-store-suggestion-item"
                                                            >
                                                                <div style={{ fontWeight: '600', color: 'var(--on-surface)', fontSize: '14px' }}>{store.shopName}</div>
                                                                <div style={{ fontSize: '11px', color: 'var(--on-surface-variant)' }}>{store.storeType} {store.location ? `• ${store.location}` : ''}</div>
                                                            </div>
                                                        ))}
                                                    </div>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                    <button type="submit" className="admin-add-btn" style={{ marginTop: '20px', width: '100%', justifyContent: 'center' }}>
                                        {editingProduct ? t('Update Product') : t('Create Product')}
                                    </button>
                                </form>
                            </div>
                        ) : (
                            /* Default Products List View */
                            <>
                                <div className="admin-sub-segments">
                                    <div className="admin-seg-group">
                                        {['Pending', 'Approved', 'Rejected'].map(s => (
                                            <button key={s} className={`admin-seg-btn ${statusFilter === s ? 'active' : ''}`} onClick={() => setStatusFilter(s)}>
                                                {t(s)}
                                            </button>
                                        ))}
                                    </div>
                                    <button onClick={() => { setShowProductForm(true); setEditingProduct(null); setProductForm({ name: '', description: '', price: '', images: [], categoryId: '', storeName: '', stockQuantity: '0' }); setShowStoreSuggestions(false) }}
                                        className="admin-add-btn">
                                        <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>add</span>
                                        {t('Add Product')}
                                    </button>
                                </div>

                                {productsInitialLoad ? (
                                    <div className="admin-list-grid">
                                        {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                                    </div>
                                ) : (
                                    <div className="admin-list-grid">
                                        {products.map(p => (
                                            <div key={p.id} className="admin-item-card">
                                                <div className="admin-item-info">
                                                    <div className="admin-item-title">{p.name}</div>
                                                    <div className="admin-item-sub">{formatPrice(p.price, p.currencyCode)} • {p.categoryName} • {t('by')} {p.storeName || p.storeUserId}</div>
                                                    <span className={`admin-badge admin-badge-${p.status.toLowerCase()}`}>{t(p.status)}</span>
                                                </div>
                                                <div className="admin-btn-group">
                                                    {p.status !== 'Approved' && <button onClick={() => approveProduct(p.id)} className="admin-btn-success">{t('Approve')}</button>}
                                                    {p.status !== 'Rejected' && <button onClick={() => rejectProduct(p.id)} className="admin-btn-danger">{t('Reject')}</button>}
                                                    <button onClick={() => startEditProduct(p)} className="admin-btn-action">{t('Edit')}</button>
                                                    <button onClick={() => deleteProduct(p.id)} className="admin-btn-danger">{t('Delete')}</button>
                                                </div>
                                            </div>
                                        ))}
                                        {products.length === 0 && !productsLoading && (
                                            <div className="admin-empty-state">{t('No products found for status', { status: t(statusFilter).toLowerCase() })}</div>
                                        )}
                                        {productsLoading && !productsInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                                    </div>
                                )}
                                {productsHasMore && !productsInitialLoad && <div ref={productsSentinelRef} style={{ height: '1px' }} />}
                            </>
                        )}
                    </>
                )}

                {/* ── Users Tab ── */}
                {tab === 'users' && (
                    <>
                        {usersInitialLoad ? (
                            <div className="admin-list-grid">
                                {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                            </div>
                        ) : (
                            <div className="admin-list-grid">
                                {users.map(u => (
                                    <div key={u.id} className="admin-item-card">
                                        <div className="admin-item-info">
                                            <div className="admin-item-title">{u.email}</div>
                                            <div className="admin-item-sub">{u.accountType} {u.fullName ? `• ${u.fullName}` : ''} {u.isDisabled ? `• ⛔ ${t('Disabled')}` : ''}</div>
                                        </div>
                                        <button onClick={() => toggleUser(u.id, u.isDisabled)} className={u.isDisabled ? "admin-btn-success" : "admin-btn-danger"}>
                                            {u.isDisabled ? t('Enable') : t('Disable')}
                                        </button>
                                    </div>
                                ))}
                                {usersLoading && !usersInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                            </div>
                        )}
                        {usersHasMore && !usersInitialLoad && <div ref={usersSentinelRef} style={{ height: '1px' }} />}
                    </>
                )}

                {/* ── Orders Tab ── */}
                {tab === 'orders' && (
                    <>
                        {ordersInitialLoad ? (
                            <div className="admin-list-grid">
                                {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                            </div>
                        ) : (
                            <div className="admin-list-grid">
                                {orders.map(o => (
                                    <div key={o.id} className="admin-item-card">
                                        <div className="admin-item-info">
                                            <div className="admin-item-title">{t('Order')} #{o.id}</div>
                                            <div className="admin-item-sub">{t('User:')} {o.userId} • {t('Store:')} {o.storeName || o.storeUserId} • {t('Total:')} {formatPrice(o.totalAmount, o.currencyCode)}</div>
                                        </div>
                                        <span className={`admin-badge admin-badge-${(o.status || 'pending').toLowerCase()}`}>{t(o.status)}</span>
                                    </div>
                                ))}
                                {orders.length === 0 && <div className="admin-empty-state">{t('No orders.')}</div>}
                                {ordersLoading && !ordersInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                            </div>
                        )}
                        {ordersHasMore && !ordersInitialLoad && <div ref={ordersSentinelRef} style={{ height: '1px' }} />}
                    </>
                )}

                {/* ── Categories Tab ── */}
                {tab === 'categories' && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-lg)' }}>
                        <div className="admin-card-box">
                            <h3 className="admin-card-title">{t('Create New Category')}</h3>
                            <form onSubmit={handleCreateCategory} style={{ display: 'grid', gap: '14px' }}>
                                <input value={newCatName} onChange={e => setNewCatName(e.target.value)} placeholder={t('Category name')} className="admin-input-style" required />
                                <input value={newCatCode} onChange={e => setNewCatCode(e.target.value)} placeholder={t('Unique Code (optional)')} className="admin-input-style" />
                                {!newCatParentId && (
                                    <div className="admin-field full-width">
                                        <label>{t('Category Image')}</label>
                                        <div className="admin-file-upload-row">
                                            {newCatImagePreview ? (
                                                <div className="admin-file-preview-card">
                                                    <img src={newCatImagePreview} alt={t('Preview')} className="admin-file-preview-img" />
                                                    <button type="button" onClick={() => { setNewCatImageFile(null); setNewCatImagePreview(null); }} className="admin-file-remove-btn">✕</button>
                                                </div>
                                            ) : (
                                                <label className="admin-file-dropzone">
                                                    <input type="file" accept="image/*" onChange={e => { const f = e.target.files[0]; if (f) { setNewCatImageFile(f); setNewCatImagePreview(URL.createObjectURL(f)) } }} style={{ display: 'none' }} />
                                                    <span className="material-symbols-outlined">add_photo_alternate</span>
                                                    <span className="admin-file-dropzone-text">{t('Upload Image')}</span>
                                                </label>
                                            )}
                                        </div>
                                    </div>
                                )}
                                <select value={newCatParentId} onChange={e => { setNewCatParentId(e.target.value); if (e.target.value) { setNewCatImageFile(null); setNewCatImagePreview(null) } }} className="admin-input-style">
                                    <option value="">{t('None (Top-level Category)')}</option>
                                    {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                </select>
                                <button type="submit" className="admin-add-btn" disabled={uploading} style={{ justifyContent: 'center' }}>
                                    {uploading ? t('Creating...') : t('Create Category')}
                                </button>
                            </form>
                        </div>

                        {categoriesInitialLoad ? (
                            <div className="admin-list-grid">
                                {Array.from({ length: 5 }).map((_, i) => <AdminSkeleton key={i} />)}
                            </div>
                        ) : (
                            <div className="admin-list-grid">
                                {paginatedCategories.map(c => (
                                    <div key={c.id} className="admin-item-card" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                                {c.imageUrl ? (
                                                    <img src={getImageUrl(c.imageUrl)} alt={c.name} style={{ width: '48px', height: '48px', borderRadius: '8px', objectFit: 'cover' }} />
                                                ) : (
                                                    <div style={{ width: '48px', height: '48px', borderRadius: '8px', background: 'var(--surface-container-high)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--on-surface-variant)' }}>📷</div>
                                                )}
                                                <div>
                                                    <div className="admin-item-title">{c.name} <span style={{ fontSize: '11px', color: 'var(--on-surface-variant)' }}>({c.code})</span></div>
                                                </div>
                                            </div>
                                            <div className="admin-btn-group">
                                                <button onClick={() => changeCategoryImage(c.id)} className="admin-btn-action">{t('Change Image')}</button>
                                                {c.imageUrl && <button onClick={() => removeCategoryImage(c.id)} className="admin-btn-danger">{t('Remove Image')}</button>}
                                                <button onClick={() => deleteCategory(c.id)} className="admin-btn-danger">{t('Delete')}</button>
                                            </div>
                                        </div>
                                        {subcategories[c.id] && subcategories[c.id].length > 0 && (
                                            <div style={{ marginTop: '12px', paddingLeft: '60px', display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                                                {subcategories[c.id].map(sub => (
                                                    <span key={sub.id} className="admin-subcat-chip">
                                                        {sub.name}
                                                        <button onClick={() => deleteCategory(sub.id)} className="admin-subcat-del">✕</button>
                                                    </span>
                                                ))}
                                            </div>
                                        )}
                                    </div>
                                ))}
                                {paginatedCategories.length === 0 && !categoriesLoading && <div className="admin-empty-state">{t('No categories found.')}</div>}
                                {categoriesLoading && !categoriesInitialLoad && Array.from({ length: 3 }).map((_, i) => <AdminSkeleton key={`more-${i}`} />)}
                            </div>
                        )}
                        {categoriesHasMore && !categoriesInitialLoad && <div ref={categoriesSentinelRef} style={{ height: '1px' }} />}
                    </div>
                )}

                {/* ── Ads Tab ── */}
                {tab === 'ads' && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--spacing-lg)' }}>
                        <div className="admin-card-box">
                            <h3 className="admin-card-title">{t('Add New Ad Banner')}</h3>
                            <form onSubmit={handleCreateAd} style={{ display: 'grid', gap: '14px' }}>
                                <input value={adTitle} onChange={e => setAdTitle(e.target.value)} placeholder={t('Title (optional)')} className="admin-input-style" />
                                <input value={adLinkUrl} onChange={e => setAdLinkUrl(e.target.value)} placeholder={t('Link URL (opens when clicked)')} className="admin-input-style" />
                                <input type="number" value={adSortOrder} onChange={e => setAdSortOrder(parseInt(e.target.value) || 0)} placeholder={t('Sort Order')} className="admin-input-style" />
                                <div className="admin-field full-width">
                                    <label>{t('Banner Image')} *</label>
                                    <div className="admin-file-upload-row">
                                        {adImagePreview ? (
                                            <div className="admin-file-preview-card" style={{ width: '200px', height: '100px' }}>
                                                <img src={adImagePreview} alt={t('Preview')} className="admin-file-preview-img" />
                                                <button type="button" onClick={() => { setAdImageFile(null); setAdImagePreview(null); }} className="admin-file-remove-btn">✕</button>
                                            </div>
                                        ) : (
                                            <label className="admin-file-dropzone" style={{ width: '150px' }}>
                                                <input type="file" accept="image/*" onChange={e => { const f = e.target.files[0]; if (f) { setAdImageFile(f); setAdImagePreview(URL.createObjectURL(f)) } }} style={{ display: 'none' }} required />
                                                <span className="material-symbols-outlined">add_photo_alternate</span>
                                                <span className="admin-file-dropzone-text">{t('Upload Banner')}</span>
                                            </label>
                                        )}
                                    </div>
                                </div>
                                <button type="submit" className="admin-add-btn" disabled={adUploading} style={{ justifyContent: 'center' }}>
                                    {adUploading ? t('Uploading...') : t('Create Ad Banner')}
                                </button>
                            </form>
                        </div>

                        <div className="admin-list-grid">
                            {ads.map(ad => (
                                <div key={ad.id} className="admin-item-card">
                                    <img src={`${getImageUrl(ad.imageUrl)}?r=${refreshKey}`} alt={ad.title || 'Ad'} style={{ width: '120px', height: '60px', borderRadius: '8px', objectFit: 'cover' }} />
                                    <div style={{ flex: 1 }}>
                                        <div className="admin-item-title">{ad.title || t('(No title)')}</div>
                                        {ad.linkUrl && <div style={{ fontSize: '12px', color: 'var(--primary)' }}>{ad.linkUrl}</div>}
                                        <div className="admin-item-sub">{t('Order:')} {ad.sortOrder}</div>
                                    </div>
                                    <button onClick={() => deleteAd(ad.id)} className="admin-btn-danger">{t('Delete')}</button>
                                </div>
                            ))}
                            {ads.length === 0 && <div className="admin-empty-state">{t('No ad banners yet.')}</div>}
                        </div>
                    </div>
                )}

                {/* ── AI Assistant Tab ── */}
                {tab === 'ai' && (
                    <AiDashboardTab />
                )}
            </div>

            {/* Custom Styled Confirmation Modal (Light & Dark Theme Compatible) */}
            {confirmModal.open && (
                <div className="admin-modal-overlay" onClick={() => setConfirmModal(prev => ({ ...prev, open: false }))}>
                    <div className="admin-modal-card" onClick={e => e.stopPropagation()}>
                        <div className="admin-modal-header">
                            <div className="admin-modal-icon-danger">
                                <span className="material-symbols-outlined">warning</span>
                            </div>
                            <h3 className="admin-modal-title">{confirmModal.title}</h3>
                        </div>
                        <p className="admin-modal-message">{confirmModal.message}</p>
                        <div className="admin-modal-actions">
                            <button 
                                className="admin-modal-cancel-btn"
                                onClick={() => setConfirmModal(prev => ({ ...prev, open: false }))}
                            >
                                {t('Cancel')}
                            </button>
                            <button 
                                className="admin-modal-confirm-btn"
                                onClick={async () => {
                                    const action = confirmModal.onConfirm;
                                    setConfirmModal(prev => ({ ...prev, open: false }));
                                    if (action) await action();
                                }}
                            >
                                {t('Delete')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
