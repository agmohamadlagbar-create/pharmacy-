import React, { useCallback, useState } from 'react';
import { api } from '../apiClient.js';
import useInfiniteScroll from '../hooks/useInfiniteScroll.js';
import Skeleton from '../components/Skeleton.jsx';
import { useTranslation } from 'react-i18next';
import '../pages/CategoryProductsPage.css';

export default function MyShopsPage({ onBack, onNavigateStore, onAddToCart, onNavigateCart }) {
    const { t } = useTranslation();
    const [isReordering, setIsReordering] = useState(null);
    const [expandedOrderId, setExpandedOrderId] = useState(null);
    
    const fetchOrders = useCallback(async (page, limit) => {
        const { data } = await api.get(`/api/orders?status=Delivered&page=${page}&limit=${limit}`);
        return data;
    }, []);

    const { items: orders, isLoading, hasMore, error, sentinelRef, initialLoad } = useInfiniteScroll(fetchOrders, {
        limit: 9
    });

    const statusColor = (s) => ({ Pending: '#f59e0b', Preparing: '#3b82f6', ReadyForPickup: '#22c55e', PickedUp: '#8b5cf6', OnTheWay: '#06b6d4', Delivered: '#22c55e', Cancelled: '#ef4444' }[s] || '#888')

    return (
        <div className="category-products-container">
            <header className="category-page-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                <h1>{t('Delivered Orders')}</h1>
                <div style={{ width: 40 }} />
            </header>

            <main className="products-grid" style={{ padding: '0 16px 20px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                {error && <div style={{ color: 'red', textAlign: 'center' }}>{error}</div>}
                {initialLoad ? (
                     Array.from({ length: 4 }).map((_, i) => (
                          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: '16px', background: 'var(--card)', padding: '16px', borderRadius: '12px' }}>
                              <Skeleton width="60px" height="60px" borderRadius="30px" />
                              <div style={{ flex: 1 }}>
                                  <Skeleton width="60%" height="20px" style={{ marginBottom: '8px' }} />
                                  <Skeleton width="40%" height="16px" />
                              </div>
                          </div>
                     ))
                ) : (
                    (() => {
                        const groups = [];
                        orders.forEach(o => {
                            const gid = o.orderGroupId || `S-${o.id}`;
                            let g = groups.find(x => x.id === gid);
                            if (!g) {
                                g = { id: gid, date: o.createdAtUtc, subOrders: [], total: 0, status: o.status };
                                groups.push(g);
                            }
                            g.subOrders.push(o);
                            g.total += o.totalAmount;
                        });

                        return groups.map(g => (
                            <div 
                                key={g.id} 
                                onClick={() => setExpandedOrderId(expandedOrderId === g.id ? null : g.id)}
                                style={{ background: 'var(--card)', cursor: 'pointer', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)', transition: 'background 0.2s' }}
                            >
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                    <div>
                                        <strong style={{ color: 'var(--text-primary)', display: 'block' }}>{t('Order #{{id}}', { id: g.id })}</strong>
                                        <span style={{ fontSize: '12px', color: 'var(--muted)' }}>
                                            {t('{{count}} stores involved', { count: g.subOrders.length })}
                                        </span>
                                        <div style={{ fontSize: '11px', color: 'var(--muted)', marginTop: '4px' }}>{new Date(g.date).toLocaleDateString()}</div>
                                    </div>
                                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '8px' }}>
                                        <span style={{ fontSize: '12px', fontWeight: '600', color: statusColor(g.status), background: `${statusColor(g.status)}15`, padding: '4px 10px', borderRadius: '12px' }}>{g.status}</span>
                                        <span style={{ fontSize: '14px', fontWeight: 'bold', color: 'var(--text-primary)' }}>${g.total.toFixed(2)}</span>
                                    </div>
                                </div>
                                
                                {expandedOrderId === g.id && (
                                    <div style={{ marginTop: '16px', paddingTop: '16px', borderTop: '1px solid var(--border)' }} onClick={(e) => e.stopPropagation()}>
                                        {g.subOrders.map(so => (
                                            <div key={so.id} style={{ marginBottom: '16px', padding: '12px', borderRadius: '8px', background: 'rgba(255,255,255,0.03)' }}>
                                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                                                    <span style={{ color: 'var(--primary)', fontSize: '13px', fontWeight: 'bold' }} onClick={() => onNavigateStore && onNavigateStore(so.storeUserId)}>
                                                        {so.storeName || t('Unknown Store')}
                                                    </span>
                                                    <span style={{ fontSize: '11px', color: 'var(--muted)' }}>{t('Part #{{id}}', { id: so.id })}</span>
                                                </div>
                                                {so.items.map(item => (
                                                    <div key={item.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px', color: 'var(--muted)', marginBottom: '4px' }}>
                                                        <span>{item.productName} × {item.quantity}</span>
                                                        <span>${(item.unitPrice * item.quantity).toFixed(2)}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        ))}
                                        
                                        <div style={{ marginTop: '12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: '600', color: 'var(--text-primary)', fontSize: '14px' }}>
                                            <span>{t('Total Paid')}</span>
                                            <span>${g.total.toFixed(2)}</span>
                                        </div>
                                        
                                        <button
                                            className="primary"
                                            disabled={isReordering === g.id}
                                            style={{ padding: '10px', width: '100%', marginTop: '16px', fontSize: '14px', borderRadius: '8px' }}
                                            onClick={async (e) => {
                                                e.stopPropagation();
                                                if (isReordering) return;
                                                setIsReordering(g.id);
                                                for(const so of g.subOrders) {
                                                    for(const item of so.items) {
                                                        if (onAddToCart) await onAddToCart(item.productId, item.quantity);
                                                    }
                                                }
                                                setIsReordering(null);
                                                if (onNavigateCart) onNavigateCart();
                                            }}
                                        >
                                            {isReordering === g.id ? t('Adding to Cart...') : t('Order All Again')}
                                        </button>
                                    </div>
                                )}
                            </div>
                        ));
                    })()
                )}
                {!initialLoad && orders.length === 0 && !error && (
                    <div style={{ textAlign: 'center', padding: '40px 20px', color: 'var(--muted)' }}>
                        <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" style={{ marginBottom: '16px', opacity: 0.5 }}>
                            <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path>
                        </svg>
                        <h3>{t('No Delivered Orders')}</h3>
                        <p>{t("You haven't received any orders yet.")}</p>
                    </div>
                )}
                
                {hasMore && !initialLoad && (
                    <div ref={sentinelRef} style={{ padding: '20px', textAlign: 'center' }}>
                        {isLoading && <div className="spinner" style={{ borderColor: 'var(--primary)', borderTopColor: 'transparent', width: 24, height: 24, borderWidth: 2 }} />}
                    </div>
                )}
            </main>
        </div>
    );
}
