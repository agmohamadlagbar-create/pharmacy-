import { useState, useEffect } from 'react'
import { api } from '../apiClient.js'
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { useTranslation } from 'react-i18next'
import './EditProfilePage.css'

// Fix default marker icon in Leaflet
delete L.Icon.Default.prototype._getIconUrl
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
    iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
})

function MapEvents({ onMapClick }) {
    useMapEvents({
        click: (e) => {
            onMapClick(e.latlng.lat, e.latlng.lng)
        },
    })
    return null
}

export default function EditProfilePage({ onBack, onSave }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [formData, setFormData] = useState({
        fullName: '',
        country: '',
        street: '',
        buildingNumber: '',
        city: '',
        postalCode: '',
        district: '',
        neighborhood: '',
        floor: '',
        apartmentNo: '',
        latitude: 41.0082,
        longitude: 28.9784,
        description: '',
        openingTime: '',
        closingTime: '',
        accountType: ''
    })
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)
    const [errorModal, setErrorModal] = useState({ open: false, title: '', message: '' })

    useEffect(() => {
        fetchProfile().then(() => {
            if (window.location.hash === '#address') {
                setTimeout(() => {
                    const el = document.getElementById('address-section')
                    if (el) {
                        el.scrollIntoView({ behavior: 'smooth', block: 'center' })
                        const firstInput = el.querySelector('input')
                        if (firstInput) firstInput.focus()
                    }
                }, 150)
            }
        })
    }, [])

    async function fetchProfile() {
        try {
            const res = await api.get('/api/me')
            const data = res.data
            setFormData({
                fullName: data.fullName || '',
                country: data.country || '',
                street: data.street || '',
                buildingNumber: data.buildingNumber || '',
                city: data.city || '',
                postalCode: data.postalCode || '',
                district: data.district || '',
                neighborhood: data.neighborhood || '',
                floor: data.floor || '',
                apartmentNo: data.apartmentNo || '',
                latitude: data.latitude || 41.0082,
                longitude: data.longitude || 28.9784,
                description: data.description || '',
                openingTime: data.openingTime || '',
                closingTime: data.closingTime || '',
                accountType: data.accountType || ''
            })
        } catch (err) {
            console.error('Failed to fetch profile', err)
        } finally {
            setLoading(false)
        }
    }

    async function handleMapClick(lat, lng) {
        setFormData(prev => ({ ...prev, latitude: lat, longitude: lng }))

        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`, {
                headers: { 'User-Agent': 'OnlineStoreWeb' }
            })
            const data = await res.json()
            if (data.address) {
                const addr = data.address
                setFormData(prev => ({
                    ...prev,
                    street: addr.road || addr.suburb || prev.street,
                    city: (addr.city || addr.town || addr.village || addr.state || prev.city).trim(),
                    postalCode: addr.postcode || prev.postalCode,
                    country: addr.country || prev.country
                }))
            }
        } catch (err) {
            console.error('Geocoding error', err)
        }
    }

    function handleChange(e) {
        const { name, value } = e.target
        setFormData(prev => ({ ...prev, [name]: value }))
    }

    async function handleSubmit(e) {
        e.preventDefault()
        setSaving(true)

        // Frontend validation for Customer (User) accounts
        if (formData.accountType === 'User') {
            const requiredFields = [
                { key: 'street', label: t('Address Information') },
                { key: 'district', label: t('District') },
                { key: 'neighborhood', label: t('Neighborhood') },
                { key: 'buildingNumber', label: t('Building Number') },
                { key: 'floor', label: t('Floor') },
                { key: 'apartmentNo', label: t('Apartment No.') },
                { key: 'city', label: t('City') },
            ]
            const missing = requiredFields.filter(f => !(formData[f.key] || '').trim())
            if (missing.length > 0) {
                setSaving(false)
                setErrorModal({
                    open: true,
                    title: t('Missing Required Fields'),
                    message: `${t('Please fill in the following fields:')}\n\n${missing.map(f => '• ' + f.label).join('\n')}`
                })
                return
            }
        }

        const payload = {
            fullName: (formData.fullName || '').trim(),
            country: (formData.country || '').trim(),
            street: (formData.street || '').trim(),
            buildingNumber: (formData.buildingNumber || '').trim(),
            city: (formData.city || '').trim(),
            postalCode: (formData.postalCode || '').trim(),
            district: (formData.district || '').trim(),
            neighborhood: (formData.neighborhood || '').trim(),
            floor: (formData.floor || '').trim(),
            apartmentNo: (formData.apartmentNo || '').trim(),
            latitude: formData.latitude ? parseFloat(formData.latitude) : 0,
            longitude: formData.longitude ? parseFloat(formData.longitude) : 0,
            description: formData.description ? formData.description.trim() : null,
            openingTime: formData.openingTime || null,
            closingTime: formData.closingTime || null
        }

        try {
            await api.put('/api/me/profile', payload)
            if (typeof onSave === 'function') {
                onSave()
            } else if (typeof onBack === 'function') {
                onBack()
            }
        } catch (err) {
            console.error('Save error', err)
            const serverMsg = err?.response?.data?.error || err?.response?.data?.message || (Array.isArray(err?.response?.data?.errors) ? err.response.data.errors.join(', ') : null) || err.message || t('Unknown error occurred')
            setErrorModal({
                open: true,
                title: t('Profile Update Failed'),
                message: `${t('We could not update your profile address.')}\n\n${t('Reason:')} ${serverMsg}`
            })
        } finally {
            setSaving(false)
        }
    }

    if (loading) return (
        <div className="edit-profile-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            <header className="edit-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Edit Profile')}</h1>
            </header>
            <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
                <div className="spinner" style={{ width: 40, height: 40, border: '4px solid var(--outline-variant)', borderTop: '4px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
            </div>
        </div>
    )

    return (
        <div className="edit-profile-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Header */}
            <header className="edit-header">
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Edit Profile')}</h1>
                <div style={{ width: 44 }} />
            </header>

            <main className="edit-content">
                <form className="edit-form" onSubmit={handleSubmit}>
                    {/* Personal Information */}
                    <div className="form-section">
                        <h3>{t('Personal Information')}</h3>
                        <div className="input-group">
                            <label>{formData.accountType === 'Shop' ? t('Shop Name') : t('Full Name')}</label>
                            <input
                                type="text"
                                name="fullName"
                                value={formData.fullName}
                                onChange={handleChange}
                                placeholder={t('Enter your full name')}
                                required
                            />
                        </div>
                        <div className="input-group" style={{ marginTop: '12px' }}>
                            <label>{t('Country')}</label>
                            <input
                                type="text"
                                name="country"
                                value={formData.country}
                                onChange={handleChange}
                                placeholder={t('e.g. Iraq, Turkey...')}
                            />
                        </div>
                    </div>

                    {/* Store Information (Shop Owners Only) */}
                    {formData.accountType === 'Shop' && (
                        <div className="form-section">
                            <h3>{t('Store Details')}</h3>
                            <div className="input-group">
                                <label>{t('Description')}</label>
                                <textarea
                                    name="description"
                                    value={formData.description}
                                    onChange={handleChange}
                                    placeholder={t('Tell customers about your store...')}
                                    rows="3"
                                    className="edit-textarea"
                                />
                            </div>
                            <div className="input-grid" style={{ marginTop: '12px' }}>
                                <div className="input-group">
                                    <label>{t('Opening Time')}</label>
                                    <input
                                        type="time"
                                        name="openingTime"
                                        value={formData.openingTime}
                                        onChange={handleChange}
                                    />
                                </div>
                                <div className="input-group">
                                    <label>{t('Closing Time')}</label>
                                    <input
                                        type="time"
                                        name="closingTime"
                                        value={formData.closingTime}
                                        onChange={handleChange}
                                    />
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Address Details */}
                    <div className="form-section" id="address-section">
                        <h3>{t('Address Details')}</h3>

                        {/* Address Information (full width) */}
                        <div className="input-group">
                            <label>
                                {t('Address Information')}
                                {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                            </label>
                            <input
                                type="text"
                                name="street"
                                value={formData.street}
                                onChange={handleChange}
                                placeholder={t('Address (Street, Road, and Other Details)')}
                                required={formData.accountType === 'User'}
                            />
                        </div>

                        {/* District + Neighborhood */}
                        <div className="input-grid">
                            <div className="input-group">
                                <label>
                                    {t('District')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="district"
                                    value={formData.district}
                                    onChange={handleChange}
                                    placeholder={t('District')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                            <div className="input-group">
                                <label>
                                    {t('Neighborhood')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="neighborhood"
                                    value={formData.neighborhood}
                                    onChange={handleChange}
                                    placeholder={t('Neighborhood')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                        </div>

                        {/* Building Number + Floor */}
                        <div className="input-grid">
                            <div className="input-group">
                                <label>
                                    {t('Building Number')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="buildingNumber"
                                    value={formData.buildingNumber}
                                    onChange={handleChange}
                                    placeholder={t('Building Number')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                            <div className="input-group">
                                <label>
                                    {t('Floor')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="floor"
                                    value={formData.floor}
                                    onChange={handleChange}
                                    placeholder={t('Floor')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                        </div>

                        {/* Apartment No. + City */}
                        <div className="input-grid">
                            <div className="input-group">
                                <label>
                                    {t('Apartment No.')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="apartmentNo"
                                    value={formData.apartmentNo}
                                    onChange={handleChange}
                                    placeholder={t('Apartment No.')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                            <div className="input-group">
                                <label>
                                    {t('City')}
                                    {formData.accountType === 'User' && <span style={{ color: '#ef4444', marginInlineStart: '4px' }}>*</span>}
                                </label>
                                <input
                                    type="text"
                                    name="city"
                                    value={formData.city}
                                    onChange={handleChange}
                                    onBlur={(e) => setFormData(prev => ({ ...prev, city: e.target.value.trim() }))}
                                    placeholder={t('City')}
                                    required={formData.accountType === 'User'}
                                />
                            </div>
                        </div>

                        {/* Postal Code + Country */}
                        <div className="input-grid">
                            <div className="input-group">
                                <label>{t('Postal Code')}</label>
                                <input
                                    type="text"
                                    name="postalCode"
                                    value={formData.postalCode}
                                    onChange={handleChange}
                                    placeholder={t('Postal Code')}
                                />
                            </div>
                            <div className="input-group">
                                <label>{t('Country')}</label>
                                <input
                                    type="text"
                                    name="country"
                                    value={formData.country}
                                    onChange={handleChange}
                                    placeholder={t('Country')}
                                />
                            </div>
                        </div>
                    </div>

                    {/* Select Location on Map */}
                    <div className="form-section">
                        <div className="section-header">
                            <h3>{t('Select Location')}</h3>
                            <span className="location-coords">
                                {formData.latitude.toFixed(4)}, {formData.longitude.toFixed(4)}
                            </span>
                        </div>
                        <p className="section-hint">{t('Click on the map to set your pin location')}</p>

                        <div className="map-container">
                            <MapContainer
                                center={[formData.latitude, formData.longitude]}
                                zoom={15}
                                style={{ height: '300px', width: '100%', borderRadius: 'var(--radius-lg)' }}
                            >
                                <TileLayer
                                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                                />
                                <Marker position={[formData.latitude, formData.longitude]} />
                                <MapEvents onMapClick={handleMapClick} />
                            </MapContainer>
                        </div>
                    </div>

                    {/* Submit Button */}
                    <button type="submit" className="primary" disabled={saving} style={{ padding: '16px', fontSize: '16px' }}>
                        <span className="material-symbols-outlined">check</span>
                        <span>{saving ? t('Saving...') : t('Save Changes')}</span>
                    </button>
                </form>
            </main>

            {/* Custom Failure Popup Modal (Matches Admin Delete Modal Style) */}
            {errorModal.open && (
                <div className="admin-modal-overlay" onClick={() => setErrorModal({ open: false, title: '', message: '' })}>
                    <div className="admin-modal-card" onClick={e => e.stopPropagation()}>
                        <div className="admin-modal-header">
                            <div className="admin-modal-icon-danger">
                                <span className="material-symbols-outlined">warning</span>
                            </div>
                            <h3 className="admin-modal-title">{errorModal.title}</h3>
                        </div>
                        <p className="admin-modal-message" style={{ whiteSpace: 'pre-line' }}>{errorModal.message}</p>
                        <div className="admin-modal-actions">
                            <button 
                                type="button"
                                className="admin-modal-confirm-btn"
                                style={{ background: 'var(--primary)', color: 'var(--on-primary, #ffffff)', boxShadow: '0 4px 14px rgba(63, 81, 181, 0.3)' }}
                                onClick={() => setErrorModal({ open: false, title: '', message: '' })}
                            >
                                {t('OK')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
