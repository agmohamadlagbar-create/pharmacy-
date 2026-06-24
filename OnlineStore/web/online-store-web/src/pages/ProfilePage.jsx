import { useState, useEffect } from 'react'
import { api } from '../apiClient.js'
import ProfileRippleAvatar from '../components/ProfileRippleAvatar.jsx'
import { useTranslation } from 'react-i18next'
import './ProfilePage.css'

const UserIcon = () => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
const MailIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>
const MapPinIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
const HomeIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
const BackIcon = () => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>

export default function ProfilePage({ onBack, onEdit }) {
    const { t } = useTranslation()
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

    if (loading) return <div className="profile-loading">{t('Loading profile...')}</div>

    const displayName = profile?.shopName || profile?.fullName || t('User')
    const avatarUrl = profile?.profilePictureUrl ? `${api.defaults.baseURL}${profile.profilePictureUrl}` : null

    return (
        <div className="profile-page">
            <header className="profile-header">
                <button className="icon-btn" onClick={onBack}>
                    <BackIcon />
                </button>
                <h1>{t('My Profile')}</h1>
            </header>

            <main className="profile-content">
                <div className="profile-card">
                    <div className="profile-avatar-section">
                        <ProfileRippleAvatar src={avatarUrl} size={180} />
                        <h2>{displayName}</h2>
                        <p className="profile-role" style={{ textTransform: 'uppercase' }}>
                            {profile?.accountType === 'Admin' ? t('Admin') 
                                : profile?.accountType === 'Shop' ? t('Store Owner') 
                                : profile?.accountType === 'Delivery' ? t('Delivery Rider') 
                                : t('Customer')}
                        </p>
                    </div>

                    <div className="profile-info-grid">
                        <InfoItem icon={<MailIcon />} label={t('Email')} value={profile?.email} />
                        {profile?.accountType !== 'Shop' && (
                            <InfoItem icon={<UserIcon />} label={t('Full Name')} value={profile?.fullName || t('Not provided')} />
                        )}
                        {profile?.accountType === 'Shop' && (
                            <InfoItem icon={<HomeIcon />} label={t('Shop Name')} value={profile?.shopName || t('Not provided')} />
                        )}

                        <div className="info-divider">{t('Address Information')}</div>

                        <InfoItem icon={<MapPinIcon />} label={t('City')} value={profile?.city || t('Not provided')} />
                        <InfoItem icon={<MapPinIcon />} label={t('Street')} value={profile?.street || t('Not provided')} />
                        <InfoItem icon={<MapPinIcon />} label={t('Building')} value={profile?.buildingNumber || t('Not provided')} />
                        <InfoItem icon={<MapPinIcon />} label={t('Postal Code')} value={profile?.postalCode || t('Not provided')} />

                        {(profile?.latitude && profile?.longitude) && (
                            <InfoItem
                                icon={<MapPinIcon />}
                                label={t('Location')}
                                value={`${profile.latitude.toFixed(4)}, ${profile.longitude.toFixed(4)}`}
                            />
                        )}
                    </div>

                    <button className="edit-profile-btn" onClick={onEdit}>
                        {t('Edit Profile')}
                    </button>
                </div>
            </main>
        </div>
    )
}

function InfoItem({ icon, label, value }) {
    return (
        <div className="info-item">
            <div className="info-icon">{icon}</div>
            <div className="info-text">
                <label className="info-label">{label}</label>
                <span className="info-value">{value}</span>
            </div>
        </div>
    )
}
