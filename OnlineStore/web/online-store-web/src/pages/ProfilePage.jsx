import { useState, useEffect } from 'react'
import { api } from '../apiClient.js'
import ProfileRippleAvatar from '../components/ProfileRippleAvatar.jsx'
import { useTranslation } from 'react-i18next'
import './ProfilePage.css'

export default function ProfilePage({ onBack, onEdit, onNavigateSettings, onLogout, onNavigateStore }) {
    const { t, i18n } = useTranslation()
    const isRTL = i18n?.language === 'ar'
    const [profile, setProfile] = useState(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        fetchProfile()
    }, [])

    async function fetchProfile() {
        try {
            const res = await api.get('/api/me')
            setProfile(res.data)
        } catch (err) {
            console.error('Failed to fetch profile', err)
        } finally {
            setLoading(false)
        }
    }

    if (loading) return (
        <div className="profile-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            <header className="profile-header">
                <button className="back-btn-circular" onClick={onBack}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('My Profile')}</h1>
                <div style={{ width: 44 }} />
            </header>
            <div style={{ display: 'flex', justifyContent: 'center', padding: '80px 0' }}>
                <div className="spinner" style={{ width: 40, height: 40, border: '4px solid var(--outline-variant)', borderTop: '4px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
            </div>
        </div>
    )

    const displayName = profile?.shopName || profile?.fullName || t('User')
    const avatarUrl = profile?.profilePictureUrl ? `${api.defaults.baseURL}${profile.profilePictureUrl}` : null
    const roleLabel = profile?.accountType === 'Admin' ? t('Admin') 
        : profile?.accountType === 'Shop' ? t('Store Owner') 
        : profile?.accountType === 'Delivery' ? t('Delivery Rider') 
        : t('Customer')

    return (
        <div className="profile-page page-enter" dir={isRTL ? 'rtl' : 'ltr'}>
            {/* Header */}
            <header className="profile-header">
                <button className="back-btn-circular" onClick={onBack} title={t('Back')}>
                    <span className="material-symbols-outlined">{isRTL ? 'arrow_forward' : 'arrow_back'}</span>
                </button>
                <h1>{t('My Profile')}</h1>
                <button className="back-btn-circular" onClick={onNavigateSettings} title={t('Settings')}>
                    <span className="material-symbols-outlined">settings</span>
                </button>
            </header>

            <main className="profile-content">
                <div className="profile-main-container">
                    {/* Hero Profile Section */}
                    <div className="card profile-hero-card">
                        <div className="profile-avatar-wrap">
                            <ProfileRippleAvatar src={avatarUrl} size={130} />
                        </div>

                        <div className="profile-hero-text">
                            <div style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: '8px', flexWrap: 'wrap' }}>
                                <h2 className="profile-name" style={{ margin: 0 }}>{displayName}</h2>
                                <span className="role-badge merchant" style={{ fontSize: '11px', display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                                    <span className="material-symbols-outlined" style={{ fontSize: 14 }}>verified</span>
                                    {roleLabel}
                                </span>
                            </div>
                            <p className="profile-email" style={{ marginTop: '6px' }}>{profile?.email}</p>
                        </div>

                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', width: '100%', marginTop: '20px' }}>
                            <button className="primary" onClick={onEdit}>
                                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>edit</span>
                                <span>{t('Edit Profile')}</span>
                            </button>
                            <button className="btn-premium secondary" onClick={onNavigateSettings}>
                                <span className="material-symbols-outlined" style={{ fontSize: 18 }}>settings</span>
                                <span>{t('Settings')}</span>
                            </button>
                        </div>

                        {profile?.accountType === 'Shop' && (
                            <button
                                className="primary"
                                style={{
                                    width: '100%',
                                    marginTop: '12px',
                                    padding: '14px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    gap: '10px',
                                    background: 'linear-gradient(135deg, var(--primary), var(--secondary))',
                                    color: 'var(--on-primary)',
                                    borderRadius: 'var(--radius-lg)',
                                    fontWeight: 700,
                                    fontSize: '14px',
                                    cursor: 'pointer',
                                    border: 'none'
                                }}
                                onClick={() => {
                                    if (onNavigateStore && profile?.id) {
                                        onNavigateStore(profile.id)
                                    }
                                }}
                            >
                                <span className="material-symbols-outlined" style={{ fontSize: 20 }}>storefront</span>
                                <span>{t('View My Store Page')}</span>
                            </button>
                        )}
                    </div>

                    {/* Account Information Card */}
                    <div className="card profile-details-card">
                        <h3 className="section-subtitle">{t('Personal Information')}</h3>

                        <div className="info-grid">
                            <InfoItem icon="mail" label={t('Email')} value={profile?.email} />
                            {profile?.accountType !== 'Shop' && (
                                <InfoItem icon="person" label={t('Full Name')} value={profile?.fullName || t('Not provided')} />
                            )}
                            {profile?.accountType === 'Shop' && (
                                <InfoItem icon="storefront" label={t('Shop Name')} value={profile?.shopName || t('Not provided')} />
                            )}
                        </div>

                        <div className="section-divider" />

                        <h3 className="section-subtitle">{t('Default Location & Address')}</h3>

                        <div className="info-grid">
                            <InfoItem icon="location_city" label={t('City')} value={profile?.city || t('Not provided')} />
                            <InfoItem icon="streetview" label={t('Street')} value={profile?.street || t('Not provided')} />
                            <InfoItem icon="home" label={t('Building')} value={profile?.buildingNumber || t('Not provided')} />
                            <InfoItem icon="markunread_mailbox" label={t('Postal Code')} value={profile?.postalCode || t('Not provided')} />

                            {(profile?.latitude && profile?.longitude) && (
                                <InfoItem
                                    icon="my_location"
                                    label={t('GPS Coordinates')}
                                    value={`${profile.latitude.toFixed(4)}, ${profile.longitude.toFixed(4)}`}
                                />
                            )}
                        </div>
                    </div>

                    {/* Log Out Action */}
                    {onLogout && (
                        <button 
                            onClick={onLogout} 
                            style={{
                                width: '100%',
                                padding: '16px',
                                borderRadius: 'var(--radius-xl)',
                                background: 'var(--err-bg)',
                                border: '1px solid var(--err-border)',
                                color: 'var(--error)',
                                fontWeight: 700,
                                fontSize: '15px',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                gap: '8px',
                                cursor: 'pointer',
                                transition: 'all 0.2s ease'
                            }}
                        >
                            <span className="material-symbols-outlined" style={{ fontSize: 20 }}>logout</span>
                            <span>{t('Log Out')}</span>
                        </button>
                    )}
                </div>
            </main>
        </div>
    )
}

function InfoItem({ icon, label, value }) {
    return (
        <div className="info-item-row">
            <div className="info-icon-box">
                <span className="material-symbols-outlined" style={{ fontSize: 20, color: 'var(--primary)' }}>{icon}</span>
            </div>
            <div className="info-item-content">
                <span className="info-item-label">{label}</span>
                <span className="info-item-value">{value}</span>
            </div>
        </div>
    )
}
