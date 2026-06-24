import { useState, useEffect } from 'react'
import { api } from '../apiClient.js'
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { useTranslation } from 'react-i18next'
import './EditProfilePage.css'

// Fix for default marker icon in Leaflet
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
    iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
    shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

const BackIcon = () => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
const CheckIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>

function MapEvents({ onMapClick }) {
    useMapEvents({
        click: (e) => {
            onMapClick(e.latlng.lat, e.latlng.lng)
        },
    })
    return null
}

export default function EditProfilePage({ onBack, onSave }) {
    const { t } = useTranslation()
    const [formData, setFormData] = useState({
        fullName: '',
        country: '',
        street: '',
        buildingNumber: '',
        city: '',
        postalCode: '',
        latitude: 41.0082,
        longitude: 28.9784,
        description: '',
        openingTime: '',
        closingTime: '',
        accountType: ''
    })
    const [loading, setLoading] = useState(true)
    const [saving, setSaving] = useState(false)

    useEffect(() => {
        fetchProfile()
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

        // Reverse geocoding using Nominatim
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

        const trimmedData = {
            ...formData,
            city: formData.city.trim(),
            fullName: formData.fullName.trim(),
            country: formData.country.trim(),
            street: formData.street.trim(),
            postalCode: formData.postalCode.trim()
        }

        try {
            const res = await api.put('/api/me/profile', trimmedData)
            if (res.status === 200) {
                onSave()
            } else {
                alert(t('Failed to save profile'))
            }
        } catch (err) {
            console.error('Save error', err)
            alert(t('Error saving profile'))
        } finally {
            setSaving(false)
        }
    }

    if (loading) return <div className="edit-loading">{t('Loading...')}</div>

    return (
        <div className="edit-profile-page">
            <header className="edit-header">
                <button className="icon-btn" onClick={onBack}>
                    <BackIcon />
                </button>
                <h1>{t('Edit Profile')}</h1>
            </header>

            <main className="edit-content">
                <form className="edit-form" onSubmit={handleSubmit}>
                    <div className="form-section">
                        <h3>{t('Personal Info')}</h3>
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

                    {formData.accountType === 'Shop' && (
                        <div className="form-section">
                            <h3>{t('Store Information')}</h3>
                            <div className="input-group">
                                <label>{t('Description')}</label>
                                <textarea
                                    name="description"
                                    value={formData.description}
                                    onChange={handleChange}
                                    placeholder={t('Tell customers about your store...')}
                                    rows="3"
                                    style={{ width: '100%', padding: '12px', background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: '8px', color: 'var(--text-primary)', fontSize: '15px' }}
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

                    <div className="form-section">
                        <h3>{t('Address Details')}</h3>
                        <div className="input-grid">
                            <div className="input-group">
                                <label>{t('City')}</label>
                                <input
                                    type="text"
                                    name="city"
                                    value={formData.city}
                                    onChange={handleChange}
                                    onBlur={(e) => setFormData(prev => ({ ...prev, city: e.target.value.trim() }))}
                                />
                            </div>
                            <div className="input-group">
                                <label>{t('Postal Code')}</label>
                                <input
                                    type="text"
                                    name="postalCode"
                                    value={formData.postalCode}
                                    onChange={handleChange}
                                />
                            </div>
                        </div>
                        <div className="input-group">
                            <label>{t('Street')}</label>
                            <input
                                type="text"
                                name="street"
                                value={formData.street}
                                onChange={handleChange}
                            />
                        </div>
                        <div className="input-group">
                            <label>{t('Building Number')}</label>
                            <input
                                type="text"
                                name="buildingNumber"
                                value={formData.buildingNumber}
                                onChange={handleChange}
                            />
                        </div>
                    </div>

                    <div className="form-section">
                        <div className="section-header">
                            <h3>{t('Select Location')}</h3>
                            <span className="location-coords">
                                {formData.latitude.toFixed(4)}, {formData.longitude.toFixed(4)}
                            </span>
                        </div>
                        <p className="section-hint">{t('Click on the map to set your address')}</p>

                        <div className="map-container">
                            <MapContainer
                                center={[formData.latitude, formData.longitude]}
                                zoom={15}
                                style={{ height: '300px', width: '100%', borderRadius: '12px' }}
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

                    <button type="submit" className="save-btn" disabled={saving}>
                        {saving ? t('Saving...') : <><CheckIcon /> {t('Save Changes')}</>}
                    </button>
                </form>
            </main>
        </div>
    )
}
