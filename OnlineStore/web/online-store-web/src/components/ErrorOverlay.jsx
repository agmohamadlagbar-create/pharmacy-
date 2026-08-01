import React from 'react';
import { useError } from '../context/ErrorContext';

const ErrorOverlay = () => {
    const { banner, setBanner, toast, setToast, modal, setModal } = useError();

    if (!banner && !toast && !modal) return null;

    return (
        <>
            {/* 1. Banner Error */}
            {banner && (
                <div className="error-banner">
                    <div className="error-banner-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                    </div>
                    <div className="error-banner-content">
                        <div className="error-banner-title">{banner.title}</div>
                        <div className="error-banner-msg">{banner.message}</div>
                    </div>
                    <button className="error-banner-close" onClick={() => setBanner(null)}>
                        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                    </button>
                </div>
            )}

            {/* 2. Toast Notification */}
            {toast && (() => {
                const isSuccess = toast.type === 'success' || (toast.type !== 'error' && (toast.message?.includes('success') || toast.message?.includes('✅') || toast.message?.includes('updated') || toast.message?.includes('Profile updated')));
                return (
                    <div className="error-toast-container">
                        <div className={`error-toast ${isSuccess ? 'success' : ''}`}>
                            <div className="error-toast-icon">
                                {isSuccess ? (
                                    <span className="material-symbols-outlined" style={{ fontSize: '20px', color: '#22c55e' }}>check_circle</span>
                                ) : (
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                                )}
                            </div>
                            <span style={{ fontWeight: isSuccess ? '600' : '500' }}>{toast.message}</span>
                            {toast.actionText && (
                                <a href="#" className="error-toast-action" onClick={(e) => {
                                    e.preventDefault();
                                    if (toast.onAction) toast.onAction();
                                    setToast(null);
                                }}>
                                    {toast.actionText}
                                </a>
                            )}
                        </div>
                    </div>
                );
            })()}

            {/* 5. Error Modal / Dialog */}
            {modal && (
                <div className="error-modal-overlay" onClick={() => setModal(null)}>
                    <div className="error-modal" onClick={e => e.stopPropagation()}>
                        <div className="error-modal-header">
                            <div className="error-toast-icon">
                                <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>
                            </div>
                            <h2 className="error-modal-title">{modal.title}</h2>
                        </div>
                        <div className="error-modal-body">
                            <p>{modal.message}</p>
                            {modal.resource && <div className="error-modal-code">{modal.resource}</div>}
                        </div>
                        <div className="error-modal-actions">
                            <button className="glass-btn" onClick={() => setModal(null)}>Cancel</button>
                            <button className="admin-btn-danger" onClick={() => {
                                if (modal.onAction) modal.onAction();
                                setModal(null);
                            }}>
                                {modal.actionText || 'Confirm'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
};

export default ErrorOverlay;
