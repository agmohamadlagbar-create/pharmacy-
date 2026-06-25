import React, { useState, useEffect } from 'react';
import { api } from '../apiClient';
import { formatPrice } from '../utils/formatPrice.js';
import { loadStripe } from '@stripe/stripe-js';
import { Elements } from '@stripe/react-stripe-js';
import StripeAddCard from '../components/StripeAddCard';
import { useTranslation } from 'react-i18next';
import './PaymentPage.css';

let stripePromise = null;

export default function PaymentPage({ cart, checkoutData, onBack, onSuccess, storeUserId }) {
    const { t } = useTranslation();
    const [isProcessing, setIsProcessing] = useState(false);
    const [savedCards, setSavedCards] = useState([]);
    const [selectedCardId, setSelectedCardId] = useState(null);
    const [isAddingCard, setIsAddingCard] = useState(false);
    const [stripeLoaded, setStripeLoaded] = useState(false);

    useEffect(() => {
        if (!checkoutData) return;
        if (checkoutData.paymentMethod === 'Credit Card') {
            loadConfigAndCards();
        }
    }, [checkoutData]);

    async function loadConfigAndCards() {
        try {
            if (!stripePromise) {
                const { data: config } = await api.get('/api/payment/config');
                stripePromise = loadStripe(config.publishableKey);
            }
            setStripeLoaded(true);

            const { data } = await api.get('/api/payment/cards');
            setSavedCards(data);
            if (data.length > 0) {
                setSelectedCardId(data[0].id);
            } else {
                setIsAddingCard(true);
            }
        } catch (err) {
            console.error(err);
        }
    }

    if (!checkoutData) {
        onBack();
        return null;
    }

    async function handleConfirmPayment() {
        if (checkoutData.paymentMethod === 'Credit Card' && !selectedCardId) {
            alert(t('Please select a payment card.'));
            return;
        }

        setIsProcessing(true);

        try {
            await api.post('/api/orders/checkout-cart', {
                deliveryAddress: checkoutData.addressString,
                deliveryCity: checkoutData.addressCity || null,
                paymentMethod: checkoutData.paymentMethod,
                offerCode: checkoutData.offerCode,
                paymentToken: selectedCardId
            });

            setTimeout(() => {
                setIsProcessing(false);
                onSuccess();
            }, 1000);

        } catch (err) {
            console.error(err);
            alert(t("Payment failed: ") + (err.response?.data?.error || err.message));
            setIsProcessing(false);
        }
    }

    async function handleDeleteCard(id) {
        try {
            await api.delete(`/api/payment/cards/${id}`);
            const updatedCards = savedCards.filter(c => c.id !== id);
            setSavedCards(updatedCards);
            if (selectedCardId === id) {
                setSelectedCardId(updatedCards.length > 0 ? updatedCards[0].id : null);
                if (updatedCards.length === 0) setIsAddingCard(true);
            }
        } catch (err) {
            alert(t('Could not delete card'));
        }
    }

    const formattedAmount = formatPrice(cart.totalAmount, cart.items[0]?.currencyCode || 'USD');

    return (
        <div className="payment-page-container fade-in">
            <header className="payment-appbar">
                <button className="icon-btn-round" onClick={onBack} disabled={isProcessing}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h2>{t('Payment details')}</h2>
            </header>

            <main className="payment-body">
                <div className="payment-box">
                    <div className="payment-order-top">
                        <div className="order-total-label">{t('ORDER TOTAL')}</div>
                        <div className="order-total-value">{formattedAmount}</div>
                    </div>

                    <div className="payment-method-row">
                        <span className="row-label">{t('Method')}</span>
                        <div className="method-chip">
                            <span className="chip-icon">💳</span>
                            <span className="chip-text">{checkoutData.paymentMethod}</span>
                        </div>
                    </div>
                    
                    <div className="payment-deliver-row">
                        <span className="row-label">{t('Deliver to')}</span>
                        <div className="deliver-address">
                            <span className="loc-icon">📍</span>
                            <span className="address-text">{checkoutData.addressString}</span>
                        </div>
                    </div>

                    <div className="divider-line"></div>

                    {checkoutData.paymentMethod === 'Credit Card' ? (
                        <div className="cards-section">
                            {isAddingCard ? (
                                <>
                                    <h3 className="section-title">{t('Add a new card')}</h3>
                                    {stripeLoaded ? (
                                        <Elements stripe={stripePromise}>
                                            <StripeAddCard 
                                                onSuccess={(newCard) => {
                                                    setSavedCards([...savedCards, newCard]);
                                                    setSelectedCardId(newCard.id);
                                                    setIsAddingCard(false);
                                                }}
                                                onCancel={() => {
                                                    if (savedCards.length > 0) setIsAddingCard(false);
                                                    else onBack();
                                                }}
                                            />
                                        </Elements>
                                    ) : (
                                        <div className="spinner-small" style={{ margin: '20px auto' }}></div>
                                    )}
                                </>
                            ) : (
                                <>
                                    <div className="saved-cards-header">
                                        <h3 className="section-title">{t('Saved Cards')}</h3>
                                        <button className="btn-text" onClick={() => setIsAddingCard(true)}>{t('+ Add new')}</button>
                                    </div>
                                    <div className="cards-list">
                                        {savedCards.map(c => (
                                            <div 
                                                key={c.id} 
                                                className={`card-item ${selectedCardId === c.id ? 'selected' : ''}`}
                                                onClick={() => setSelectedCardId(c.id)}
                                            >
                                                <div className="card-item-left">
                                                    <span className="card-icon">💳</span>
                                                    <div className="card-item-info">
                                                        <div className="c-brand">{t('{{brand}} ending in {{last4}}', { brand: c.brand.toUpperCase(), last4: c.last4 })}</div>
                                                        <div className="c-exp">{t('Expires {{month}}/{{year}}', { month: c.expMonth, year: c.expYear })}</div>
                                                    </div>
                                                </div>
                                                <button className="btn-icon-delete" onClick={(e) => { e.stopPropagation(); handleDeleteCard(c.id); }}>
                                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 6h18"></path><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                                                </button>
                                            </div>
                                        ))}
                                    </div>
                                </>
                            )}
                        </div>
                    ) : (
                        <div className="cod-message">
                            <span style={{fontSize: '48px'}}>🚚</span>
                            <p>{t('You have selected Cash on Delivery. You will pay when the order arrives.')}</p>
                        </div>
                    )}

                    {!isAddingCard && (
                        <div className="payment-footer">
                           <div className="divider-line"></div>
                           <button 
                                className="btn-pay-now"
                                onClick={handleConfirmPayment}
                                disabled={isProcessing || (checkoutData.paymentMethod === 'Credit Card' && !selectedCardId)}
                           >
                               {isProcessing ? <div className="spinner-small" style={{borderColor: 'var(--text-primary)', borderTopColor: 'transparent'}}></div> : (
                                   <>
                                        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{marginRight: '8px'}}><line x1="5" y1="12" x2="19" y2="12"></line><polyline points="12 5 19 12 12 19"></polyline></svg>
                                        <span>{checkoutData.paymentMethod === 'Credit Card' ? t('Pay now - {{amount}}', { amount: formattedAmount }) : t('Confirm Order')}</span>
                                   </>
                               )}
                           </button>
                           <div className="ssl-secured">
                               <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
                               <span>{t('SSL encrypted - Secured by Lagbar')}</span>
                           </div>
                        </div>
                    )}
                </div>
            </main>
        </div>
    );
}
