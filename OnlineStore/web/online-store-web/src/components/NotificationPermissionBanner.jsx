import React, { useState, useEffect } from 'react';
import { requestNotificationPermissionAndGetToken } from '../utils/firebase';
import { useTranslation } from 'react-i18next';

export default function NotificationPermissionBanner() {
  const { t } = useTranslation();
  const [showBanner, setShowBanner] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // Check if browser supports Notification and permission is default
    const authToken = localStorage.getItem('token');
    const dismissed = localStorage.getItem('notification_banner_dismissed');

    if (authToken && 'Notification' in window && Notification.permission === 'default' && !dismissed) {
      setShowBanner(true);
    }
  }, []);

  const handleEnable = async () => {
    setLoading(true);
    try {
      const token = await requestNotificationPermissionAndGetToken();
      if (token) {
        setShowBanner(false);
      }
    } finally {
      setLoading(false);
      setShowBanner(false);
    }
  };

  const handleDismiss = () => {
    localStorage.setItem('notification_banner_dismissed', 'true');
    setShowBanner(false);
  };

  if (!showBanner) return null;

  return (
    <div style={styles.banner}>
      <div style={styles.content}>
        <div style={styles.iconContainer}>
          🔔
        </div>
        <div style={styles.textContainer}>
          <h4 style={styles.title}>
            {t('enable_notifications_title', 'Stay updated on your orders!')}
          </h4>
          <p style={styles.subtitle}>
            {t('enable_notifications_desc', 'Get instant background updates when order status changes or delivery arrives, even when tab is closed.')}
          </p>
        </div>
      </div>
      <div style={styles.actions}>
        <button
          onClick={handleEnable}
          disabled={loading}
          style={styles.enableButton}
        >
          {loading ? t('enabling', 'Enabling...') : t('enable_notifications', 'Enable Notifications')}
        </button>
        <button
          onClick={handleDismiss}
          style={styles.dismissButton}
        >
          {t('maybe_later', 'Maybe later')}
        </button>
      </div>
    </div>
  );
}

const styles = {
  banner: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexWrap: 'wrap',
    gap: '12px',
    backgroundColor: '#1E293B',
    color: '#F8FAFC',
    padding: '12px 20px',
    borderRadius: '12px',
    margin: '12px auto',
    maxWidth: '1200px',
    boxShadow: '0 10px 15px -3px rgba(0,0,0,0.3)',
    border: '1px solid rgba(255,255,255,0.1)'
  },
  content: {
    display: 'flex',
    alignItems: 'center',
    gap: '14px',
    flex: '1 1 300px'
  },
  iconContainer: {
    fontSize: '1.8rem',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'rgba(59, 130, 246, 0.2)',
    borderRadius: '50%',
    width: '44px',
    height: '44px'
  },
  textContainer: {
    display: 'flex',
    flexDirection: 'column'
  },
  title: {
    margin: 0,
    fontSize: '0.95rem',
    fontWeight: '600',
    color: '#F8FAFC'
  },
  subtitle: {
    margin: '2px 0 0 0',
    fontSize: '0.82rem',
    color: '#94A3B8'
  },
  actions: {
    display: 'flex',
    alignItems: 'center',
    gap: '10px'
  },
  enableButton: {
    backgroundColor: '#3B82F6',
    color: '#FFFFFF',
    border: 'none',
    borderRadius: '8px',
    padding: '8px 16px',
    fontWeight: '600',
    fontSize: '0.85rem',
    cursor: 'pointer',
    transition: 'background-color 0.2s'
  },
  dismissButton: {
    backgroundColor: 'transparent',
    color: '#94A3B8',
    border: '1px solid #475569',
    borderRadius: '8px',
    padding: '8px 14px',
    fontSize: '0.85rem',
    cursor: 'pointer'
  }
};
