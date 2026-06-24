import React, { useState } from 'react';
import { useStripe, useElements, CardNumberElement, CardExpiryElement, CardCvcElement } from '@stripe/react-stripe-js';
import Spline from '@splinetool/react-spline';
import { api } from '../apiClient';

export default function StripeAddCard({ onSuccess, onCancel }) {
    const stripe = useStripe();
    const elements = useElements();
    const [isProcessing, setIsProcessing] = useState(false);
    const [error, setError] = useState(null);
    const [cardholderName, setCardholderName] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!stripe || !elements) return;

        setIsProcessing(true);
        setError(null);

        const cardNumberElement = elements.getElement(CardNumberElement);

        const { error, paymentMethod } = await stripe.createPaymentMethod({
            type: 'card',
            card: cardNumberElement,
            billing_details: {
                name: cardholderName,
            }
        });

        if (error) {
            setError(error.message);
            setIsProcessing(false);
        } else {
            try {
                const { data } = await api.post('/api/payment/cards', {
                    paymentMethodId: paymentMethod.id
                });
                setIsProcessing(false);
                onSuccess(data);
            } catch (err) {
                setError(err.response?.data?.error || err.message);
                setIsProcessing(false);
            }
        }
    };

    const elementOptions = {
        style: {
            base: {
                fontSize: '15px',
                color: '#ffffff',
                fontFamily: 'system-ui, sans-serif',
                '::placeholder': {
                    color: '#888888',
                },
            },
            invalid: { color: '#e74c3c' },
        },
    };

    return (
        <form onSubmit={handleSubmit} className="mock-card-form">
            <div className="spline-mockup-wrapper">
                <div className="spline-tint-overlay"></div>
                <Spline scene="https://prod.spline.design/Iv2mljRSq2vd7Z-T/scene.splinecode" />
            </div>

            <div className="form-group-custom">
                <label className="custom-label">CARD NUMBER</label>
                <div className="stripe-input-wrapper">
                    <CardNumberElement options={elementOptions} />
                </div>
            </div>

            <div className="form-group-custom">
                <label className="custom-label">EXPIRY</label>
                <div className="stripe-input-wrapper">
                    <CardExpiryElement options={elementOptions} />
                </div>
            </div>
            
            <div className="form-group-custom">
                <label className="custom-label">CVV</label>
                <div className="stripe-input-wrapper">
                    <CardCvcElement options={elementOptions} />
                </div>
            </div>

            <div className="form-group-custom">
                <label className="custom-label">CARDHOLDER NAME</label>
                <input 
                    type="text" 
                    className="custom-input" 
                    placeholder="Name on card" 
                    value={cardholderName}
                    onChange={(e) => setCardholderName(e.target.value)}
                />
            </div>

            {error && <div className="error-message">{error}</div>}
            
            <div className="form-actions-horizontal">
                <button type="button" className="btn-cancel" onClick={onCancel} disabled={isProcessing}>
                    Cancel
                </button>
                <button type="submit" className="btn-save" disabled={!stripe || isProcessing}>
                    {isProcessing ? <div className="spinner-small" style={{borderColor: 'var(--text-primary)', borderTopColor: 'transparent'}}></div> : 'Save card'}
                </button>
            </div>
        </form>
    );
}
