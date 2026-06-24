import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import Skeleton from '../components/Skeleton.jsx'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import './StoreProductsPage.css'

export default function StoreProductsPage({ onBack }) {
    const { t } = useTranslation()
    const [showForm, setShowForm] = useState(false)
    const [categories, setCategories] = useState([])
    const [subcategories, setSubcategories] = useState([])
    const [form, setForm] = useState({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' })
    const [editingId, setEditingId] = useState(null)
    const [msg, setMsg] = useState(null)
    const [searchQuery, setSearchQuery] = useState('')
    const [statusFilter, setStatusFilter] = useState('All')
    const [uploading, setUploading] = useState(false)
    const [searchDebounced, setSearchDebounced] = useState('')

    // Debounce search query
    useEffect(() => {
        const timer = setTimeout(() => setSearchDebounced(searchQuery), 300)
        return () => clearTimeout(timer)
    }, [searchQuery])

    // ── Infinite scroll ──
    const fetchMyProducts = useCallback(async (page, limit) => {
        const params = new URLSearchParams({
            page: page.toString(),
            limit: limit.toString(),
            _: Date.now().toString()
        })
        if (statusFilter !== 'All') params.set('status', statusFilter)
        if (searchDebounced) params.set('search', searchDebounced)
        const { data } = await api.get(`/api/products/mine?${params.toString()}`)
        return data
    }, [statusFilter, searchDebounced])

    const {
        items: products,
        isLoading,
        hasMore,
        error,
        sentinelRef,
        retry,
        reset,
        initialLoad,
        total
    } = useInfiniteScroll(fetchMyProducts, {
        limit: 9,
        deps: [statusFilter, searchDebounced]
    })

    // Load categories on mount
    useEffect(() => {
        (async () => {
            try {
                const { data } = await api.get(`/api/categories?_=${Date.now()}`)
                setCategories(Array.isArray(data) ? data : [])
            } catch (err) {
                setMsg({ type: 'error', text: err.message })
            }
        })()
    }, [])

    async function handleImageUpload(e) {
        const files = Array.from(e.target.files)
        if (!files.length) return
        
        const currentCount = form.images.length
        const remaining = 5 - currentCount
        if (remaining <= 0) { setMsg({ type: 'warning', text: t('Maximum 5 images allowed') }); return }
        
        const toUpload = files.slice(0, remaining)
        setUploading(true)
        setMsg(null)
        
        try {
            for (const file of toUpload) {
                const formData = new FormData()
                formData.append('file', file)
                const url = form.categoryId ? `/api/products/upload-image?categoryId=${form.categoryId}` : '/api/products/upload-image'
                const { data } = await api.post(url, formData, {
                    headers: { 'Content-Type': 'multipart/form-data' }
                })
                setForm(prev => ({ ...prev, images: [...prev.images, data.imageUrl] }))
            }
            setMsg({ type: 'ok', text: t('Uploaded {{count}} image(s)!', { count: toUpload.length }) })
        } catch (err) {
            setMsg({ type: 'error', text: t('Some uploads failed:') + ' ' + (err?.response?.data?.error || err.message) })
        } finally {
            setUploading(false)
        }
    }

    async function fetchSubcategories(catId) {
        try {
            const { data } = await api.get(`/api/categories/${catId}/subcategories?_=${Date.now()}`)
            setSubcategories(Array.isArray(data) ? data : [])
        } catch {
            setSubcategories([])
        }
    }

    async function handleSubmit(e) {
        e.preventDefault()
        setMsg(null)
        try {
            const finalCategoryId = form.subcategoryId ? parseInt(form.subcategoryId) : parseInt(form.categoryId)
            const mainImage = form.images[0] || null
            const additionalImages = form.images.slice(1)

            const body = {
                name: form.name,
                description: form.description || null,
                price: parseFloat(form.price),
                imageUrl: mainImage,
                additionalImageUrls: additionalImages,
                categoryId: finalCategoryId,
                stockQuantity: parseInt(form.stockQuantity || '0')
            }
            if (editingId) {
                await api.put(`/api/products/${editingId}`, body)
                setMsg({ type: 'ok', text: t('Product updated (pending re-approval)') })
            } else {
                await api.post('/api/products', body)
                setMsg({ type: 'ok', text: t('Product created (pending approval)') })
            }
            setShowForm(false)
            setEditingId(null)
            setForm({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' })
            setSubcategories([])
            reset()
        } catch (err) {
            setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
        }
    }

    function startEdit(p) {
        const productImages = []
        if (p.imageUrl) productImages.push(p.imageUrl)
        if (p.additionalImageUrls) productImages.push(...p.additionalImageUrls)

        setForm({ 
            name: p.name, 
            description: p.description || '', 
            price: p.price.toString(), 
            images: productImages,
            categoryId: p.categoryId.toString(), 
            subcategoryId: '',
            stockQuantity: (p.stockQuantity || 0).toString()
        })
        setEditingId(p.id)
        setShowForm(true)
        fetchSubcategories(p.categoryId)
    }

    async function handleDelete(id) {
        if (!confirm(t('Delete this product?'))) return
        try {
            await api.delete(`/api/products/${id}`)
            reset()
        } catch (err) {
            setMsg({ type: 'error', text: err?.response?.data?.error || err.message })
        }
    }

    function getImageUrl(url) {
        if (!url) return null;
        if (url.startsWith('http')) return url;
        return `${api.defaults.baseURL}${url}`;
    }

    // Skeleton card matching the real product card layout
    const SkeletonCard = () => (
        <div className="sp-glass-card sp-product-card">
            <div className="sp-product-image" style={{ padding: 0 }}>
                <Skeleton width="100%" height="100%" borderRadius="16px 16px 0 0" />
            </div>
            <div className="sp-product-content">
                <div className="sp-product-header">
                    <Skeleton width="60%" height="20px" />
                    <Skeleton width="60px" height="24px" borderRadius="12px" />
                </div>
                <div className="sp-product-meta" style={{ marginTop: '12px' }}>
                    <Skeleton width="40%" height="16px" />
                </div>
                <div className="sp-product-actions" style={{ marginTop: '16px' }}>
                    <Skeleton width="45%" height="32px" borderRadius="8px" />
                    <Skeleton width="45%" height="32px" borderRadius="8px" />
                </div>
            </div>
        </div>
    )

    return (
        <div className="store-products-page">
            <div className="sp-container">
                <div className="sp-header">
                    <button onClick={onBack} className="sp-back-btn">
                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
                        {t('Back')}
                    </button>
                    <h2 className="sp-title">{t('My Products')}</h2>
                    <button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' }); setSubcategories([]); }}
                        className="sp-btn-gradient">
                        {showForm ? t('Cancel') : '+ ' + t('Add Product')}
                    </button>
                </div>

                {msg && <div className={`msg ${msg.type}`} style={{ marginBottom: '16px' }}>{msg.text}</div>}

                {showForm && (
                    <form onSubmit={handleSubmit} className="sp-glass-card sp-form">
                        <h3 style={{ margin: '0 0 16px', color: 'var(--text-primary)' }}>{editingId ? t('Edit Product') : t('Create New Product')}</h3>
                        <div className="sp-form-grid">
                            <div className="field">
                                <label>{t('Name')}</label>
                                <input className="sp-input" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
                            </div>
                            <div className="field">
                                <label>{t('Price ($)')}</label>
                                <input className="sp-input" type="number" step="0.01" value={form.price} onChange={e => setForm({ ...form, price: e.target.value })} required />
                            </div>
                            <div className="field">
                                <label>{t('Stock Quantity')}</label>
                                <input className="sp-input" type="number" value={form.stockQuantity} onChange={e => setForm({ ...form, stockQuantity: e.target.value })} required />
                            </div>
                            <div className="field" style={{ gridColumn: '1 / -1' }}>
                                <label>{t('Description')}</label>
                                <textarea className="sp-input" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} rows={3} />
                            </div>
                            <div className="field" style={{ gridColumn: '1 / -1' }}>
                                <label>{t('Product Images')} ({form.images.length}/5)</label>
                                <div className="sp-image-upload-area" style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', border: 'none', padding: 0 }}>
                                    {form.images.map((img, idx) => (
                                        <div key={idx} className="sp-image-preview-wrapper" style={{ width: '100px', height: '100px', margin: 0 }}>
                                            <img src={getImageUrl(img)} alt="Preview" className="sp-image-preview" style={{ borderRadius: '8px' }} />
                                            <button type="button" className="sp-image-remove-btn" onClick={() => setForm(prev => ({ ...prev, images: prev.images.filter((_, i) => i !== idx) }))}>✕</button>
                                        </div>
                                    ))}
                                    {form.images.length < 5 && (
                                        <label className="sp-image-upload-label" style={{ width: '100px', height: '100px', margin: 0 }}>
                                            <input type="file" accept="image/*" multiple onChange={handleImageUpload} style={{ display: 'none' }} />
                                            <div className="sp-image-upload-placeholder" style={{ padding: '10px' }}>
                                                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="12" y1="8" x2="12" y2="16"></line><line x1="8" y1="12" x2="16" y2="12"></line></svg>
                                                <span style={{ fontSize: '10px' }}>{uploading ? t('...') : t('Add')}</span>
                                            </div>
                                        </label>
                                    )}
                                </div>
                                {form.images.length === 0 && <div style={{ fontSize: '12px', color: '#f87171', marginTop: '5px' }}>{t('At least 1 image is required.')}</div>}
                            </div>
                            <div className="field">
                                <label>{t('Category')}</label>
                                <select className="sp-input" value={form.categoryId} onChange={e => {
                                    setForm({ ...form, categoryId: e.target.value, subcategoryId: '' })
                                    if (e.target.value) fetchSubcategories(e.target.value)
                                    else setSubcategories([])
                                }} required>
                                    <option value="">{t('Select category')}</option>
                                    {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                </select>
                            </div>
                            {subcategories.length > 0 && (
                                <div className="field">
                                    <label>{t('Subcategory')}</label>
                                    <select className="sp-input" value={form.subcategoryId} onChange={e => setForm({ ...form, subcategoryId: e.target.value })}>
                                        <option value="">{t('None (use main category)')}</option>
                                        {subcategories.map(sc => <option key={sc.id} value={sc.id}>{sc.name}</option>)}
                                    </select>
                                </div>
                            )}
                        </div>
                        <button type="submit" className="sp-btn-gradient" style={{ marginTop: '20px', width: '100%', justifyContent: 'center' }}>
                            {editingId ? t('Update Product') : t('Create Product')}
                        </button>
                    </form>
                )}

                {!showForm && (
                    <div className="sp-filters-row">
                        <div className="sp-search-wrapper">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                            <input
                                className="sp-input sp-search-input"
                                placeholder={t('Search products...')}
                                value={searchQuery}
                                onChange={e => setSearchQuery(e.target.value)}
                            />
                        </div>
                        <div className="sp-filter-pills">
                            {['All', 'Approved', 'Pending'].map(status => (
                                <button
                                    key={status}
                                    className={`sp-filter-pill ${statusFilter === status ? 'active' : ''}`}
                                    onClick={() => setStatusFilter(status)}
                                >
                                    {t(status)}
                                </button>
                            ))}
                        </div>
                    </div>
                )}

                {initialLoad ? (
                    <div className="sp-products-grid">
                        {Array.from({ length: 6 }).map((_, i) => (
                            <SkeletonCard key={i} />
                        ))}
                    </div>
                ) : products.length === 0 && !isLoading ? (
                    <div className="sp-empty-state">
                        {searchQuery || statusFilter !== 'All' 
                            ? t('No products match your search/filters.') 
                            : t('No products yet. Add your first product!')
                        }
                    </div>
                ) : (
                    <div className="sp-products-grid">
                        {products.map(p => (
                            <div key={p.id} className="sp-glass-card sp-product-card">
                                <div className="sp-product-image">
                                    {getImageUrl(p.imageUrl) ? (
                                        <img src={getImageUrl(p.imageUrl)} alt={p.name} />
                                    ) : (
                                        <div className="sp-product-img-placeholder">
                                            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" opacity="0.3"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><circle cx="8.5" cy="8.5" r="1.5"></circle><polyline points="21 15 16 10 5 21"></polyline></svg>
                                        </div>
                                    )}
                                </div>
                                <div className="sp-product-content">
                                    <div className="sp-product-header">
                                        <div className="sp-product-name">{p.name}</div>
                                        <span className={`sp-badge sp-badge-${p.status.toLowerCase()}`}>
                                            {t(p.status)}
                                        </span>
                                    </div>
                                    <div className="sp-product-meta">
                                        <span className="sp-product-price">${(p.price || 0).toFixed(2)}</span>
                                        <span className="sp-product-dot">•</span>
                                        <span className="sp-product-category">{p.categoryName || t('No category')}</span>
                                    </div>
                                    {p.rejectionReason && <div className="sp-product-reason">{t('Reason:')} {p.rejectionReason}</div>}
                                    <div className="sp-product-actions">
                                        <button className="sp-btn-edit" onClick={() => startEdit(p)}>{t('Edit')}</button>
                                        <button className="sp-btn-delete" onClick={() => handleDelete(p.id)}>{t('Delete')}</button>
                                    </div>
                                </div>
                            </div>
                        ))}
                        {/* Loading more skeleton cards */}
                        {isLoading && !initialLoad && (
                            Array.from({ length: 3 }).map((_, i) => (
                                <SkeletonCard key={`skeleton-more-${i}`} />
                            ))
                        )}
                    </div>
                )}

                {/* Sentinel for IntersectionObserver */}
                {hasMore && !initialLoad && <div ref={sentinelRef} style={{ height: '1px', width: '100%' }} />}

                {/* End of list message */}
                {!hasMore && products.length > 0 && (
                    <div style={{ textAlign: 'center', color: 'var(--muted, #888)', padding: '24px 0 48px', fontSize: '13px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px' }}>
                        <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--border, rgba(255,255,255,0.1))' }} />
                        {t('No more products')}
                        <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--border, rgba(255,255,255,0.1))' }} />
                    </div>
                )}

                {/* Error retry */}
                {error && products.length > 0 && (
                    <div style={{ textAlign: 'center', padding: '16px', color: 'var(--muted, #888)' }}>
                        <p style={{ marginBottom: '8px' }}>{t('Failed to load more products.')}</p>
                        <button className="sp-btn-gradient" onClick={retry} style={{ display: 'inline-flex', padding: '8px 20px' }}>{t('Retry')}</button>
                    </div>
                )}
            </div>
        </div>
    )
}
