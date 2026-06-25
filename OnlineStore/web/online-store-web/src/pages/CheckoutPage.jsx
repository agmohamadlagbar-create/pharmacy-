import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { api } from '../apiClient';
import { formatPrice } from '../utils/formatPrice.js';
import './CheckoutPage.css';

export default function CheckoutPage({ cart, onBack, onProceed, userLocation, onChangeLocation }) {
    const { t } = useTranslation();
    const [addresses, setAddresses] = useState([]);
    const [loading, setLoading] = useState(true);

    const [selectedAddressId, setSelectedAddressId] = useState('');
    const [paymentMethod, setPaymentMethod] = useState('Cash on Delivery');
    const [offerCode, setOfferCode] = useState('');

    const [isAddingAddress, setIsAddingAddress] = useState(false);
    const [editingAddressId, setEditingAddressId] = useState(null);
    const [newAddress, setNewAddress] = useState({
        title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true
    });

    const [locationWarning, setLocationWarning] = useState(null);
    const lastSafeAddressIdRef = useRef('');

    // Auto-sync location when an address is selected
    useEffect(() => {
        if (!selectedAddressId || !addresses.length) return;

        const addr = addresses.find(a => a.id.toString() === selectedAddressId);
        if (!addr) return;

        const addressCity = (addr.city || '').trim();
        const profileCity = (userLocation?.city || '').trim();

        if (addressCity && profileCity && addressCity.toLowerCase() !== profileCity.toLowerCase()) {
            setLocationWarning({ addressCity, profileCity });
        } else {
            lastSafeAddressIdRef.current = selectedAddressId;
        }
    }, [selectedAddressId, addresses, userLocation?.city]);

    const handleCancelLocationUpdate = () => {
        setSelectedAddressId(lastSafeAddressIdRef.current);
        setLocationWarning(null);
    };

    const handleConfirmLocationUpdate = () => {
        if (onChangeLocation && locationWarning) {
            // Update active location behind the scenes without redirecting
            onChangeLocation(locationWarning.addressCity, false);
        }
        setLocationWarning(null);
    };

    useEffect(() => {
        fetchAddresses();
    }, []);

    async function fetchAddresses() {
        try {
            setLoading(true);
            const { data } = await api.get('/api/me/addresses');
            setAddresses(data);
            if (data.length > 0) {
                const def = data.find(a => a.isDefault) || data[0];
                setSelectedAddressId(def.id.toString());
            }
        } catch (err) {
            console.error('Failed to load addresses', err);
        } finally {
            setLoading(false);
        }
    }

    async function handleSaveAddress(e) {
        e.preventDefault();
        try {
            if (!newAddress.street || !newAddress.city) {
                alert(t("Street and City are required."));
                return;
            }
            const trimmedAddress = {
                ...newAddress,
                city: newAddress.city.trim(),
                street: newAddress.street.trim(),
                title: newAddress.title.trim()
            };
            if (editingAddressId) {
                const { data } = await api.put(`/api/me/addresses/${editingAddressId}`, trimmedAddress);
                setAddresses(addresses.map(a => a.id === editingAddressId ? data : a));
                if (selectedAddressId === editingAddressId.toString()) {
                    setSelectedAddressId(data.id.toString());
                }
            } else {
                const { data } = await api.post('/api/me/addresses', trimmedAddress);
                setAddresses([...addresses, data]);
                setSelectedAddressId(data.id.toString());
            }
            setIsAddingAddress(false);
            setEditingAddressId(null);
            setNewAddress({ title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true });
        } catch (err) {
            alert(t('Failed to save address'));
        }
    }

    async function handleDeleteAddress(id) {
        if (!window.confirm(t('Are you sure you want to delete this address?'))) return;
        try {
            await api.delete(`/api/me/addresses/${id}`);
            const updated = addresses.filter(a => a.id !== id);
            setAddresses(updated);
            if (selectedAddressId === id.toString()) {
                const def = updated.find(a => a.isDefault) || updated[0];
                setSelectedAddressId(def ? def.id.toString() : '');
            }
        } catch (err) {
            alert(t('Failed to delete address'));
        }
    }

    function handleEditClick(addr) {
        setEditingAddressId(addr.id);
        setNewAddress({
            title: addr.title || '',
            street: addr.street || '',
            buildingNumber: addr.buildingNumber || '',
            city: addr.city || '',
            postalCode: addr.postalCode || '',
            isDefault: addr.isDefault
        });
        setIsAddingAddress(true);
    }

    function handleAddNewClick() {
        setEditingAddressId(null);
        setNewAddress({ title: '', street: '', buildingNumber: '', city: '', postalCode: '', isDefault: true });
        setIsAddingAddress(true);
    }

    function handleProceed() {
        if (!selectedAddressId) {
            alert(t('Please select or add a delivery address to confirm your order.'));
            return;
        }

        const selectedAddr = addresses.find(a => a.id.toString() === selectedAddressId);
        if (!selectedAddr) return;

        proceedWithOrder(selectedAddr);
    }

    function proceedWithOrder(selectedAddr) {
        const addressCity = (selectedAddr.city || '').trim();
        // Flatten address into a string format for the Order Request
        const addressString = `${selectedAddr.title}: ${selectedAddr.street}, ${selectedAddr.buildingNumber || ''}, ${selectedAddr.city} ${selectedAddr.postalCode || ''}`.trim();

        onProceed({
            addressString,
            addressCity,
            paymentMethod,
            offerCode
        });
    }



    if (loading) return <div className="checkout-page"><div className="loading-spinner"></div></div>;

    return (
        <div className="checkout-page fade-in">
            <header className="checkout-header">
                <button className="icon-btn back-btn" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{t('Checkout')}</h1>
            </header>

            <main className="checkout-content">
                <div className="checkout-grid">
                    <div className="checkout-left">
                        {/* STEP 1: ADDRESS */}
                        <div className="step-header">
                            <div className="step-number">1</div>
                            <h2 className="step-title">{t('Delivery address')}</h2>
                        </div>

                        <div className="premium-card">
                            {addresses.length === 0 && !isAddingAddress && (
                                <div className="dashed-area">
                                    <div style={{ marginBottom: '16px' }}>
                                        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="var(--primary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                            <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
                                            <circle cx="12" cy="10" r="3"></circle>
                                        </svg>
                                    </div>
                                    <p style={{ color: 'var(--text-secondary)', marginBottom: '16px' }}>{t('You have no saved addresses')}</p>
                                    <button className="btn-premium secondary" onClick={handleAddNewClick}>
                                        {t('+ Add delivery address')}
                                    </button>
                                </div>
                            )}

                            {addresses.length > 0 && !isAddingAddress && (
                                <div className="address-list">
                                    {addresses.map(addr => (
                                        <label key={addr.id} className={`address-item ${selectedAddressId === addr.id.toString() ? 'active' : ''}`}>
                                            <input
                                                type="radio"
                                                name="address"
                                                value={addr.id}
                                                checked={selectedAddressId === addr.id.toString()}
                                                onChange={(e) => setSelectedAddressId(e.target.value)}
                                            />
                                            <div className="address-details">
                                                <div className="address-line-top">
                                                    <strong>{addr.title}</strong>
                                                    {addr.isDefault && <span className="default-pill">{t('Default')}</span>}
                                                </div>
                                                <p className="address-text">{addr.street}, {addr.buildingNumber || ''}</p>
                                                <p className="address-text-sub">{addr.city}, {addr.postalCode || ''}</p>
                                            </div>
                                            <div className="check-mark" style={{ marginRight: '40px' }}>
                                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>
                                            </div>
                                            <div className="address-actions" style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', display: 'flex', flexDirection: 'column', gap: '8px', zIndex: 10 }}>
                                                <button type="button" className="icon-btn hover-bg" style={{ padding: '6px', borderRadius: '8px' }} onClick={(e) => { e.preventDefault(); e.stopPropagation(); handleEditClick(addr); }}>
                                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="var(--text-secondary)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                                                </button>
                                                <button type="button" className="icon-btn hover-bg" style={{ padding: '6px', borderRadius: '8px' }} onClick={(e) => { e.preventDefault(); e.stopPropagation(); handleDeleteAddress(addr.id); }}>
                                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#e74c3c" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                                                </button>
                                            </div>
                                        </label>
                                    ))}
                                    <button className="add-link-btn" onClick={handleAddNewClick}>
                                        {t('+ Add a new address')}
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
                                        <label>{t('Street Name')}</label>
                                        <input type="text" value={newAddress.street} onChange={e => setNewAddress({ ...newAddress, street: e.target.value })} required />
                                    </div>
                                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                                        <div className="field">
                                            <label>{t('City')}</label>
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
                                        <button type="button" className="btn-premium secondary" onClick={() => { setIsAddingAddress(false); setEditingAddressId(null); }}>{t('Cancel')}</button>
                                        <button type="submit" className="btn-premium primary">{editingAddressId ? t('Update Address') : t('Save Address')}</button>
                                    </div>
                                </form>
                            )}
                        </div>

                        {/* STEP 2: PAYMENT */}
                        <div className="step-header">
                            <div className="step-number">2</div>
                            <h2 className="step-title">{t('Payment method')}</h2>
                        </div>

                        <div className="premium-card">
                            <div className="payment-grid">
                                <label className={`method-card ${paymentMethod === 'Cash on Delivery' ? 'active' : ''}`}>
                                    <input type="radio" value="Cash on Delivery" checked={paymentMethod === 'Cash on Delivery'} onChange={e => setPaymentMethod(e.target.value)} />
                                    <div className="method-icon-bg">
                                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="6" width="20" height="12" rx="2"></rect><circle cx="12" cy="12" r="2"></circle><path d="M6 12h.01M18 12h.01"></path></svg>
                                    </div>
                                    <span className="method-label">{t('Cash on delivery')}</span>
                                </label>
                                <label className={`method-card ${paymentMethod === 'Credit Card' ? 'active' : ''}`}>
                                    <input type="radio" value="Credit Card" checked={paymentMethod === 'Credit Card'} onChange={e => setPaymentMethod(e.target.value)} />
                                    <div className="method-icon-bg">
                                        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect><line x1="1" y1="10" x2="23" y2="10"></line></svg>
                                    </div>
                                    <span className="method-label">{t('Credit / Debit card')}</span>
                                </label>
                            </div>
                        </div>

                        {/* STEP 3: OFFERS */}
                        <div className="step-header">
                            <div className="step-number">3</div>
                            <h2 className="step-title">{t('Offers & promos')}</h2>
                        </div>

                        <div className="premium-card" style={{ padding: '8px' }}>
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
                    </div>

                    <div className="checkout-right">
                        <div className="summary-card sticky">
                            <h2 style={{ marginBottom: '24px', fontSize: '20px' }}>{t('Order summary')}</h2>
                            <div className="summary-list">
                                <div className="summary-row">
                                    <span>{t('Items ({{count}})', { count: cart.items.length })}</span>
                                    <span>{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                                </div>
                                <div className="summary-row">
                                    <span>{t('Delivery')}</span>
                                    <span className="free-tag">{t('Free')}</span>
                                </div>
                            </div>

                            <div className="summary-row total">
                                <span>{t('Total')}</span>
                                <span>{formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD')}</span>
                            </div>

                            <button
                                className="btn-premium primary"
                                onClick={handleProceed}
                                disabled={!selectedAddressId}
                                style={{ marginTop: '24px' }}
                            >
                                <span>{t('Proceed to payment')}</span>
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                    <line x1="5" y1="12" x2="19" y2="12"></line>
                                    <polyline points="12 5 19 12 12 19"></polyline>
                                </svg>
                            </button>

                            {!selectedAddressId && (
                                <div className="warning-msg">
                                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line></svg>
                                    <span>{t('A delivery address is required')}</span>
                                </div>
                            )}

                            <div className="security-note">
                                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
                                <span>{t('Secured checkout · SSL encrypted')}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </main>


            {locationWarning && (
                <div className="city-mismatch-overlay">
                    <div className="city-mismatch-modal flex-col">
                        <div className="city-mismatch-icon" style={{ background: '#fdf2f2', color: '#e74c3c' }}>
                            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                                <line x1="12" y1="9" x2="12" y2="13"></line>
                                <line x1="12" y1="17" x2="12.01" y2="17"></line>
                            </svg>
                        </div>
                        <h3 className="city-mismatch-title">{t('Location Update')}</h3>
                        <p className="city-mismatch-text" dangerouslySetInnerHTML={{__html: t('Your selected delivery address is in {{addressCity}}. Your active location will be updated from {{profileCity}} to {{addressCity}}.', { addressCity: `<strong>${locationWarning.addressCity}</strong>`, profileCity: `<strong>${locationWarning.profileCity}</strong>` })}} />
                        <div className="city-mismatch-actions" style={{ marginTop: '24px' }}>
                            <button className="btn-premium secondary" onClick={handleCancelLocationUpdate}>
                                {t('Cancel')}
                            </button>
                            <button className="btn-premium primary" style={{ backgroundColor: '#E07A5F', borderColor: '#E07A5F', color: 'white' }} onClick={handleConfirmLocationUpdate}>
                                {t('Confirm')}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
