import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import Skeleton from '../components/Skeleton.jsx'
import useInfiniteScroll from '../hooks/useInfiniteScroll.js'
import './StoreProductsPage.css'

export default function StoreProductsPage({ onBack }) {
    const { t } = useTranslation()
    const [searchParams, setSearchParams] = useSearchParams()
    const editProductId = searchParams.get('editProductId')
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
    const [errors, setErrors] = useState({})
    const [touched, setTouched] = useState({})

    const validateField = useCallback((fieldName, value) => {
        let err = ''
        if (fieldName === 'name') {
            if (!value || !value.trim()) {
                err = t('Please enter a product name')
            }
        } else if (fieldName === 'price') {
            if (!value || isNaN(parseFloat(value)) || parseFloat(value) <= 0) {
                err = t('Please enter a valid price greater than 0')
            }
        } else if (fieldName === 'stockQuantity') {
            if (value === '' || value === undefined || value === null) {
                err = t('Please enter the stock quantity')
            } else if (isNaN(parseInt(value)) || parseInt(value) < 0) {
                err = t('Stock quantity cannot be negative')
            }
        } else if (fieldName === 'categoryId') {
            if (!value) {
                err = t('Please select a category for the product')
            }
        }
        return err
    }, [t])

    const validateForm = useCallback(() => {
        const newErrors = {}
        const nameErr = validateField('name', form.name)
        if (nameErr) newErrors.name = nameErr

        const priceErr = validateField('price', form.price)
        if (priceErr) newErrors.price = priceErr

        const stockErr = validateField('stockQuantity', form.stockQuantity)
        if (stockErr) newErrors.stockQuantity = stockErr

        const catErr = validateField('categoryId', form.categoryId)
        if (catErr) newErrors.categoryId = catErr

        if (form.images.length === 0) {
            newErrors.images = t('Please upload at least one image')
        }

        setErrors(newErrors)
        return Object.keys(newErrors).length === 0
    }, [form, validateField, t])

    const handleFieldChange = (fieldName, value) => {
        setForm(prev => {
            const next = { ...prev, [fieldName]: value }
            if (touched[fieldName]) {
                const err = validateField(fieldName, value)
                setErrors(e => ({ ...e, [fieldName]: err }))
            }
            return next
        })
    }

    const handleFieldBlur = (fieldName) => {
        setTouched(prev => ({ ...prev, [fieldName]: true }))
        const err = validateField(fieldName, form[fieldName])
        setErrors(e => ({ ...e, [fieldName]: err }))
    }

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
        setTouched(prev => ({ ...prev, images: true }))
        
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

        // Touch all inputs so that skipping shows errors
        const allTouched = { name: true, price: true, stockQuantity: true, categoryId: true, images: true }
        setTouched(allTouched)

        const isValid = validateForm()
        if (!isValid) {
            setMsg({ type: 'error', text: t('Please check the form for fields with errors.') })
            return
        }

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
                const { data } = await api.put(`/api/products/${editingId}`, body)
                if (data && data.status === 'Approved') {
                    setMsg({ type: 'ok', text: t('Product updated successfully!') })
                } else {
                    setMsg({ type: 'ok', text: t('Product updated (pending re-approval)') })
                }
            } else {
                await api.post('/api/products', body)
                setMsg({ type: 'ok', text: t('Product created (pending approval)') })
            }
            setShowForm(false)
            setEditingId(null)
            setForm({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' })
            setSubcategories([])
            setErrors({})
            setTouched({})
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
        setErrors({})
        setTouched({})
        setEditingId(p.id)
        setShowForm(true)
        fetchSubcategories(p.categoryId)
    }

    // Handle editProductId parameter from URL
    useEffect(() => {
        if (editProductId) {
            (async () => {
                try {
                    const { data } = await api.get(`/api/products/mine?productId=${editProductId}`)
                    const items = data && (data.data || data.items)
                    if (items && items.length > 0) {
                        const prod = items[0]
                        startEdit(prod)
                    }
                    // Clear the parameter so it doesn't open again on subsequent renders
                    const newParams = new URLSearchParams(searchParams)
                    newParams.delete('editProductId')
                    setSearchParams(newParams, { replace: true })
                } catch (err) {
                    console.error('Failed to fetch product for editing:', err)
                }
            })()
        }
    }, [editProductId, setSearchParams, searchParams])

    // Scroll to top on mount and when form is opened
    useEffect(() => {
        const isJSDom = typeof window !== 'undefined' && window.navigator && window.navigator.userAgent && window.navigator.userAgent.includes('jsdom')
        if (!isJSDom && typeof window.scrollTo === 'function') {
            try {
                window.scrollTo(0, 0)
            } catch (e) {
                console.warn(e)
            }
        }
    }, [])

    useEffect(() => {
        const isJSDom = typeof window !== 'undefined' && window.navigator && window.navigator.userAgent && window.navigator.userAgent.includes('jsdom')
        if (showForm && !isJSDom && typeof window.scrollTo === 'function') {
            try {
                window.scrollTo({ top: 0, behavior: 'smooth' })
            } catch (e) {
                try {
                    window.scrollTo(0, 0)
                } catch (err) {
                    console.warn(err)
                }
            }
        }
    }, [showForm])

    // Auto-dismiss page/submit notification messages after 5 seconds
    useEffect(() => {
        if (msg) {
            const timer = setTimeout(() => setMsg(null), 5000)
            return () => clearTimeout(timer)
        }
    }, [msg])

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

    function handleBackClick() {
        if (showForm) {
            setShowForm(false)
            setEditingId(null)
            setForm({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' })
            setSubcategories([])
            setErrors({})
            setTouched({})
        } else {
            onBack()
        }
    }

    // Skeleton card matching the real product card layout
    const SkeletonCard = () => (
        <div className="sp-glass-card sp-product-card">
            <div className="sp-product-image">
                <Skeleton width="100%" height="100%" />
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
                    <Skeleton width="80px" height="32px" borderRadius="8px" />
                    <Skeleton width="80px" height="32px" borderRadius="8px" />
                </div>
            </div>
        </div>
    )

    return (
        <div className="store-products-page">
            <div className="sp-container">
                {/* Header */}
                <div className="sp-header">
                    <button onClick={handleBackClick} className="sp-back-btn">
                        <span className="material-symbols-outlined">arrow_back</span>
                        {!showForm && t('Back')}
                    </button>
                    <h2 className="sp-title">{t('My Products')}</h2>
                    {!showForm && (
                        <button onClick={() => { setShowForm(true); setEditingId(null); setForm({ name: '', description: '', price: '', images: [], categoryId: '', subcategoryId: '', stockQuantity: '0' }); setSubcategories([]); }}
                            className="sp-btn-gradient">
                            + {t('Add Product')}
                        </button>
                    )}
                </div>

                {msg && (
                    <div className={`msg ${msg.type} ${msg.type === 'ok' ? 'msg-floating' : ''}`} style={{ marginBottom: '16px' }}>
                        {msg.type === 'ok' && <span className="material-symbols-outlined" style={{ fontSize: '20px', color: 'var(--primary)' }}>check_circle</span>}
                        <span>{msg.text}</span>
                    </div>
                )}

                {/* Bento Stats Grid */}
                {!showForm && (
                    <div className="sp-bento-grid">
                        <div className="sp-bento-card">
                            <div className="sp-bento-icon-wrapper">
                                <span className="material-symbols-outlined">inventory</span>
                            </div>
                            <div className="sp-bento-info">
                                <span className="sp-bento-label">{t('Total Products')}</span>
                                <span className="sp-bento-value">{total ?? products.length}</span>
                                <span className="sp-bento-subtext">{t('Items in Catalog')}</span>
                            </div>
                        </div>
                        <div className="sp-bento-card">
                            <div className="sp-bento-icon-wrapper">
                                <span className="material-symbols-outlined">check_circle</span>
                            </div>
                            <div className="sp-bento-info">
                                <span className="sp-bento-label">{t('Active')}</span>
                                <span className="sp-bento-value">
                                    {products.filter(p => p.status === 'Approved').length}
                                </span>
                                <span className="sp-bento-subtext">{t('Live Products')}</span>
                            </div>
                        </div>
                        <div className="sp-bento-card">
                            <div className="sp-bento-icon-wrapper low-stock">
                                <span className="material-symbols-outlined">warning</span>
                            </div>
                            <div className="sp-bento-info">
                                <span className="sp-bento-label">{t('Low Stock')}</span>
                                <span className="sp-bento-value">
                                    {products.filter(p => p.stockQuantity < 10).length}
                                </span>
                                <span className="sp-bento-subtext low-stock">{t('Items low in stock')}</span>
                            </div>
                        </div>
                    </div>
                )}

                {showForm && (
                    <form onSubmit={handleSubmit} className="sp-glass-card sp-form" noValidate>
                        <h3 style={{ margin: '0 0 16px', color: 'var(--on-surface)' }}>{editingId ? t('Edit Product') : t('Create New Product')}</h3>
                        <div className="sp-form-grid">
                            <div className="field">
                                <label>{t('Name')}</label>
                                <input 
                                    className={`sp-input ${errors.name && touched.name ? 'input-error' : ''}`} 
                                    value={form.name} 
                                    onChange={e => handleFieldChange('name', e.target.value)} 
                                    onBlur={() => handleFieldBlur('name')}
                                />
                                {errors.name && touched.name && <span className="sp-error-text">{errors.name}</span>}
                            </div>
                            <div className="field">
                                <label>{t('Price')}</label>
                                <input 
                                    className={`sp-input ${errors.price && touched.price ? 'input-error' : ''}`} 
                                    type="number" 
                                    step="0.01" 
                                    value={form.price} 
                                    onChange={e => handleFieldChange('price', e.target.value)} 
                                    onBlur={() => handleFieldBlur('price')}
                                />
                                {errors.price && touched.price && <span className="sp-error-text">{errors.price}</span>}
                            </div>
                            <div className="field">
                                <label>{t('Stock Quantity')}</label>
                                <input 
                                    className={`sp-input ${errors.stockQuantity && touched.stockQuantity ? 'input-error' : ''}`} 
                                    type="number" 
                                    value={form.stockQuantity} 
                                    onChange={e => handleFieldChange('stockQuantity', e.target.value)} 
                                    onBlur={() => handleFieldBlur('stockQuantity')}
                                />
                                {errors.stockQuantity && touched.stockQuantity && <span className="sp-error-text">{errors.stockQuantity}</span>}
                            </div>
                            <div className="field" style={{ gridColumn: '1 / -1' }}>
                                <label>{t('Description')}</label>
                                <textarea className="sp-input" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} rows={3} />
                            </div>
                            <div className="field" style={{ gridColumn: '1 / -1' }}>
                                <label>{t('Product Images')} ({form.images.length}/5)</label>
                                <div className="sp-image-upload-area">
                                    {form.images.map((img, idx) => (
                                        <div key={idx} className="sp-image-preview-wrapper">
                                            <img src={getImageUrl(img)} alt={t('Preview')} className="sp-image-preview" />
                                            <button type="button" className="sp-image-remove-btn" onClick={() => {
                                                setForm(prev => ({ ...prev, images: prev.images.filter((_, i) => i !== idx) }))
                                                setTouched(prev => ({ ...prev, images: true }))
                                            }}>✕</button>
                                        </div>
                                    ))}
                                    {form.images.length < 5 && (
                                        <label className="sp-image-upload-label">
                                            <input type="file" accept="image/*" multiple onChange={handleImageUpload} style={{ display: 'none' }} />
                                            <div className="sp-image-upload-placeholder">
                                                <span className="material-symbols-outlined" style={{ fontSize: 24 }}>add_photo_alternate</span>
                                                <span>{uploading ? t('...') : t('Add')}</span>
                                            </div>
                                        </label>
                                    )}
                                </div>
                                {touched.images && form.images.length === 0 && <span className="sp-error-text">{t('Please upload at least one image')}</span>}
                            </div>
                            <div className="field">
                                <label>{t('Category')}</label>
                                <select 
                                    className={`sp-input ${errors.categoryId && touched.categoryId ? 'input-error' : ''}`} 
                                    value={form.categoryId} 
                                    onChange={e => {
                                        handleFieldChange('categoryId', e.target.value)
                                        setForm(f => ({ ...f, subcategoryId: '' }))
                                        if (e.target.value) fetchSubcategories(e.target.value)
                                        else setSubcategories([])
                                    }}
                                    onBlur={() => handleFieldBlur('categoryId')}
                                >
                                    <option value="">{t('Select category')}</option>
                                    {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                                </select>
                                {errors.categoryId && touched.categoryId && <span className="sp-error-text">{errors.categoryId}</span>}
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
                        <button type="submit" className="sp-btn-gradient" style={{ marginTop: '24px', width: '100%' }}>
                            {editingId ? t('Update Product') : t('Create Product')}
                        </button>
                    </form>
                )}

                {!showForm && (
                    <div className="sp-filters-row">
                        <div className="sp-search-wrapper">
                            <span className="material-symbols-outlined">search</span>
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

                {!showForm && (
                    <>
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
                                                    <span className="material-symbols-outlined" style={{ fontSize: 32 }}>image</span>
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
                                                <span className="sp-product-price">{formatPrice(p.price, p.currencyCode)}</span>
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
                            <div style={{ textAlign: 'center', color: 'var(--on-surface-variant)', opacity: 0.6, padding: '24px 0 48px', fontSize: '13px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px' }}>
                                <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--outline-variant)' }} />
                                {t('No more products')}
                                <span style={{ flex: 1, maxWidth: '60px', height: '1px', background: 'var(--outline-variant)' }} />
                            </div>
                        )}

                        {/* Error retry */}
                        {error && products.length > 0 && (
                            <div style={{ textAlign: 'center', padding: '16px', color: 'var(--on-surface-variant)' }}>
                                <p style={{ marginBottom: '8px' }}>{t('Failed to load more products.')}</p>
                                <button className="sp-btn-gradient" onClick={retry} style={{ display: 'inline-flex', padding: '8px 20px' }}>{t('Retry')}</button>
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    )
}
