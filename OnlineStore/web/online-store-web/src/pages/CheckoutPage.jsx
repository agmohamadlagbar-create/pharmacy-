import React, { useState, useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient'
import { formatPrice } from '../utils/formatPrice.js'
import './CheckoutPage.css'

export default function CheckoutPage({ cart, onBack, onProceed, userLocation, onChangeLocation, onNavigateEditProfile }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n.language === 'ar'
    const [addresses, setAddresses] = useState([])
    const [loading, setLoading] = useState(true)

    const [selectedAddressId, setSelectedAddressId] = useState('')
    const [paymentMethod, setPaymentMethod] = useState('Cash on Delivery')
    const [offerCode, setOfferCode] = useState('')

    const [isAddingAddress, setIsAddingAddress] = useState(false)
    const [editingAddressId, setEditingAddressId] = useState(null)
    const [newAddress, setNewAddress] = useState({
        title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true
    })

    const [locationWarning, setLocationWarning] = useState(null)
    const lastSafeAddressIdRef = useRef('')

    useEffect(() => {
        if (!selectedAddressId || !addresses.length) return

        const addr = addresses.find(a => a.id.toString() === selectedAddressId)
        if (!addr) return

        const addressCity = (addr.city || '').trim()
        const profileCity = (userLocation?.city || '').trim()

        if (addressCity && profileCity && addressCity.toLowerCase() !== profileCity.toLowerCase()) {
            setLocationWarning({ addressCity, profileCity })
        } else {
            lastSafeAddressIdRef.current = selectedAddressId
        }
    }, [selectedAddressId, addresses, userLocation?.city])

    const handleCancelLocationUpdate = () => {
        setSelectedAddressId(lastSafeAddressIdRef.current)
        setLocationWarning(null)
    }

    const handleConfirmLocationUpdate = () => {
        if (onChangeLocation && locationWarning) {
            onChangeLocation(locationWarning.addressCity, false)
        }
        setLocationWarning(null)
    }

    useEffect(() => {
        fetchAddresses()
    }, [])

    async function fetchAddresses() {
        try {
            setLoading(true)
            const { data } = await api.get('/api/me/addresses')
            setAddresses(data)
            if (data.length > 0) {
                const def = data.find(a => a.isDefault) || data[0]
                setSelectedAddressId(def.id.toString())
            }
        } catch (err) {
            console.error('Failed to load addresses', err)
        } finally {
            setLoading(false)
        }
    }

    async function handleSaveAddress(e) {
        e.preventDefault()
        try {
            if (!newAddress.street || !newAddress.city) {
                alert(t('Street and City are required.'))
                return
            }
            const trimmedAddress = {
                ...newAddress,
                city: newAddress.city.trim(),
                street: newAddress.street.trim(),
                title: newAddress.title.trim()
            }
            if (editingAddressId) {
                const { data } = await api.put(`/api/me/addresses/${editingAddressId}`, trimmedAddress)
                setAddresses(addresses.map(a => a.id === editingAddressId ? data : a))
                if (selectedAddressId === editingAddressId.toString()) {
                    setSelectedAddressId(data.id.toString())
                }
            } else {
                const { data } = await api.post('/api/me/addresses', trimmedAddress)
                setAddresses([...addresses, data])
                setSelectedAddressId(data.id.toString())
            }
            setIsAddingAddress(false)
            setEditingAddressId(null)
            setNewAddress({ title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true })
        } catch (err) {
            alert(t('Failed to save address'))
        }
    }

    async function handleDeleteAddress(id) {
        if (!window.confirm(t('Are you sure you want to delete this address?'))) return
        try {
            await api.delete(`/api/me/addresses/${id}`)
            const updated = addresses.filter(a => a.id !== id)
            setAddresses(updated)
            if (selectedAddressId === id.toString()) {
                const def = updated.find(a => a.isDefault) || updated[0]
                setSelectedAddressId(def ? def.id.toString() : '')
            }
        } catch (err) {
            alert(t('Failed to delete address'))
        }
    }

    function handleEditClick(addr) {
        if (onNavigateEditProfile) {
            onNavigateEditProfile()
            return
        }
        setEditingAddressId(addr.id)
        setNewAddress({
            title: addr.title || '',
            street: addr.street || '',
            buildingNumber: addr.buildingNumber || '',
            city: addr.city || '',
            postalCode: addr.postalCode || '',
            isDefault: addr.isDefault
        })
        setIsAddingAddress(true)
    }

    function handleAddNewClick() {
        if (onNavigateEditProfile) {
            onNavigateEditProfile()
            return
        }
        setEditingAddressId(null)
        setNewAddress({ title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true })
        setIsAddingAddress(true)
    }

    function handleProceed() {
        if (!selectedAddressId) {
            alert(t('Please select or add a delivery address to confirm your order.'))
            return
        }

        const selectedAddr = addresses.find(a => a.id.toString() === selectedAddressId)
        if (!selectedAddr) return

        const addressCity = (selectedAddr.city || '').trim()
        const addressParts = [
            selectedAddr.title ? `${t('Title')}: ${selectedAddr.title}` : null,
            selectedAddr.street ? `${t('Address')}: ${selectedAddr.street}` : null,
            selectedAddr.district ? `${t('District')}: ${selectedAddr.district}` : null,
            selectedAddr.neighborhood ? `${t('Neighborhood')}: ${selectedAddr.neighborhood}` : null,
            selectedAddr.buildingNumber ? `${t('Bldg')}: ${selectedAddr.buildingNumber}` : null,
            selectedAddr.floor ? `${t('Floor')}: ${selectedAddr.floor}` : null,
            selectedAddr.apartmentNo ? `${t('Apt')}: ${selectedAddr.apartmentNo}` : null,
            selectedAddr.city ? `${t('City')}: ${selectedAddr.city}` : null,
            selectedAddr.postalCode ? `${t('Postal Code')}: ${selectedAddr.postalCode}` : null,
        ].filter(Boolean)

        const addressString = addressParts.join(' | ')

        onProceed({
            addressString,
            addressCity,
            paymentMethod,
            offerCode
        })
    }

    if (loading) return (
        <div className="checkout-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            <header className="checkout-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Checkout')}</h1>
            </header>
            <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
                <div className="spinner" style={{ width: 40, height: 40, border: '4px solid var(--outline-variant)', borderTop: '4px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
            </div>
        </div>
    )

    return (
        <div className="checkout-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Header */}
            <header className="checkout-header">
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('Checkout')}</h1>
            </header>

            <main className="checkout-content">
                {/* Checkout Stepper Bar */}
                <div className="checkout-stepper-container">
                    <div className="checkout-stepper">
                        <div className="stepper-line-bg" />
                        <div className="stepper-line-progress" style={{ width: '50%' }} />

                        {/* Step 1: Cart */}
                        <div className="step-point completed">
                            <div className="step-circle">
                                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>check</span>
                            </div>
                            <span className="step-label">{t('Cart')}</span>
                        </div>

                        {/* Step 2: Address */}
                        <div className="step-point active">
                            <div className="step-circle">
                                <span className="material-symbols-outlined" style={{ fontSize: 20 }}>location_on</span>
                            </div>
                            <span className="step-label">{t('Address')}</span>
                        </div>

                        {/* Step 3: Payment */}
                        <div className="step-point">
                            <div className="step-circle">
                                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>payments</span>
                            </div>
                            <span className="step-label">{t('Payment')}</span>
                        </div>
                    </div>
                </div>

                <div className="checkout-grid">
                    {/* Left Column */}
                    <div className="checkout-left">
                        {/* Section 1: Delivery Address */}
                        <section style={{ marginBottom: '32px' }}>
                            <div className="step-header">
                                <span className="material-symbols-outlined" style={{ color: 'var(--primary)', fontSize: 28 }}>home_pin</span>
                                <h2 className="step-title">{t('Delivery Address')}</h2>
                            </div>

                            <div className="card" style={{ padding: '20px' }}>
                                {addresses.length === 0 && !isAddingAddress && (
                                    <div className="dashed-area">
                                        <span className="material-symbols-outlined" style={{ fontSize: 44, color: 'var(--primary)' }}>add_location_alt</span>
                                        <p style={{ color: 'var(--on-surface-variant)' }}>{t('You have no saved addresses')}</p>
                                        <button className="btn-premium secondary" onClick={handleAddNewClick}>
                                            <span className="material-symbols-outlined">add</span>
                                            <span>{t('Add delivery address')}</span>
                                        </button>
                                    </div>
                                )}

                                {addresses.length > 0 && !isAddingAddress && (
                                    <div className="address-list">
                                        {addresses.map(addr => {
                                            const isSelected = selectedAddressId === addr.id.toString()
                                            return (
                                                <div 
                                                    key={addr.id} 
                                                    className={`address-item ${isSelected ? 'active' : ''}`}
                                                    onClick={() => setSelectedAddressId(addr.id.toString())}
                                                >
                                                    <div className="address-details">
                                                        <div className="address-line-top">
                                                            <strong>{addr.title || t('Address')}</strong>
                                                            {addr.isDefault && <span className="default-pill">{t('Default')}</span>}
                                                            {isSelected && (
                                                                <span className="material-symbols-outlined" style={{ color: 'var(--primary)', fontVariationSettings: "'FILL' 1", fontSize: 20 }}>
                                                                    check_circle
                                                                </span>
                                                            )}
                                                        </div>
                                                        <p className="address-text">{addr.street}</p>
                                                        {(addr.district || addr.neighborhood) && (
                                                            <p className="address-text-sub">
                                                                {[addr.district ? `${t('District')}: ${addr.district}` : null, addr.neighborhood ? `${t('Neighborhood')}: ${addr.neighborhood}` : null].filter(Boolean).join(', ')}
                                                            </p>
                                                        )}
                                                        {(addr.buildingNumber || addr.floor || addr.apartmentNo) && (
                                                            <p className="address-text-sub">
                                                                {[addr.buildingNumber ? `${t('Bldg')}: ${addr.buildingNumber}` : null, addr.floor ? `${t('Floor')}: ${addr.floor}` : null, addr.apartmentNo ? `${t('Apt')}: ${addr.apartmentNo}` : null].filter(Boolean).join(' • ')}
                                                            </p>
                                                        )}
                                                        <p className="address-text-sub">{addr.city}{addr.postalCode ? `, ${addr.postalCode}` : ''}</p>
                                                    </div>

                                                    <div className="address-actions">
                                                        <button 
                                                            type="button" 
                                                            className="icon-btn" 
                                                            style={{ width: 34, height: 34 }} 
                                                            onClick={(e) => { e.stopPropagation(); handleEditClick(addr) }}
                                                            title={t('Edit')}
                                                        >
                                                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>edit</span>
                                                        </button>
                                                        <button 
                                                            type="button" 
                                                            className="icon-btn" 
                                                            style={{ width: 34, height: 34, color: 'var(--error)' }} 
                                                            onClick={(e) => { e.stopPropagation(); handleDeleteAddress(addr.id) }}
                                                            title={t('Delete')}
                                                        >
                                                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>delete</span>
                                                        </button>
                                                    </div>
                                                </div>
                                            )
                                        })}

                                        <button className="add-link-btn" onClick={handleAddNewClick}>
                                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>add</span>
                                            <span>{t('Add a new address')}</span>
                                        </button>
                                    </div>
                                )}

                                {isAddingAddress && (
                                    <form className="form" onSubmit={handleSaveAddress}>
                                        <div className="field">
                                            <label>{t('Label')}</label>
                                            <input type="text" placeholder={t("e.g., Home, Office")} value={newAddress.title} onChange={e => setNewAddress({ ...newAddress, title: e.target.value })} />
                                        </div>
                                        <div className="field">
                                            <label>{t('Street Name *')}</label>
                                            <input type="text" value={newAddress.street} onChange={e => setNewAddress({ ...newAddress, street: e.target.value })} required />
                                        </div>
                                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                                            <div className="field">
                                                <label>{t('City *')}</label>
                                                <input
                                                    type="text"
                                                    value={newAddress.city}
                                                    onChange={e => setNewAddress({ ...newAddress, city: e.target.value })}
                                                    onBlur={e => setNewAddress({ ...newAddress, city: e.target.value.trim() })}
                                                    required
                                                />
                                            </div>
                                            <div className="field">
                                                <label>{t('Building / Apt')}</label>
                                                <input type="text" value={newAddress.buildingNumber} onChange={e => setNewAddress({ ...newAddress, buildingNumber: e.target.value })} />
                                            </div>
                                        </div>
                                        <div className="action-btn-group">
                                            <button type="button" className="btn-premium secondary" onClick={() => { setIsAddingAddress(false); setEditingAddressId(null); }}>
                                                {t('Cancel')}
                                            </button>
                                            <button type="submit" className="primary" style={{ width: 'auto', padding: '12px 24px' }}>
                                                {editingAddressId ? t('Update Address') : t('Save Address')}
                                            </button>
                                        </div>
                                    </form>
                                )}
                            </div>
                        </section>

                        {/* Section 2: Payment Method */}
                        <section style={{ marginBottom: '32px' }}>
                            <div className="step-header">
                                <span className="material-symbols-outlined" style={{ color: 'var(--primary)', fontSize: 28 }}>payments</span>
                                <h2 className="step-title">{t('Payment Method')}</h2>
                            </div>

                            <div className="card" style={{ padding: '20px' }}>
                                <div className="payment-grid">
                                    <label className={`method-card ${paymentMethod === 'Cash on Delivery' ? 'active' : ''}`}>
                                        <input type="radio" value="Cash on Delivery" checked={paymentMethod === 'Cash on Delivery'} onChange={e => setPaymentMethod(e.target.value)} />
                                        <div className="method-icon-bg">
                                            <span className="material-symbols-outlined">payments</span>
                                        </div>
                                        <span className="method-label">{t('Cash on Delivery')}</span>
                                    </label>
                                    <label className={`method-card ${paymentMethod === 'Credit Card' ? 'active' : ''}`}>
                                        <input type="radio" value="Credit Card" checked={paymentMethod === 'Credit Card'} onChange={e => setPaymentMethod(e.target.value)} />
                                        <div className="method-icon-bg">
                                            <span className="material-symbols-outlined">credit_card</span>
                                        </div>
                                        <span className="method-label">{t('Credit / Debit Card')}</span>
                                    </label>
                                </div>
                            </div>
                        </section>

                        {/* Section 3: Offers & Promos */}
                        <section>
                            <div className="step-header">
                                <span className="material-symbols-outlined" style={{ color: 'var(--primary)', fontSize: 28 }}>local_offer</span>
                                <h2 className="step-title">{t('Offers & Promos')}</h2>
                            </div>

                            <div className="card" style={{ padding: '12px' }}>
                                <div className="offer-input-wrapper">
                                    <input
                                        type="text"
                                        className="offer-input"
                                        placeholder={t("Enter offer code")}
                                        value={offerCode}
                                        onChange={e => setOfferCode(e.target.value)}
                                    />
                                    <button className="offer-apply-btn">{t('Apply')}</button>
                                </div>
                            </div>
                        </section>
                    </div>

                    {/* Right Column: Summary */}
                    <div className="checkout-right">
                        <div className="summary-card">
                            <h2 style={{ marginBottom: '24px', fontSize: '20px', fontWeight: 700 }}>{t('Order Summary')}</h2>
                            <div className="summary-list">
                                <div className="summary-row">
                                    <span>{t('Items ({{count}})', { count: cart.items.length })}</span>
                                    <span>{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                                </div>
                                <div className="summary-row">
                                    <span>{t('Delivery')}</span>
                                    <span className="status-badge success" style={{ padding: '2px 8px', fontSize: '12px' }}>{t('Free')}</span>
                                </div>
                            </div>

                            <div className="summary-divider" style={{ margin: '20px 0' }} />

                            <div className="summary-row total-row">
                                <span>{t('Total')}</span>
                                <span className="total-amount">{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                            </div>

                            <button
                                className="primary"
                                onClick={handleProceed}
                                disabled={!selectedAddressId}
                                style={{ marginTop: '24px', padding: '16px', fontSize: '16px' }}
                            >
                                <span>{t('Proceed to Payment')}</span>
                                <span className="material-symbols-outlined">{isRTL ? 'arrow_back' : 'arrow_forward'}</span>
                            </button>

                            {!selectedAddressId && (
                                <div className="warning-msg">
                                    <span className="material-symbols-outlined" style={{ fontSize: 16 }}>warning</span>
                                    <span>{t('A delivery address is required')}</span>
                                </div>
                            )}

                            <div className="security-note">
                                <span className="material-symbols-outlined" style={{ fontSize: 14, color: 'var(--secondary)' }}>verified_user</span>
                                <span>{t('Secured checkout · SSL encrypted')}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </main>

            {/* City Mismatch Warning Modal */}
            {locationWarning && (
                <div className="error-modal-overlay">
                    <div className="error-modal" style={{ textAlign: 'center' }}>
                        <div style={{ width: 64, height: 64, borderRadius: '50%', background: 'var(--err-bg)', border: '1px solid var(--err-border)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px', color: 'var(--err-icon)' }}>
                            <span className="material-symbols-outlined" style={{ fontSize: 32 }}>location_off</span>
                        </div>
                        <h3 className="error-modal-title" style={{ marginBottom: '12px' }}>{t('Location Update')}</h3>
                        <p className="error-modal-body" dangerouslySetInnerHTML={{__html: t('Your selected delivery address is in {{addressCity}}. Your active location will be updated from {{profileCity}} to {{addressCity}}.', { addressCity: `<strong>${locationWarning.addressCity}</strong>`, profileCity: `<strong>${locationWarning.profileCity}</strong>` })}} />
                        <div className="error-modal-actions" style={{ justifyContent: 'center', gap: '12px', marginTop: '24px' }}>
                            <button className="btn-premium secondary" onClick={handleCancelLocationUpdate}>
                                {t('Cancel')}
                            </button>
                            <button className="primary" style={{ width: 'auto', padding: '10px 24px' }} onClick={handleConfirmLocationUpdate}>
                                {t('Confirm')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
