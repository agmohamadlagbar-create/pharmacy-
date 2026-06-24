import React, { createContext, useContext, useState, useCallback } from 'react';

const ErrorContext = createContext();

export const useError = () => {
    const context = useContext(ErrorContext);
    if (!context) {
        throw new Error('useError must be used within an ErrorProvider');
    }
    return context;
};

// Simple event-like system for axios to call
let globalErrorCallback = null;
export const triggerGlobalError = (type, data) => {
    if (globalErrorCallback) {
        globalErrorCallback(type, data);
    }
};

export const ErrorProvider = ({ children }) => {
    const [banner, setBanner] = useState(null);
    const [toast, setToast] = useState(null);
    const [modal, setModal] = useState(null);

    const showBanner = useCallback((title, message) => {
        setBanner({ title, message });
    }, []);

    const showToast = useCallback((message, actionText, onAction) => {
        setToast({ message, actionText, onAction });
        setTimeout(() => setToast(null), 4000); // Auto-hide
    }, []);

    const showModal = useCallback((title, message, resource, actionText, onAction) => {
        setModal({ title, message, resource, actionText, onAction });
    }, []);

    const clearAll = useCallback(() => {
        setBanner(null);
        setToast(null);
        setModal(null);
    }, []);

    // Register the global trigger
    globalErrorCallback = (type, data) => {
        if (type === 'banner') showBanner(data.title, data.message);
        if (type === 'toast') showToast(data.message, data.actionText, data.onAction);
        if (type === 'modal') showModal(data.title, data.message, data.resource, data.actionText, data.onAction);
        if (type === 'clear') clearAll();
    };

    return (
        <ErrorContext.Provider value={{ 
            banner, setBanner, 
            toast, setToast, 
            modal, setModal,
            showBanner, showToast, showModal, clearAll 
        }}>
            {children}
        </ErrorContext.Provider>
    );
};
