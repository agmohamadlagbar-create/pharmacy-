import React, { useState } from 'react'
import { useTranslation } from 'react-i18next'
import './NavBar.css'

/**
 * NavBar — Shared top navigation bar for the LagbarShop application.
 * 
 * Props:
 * - loggedIn: boolean
 * - isGuest: boolean
 * - accountType: string ('User' | 'Shop' | 'Delivery' | 'Admin')
 * - cartCount: number
 * - theme: string ('dark' | 'light')
 * - toggleTheme: function
 * - userLocation: { country, city }
 * - onNavigateHome: function
 * - onNavigateCart: function
 * - onNavigateFavorites: function
 * - onNavigateProfile: function
 * - onNavigateNotifications: function
 * - onNavigateSearch: function(query)
 * - onToggleSidebar: function
 */
export default function NavBar({
  loggedIn,
  isGuest,
  accountType,
  cartCount = 0,
  theme,
  toggleTheme,
  userLocation,
  onNavigateHome,
  onNavigateCart,
  onNavigateFavorites,
  onNavigateProfile,
  onNavigateEditProfile,
  onNavigateNotifications,
  onNavigateSettings,
  onNavigateSearch,
  onToggleSidebar,
  onNavigateStoreAnalytics,
  onNavigateAdmin,
  onNavigateDeliveryJobs,
  onNavigateStoreProducts,
  onNavigateStoreOrders,
  onGuestSignIn
}) {
  const { t, i18n } = useTranslation()
  const [searchQuery, setSearchQuery] = useState('')
  const isRTL = i18n.language?.startsWith('ar')

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    if (searchQuery.trim() && onNavigateSearch) {
      onNavigateSearch(searchQuery.trim())
    }
  }

  const handleSearchKeyDown = (e) => {
    if (e.key === 'Enter') {
      handleSearchSubmit(e)
    }
  }

  const handleLocationClick = (e) => {
    e.stopPropagation()
    if (onNavigateEditProfile) {
      onNavigateEditProfile()
    } else if (onNavigateProfile) {
      onNavigateProfile()
    }
  }

  return (
    <>
      <nav className="navbar" dir={isRTL ? 'rtl' : 'ltr'}>
        <div className="navbar-inner">
          {/* Brand Logo & Location */}
          <div className="navbar-brand">
            <span className="navbar-logo" onClick={onNavigateHome} style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
              <img src="/lagbarshop-icon.svg" alt="LagbarShop Logo" style={{ width: '28px', height: '28px', objectFit: 'contain' }} />
              LagbarShop
            </span>
            {/* Location Indicator - Click to change location */}
            {userLocation?.city && (
              <div 
                className="navbar-location" 
                onClick={handleLocationClick}
                title={t('Click to change your location in Edit Profile')}
              >
                <span className="material-symbols-outlined">location_on</span>
                <span>{userLocation.city}{userLocation.country ? `, ${userLocation.country}` : ''}</span>
              </div>
            )}
          </div>

          {/* Search Bar */}
          <form className="navbar-search" onSubmit={handleSearchSubmit}>
            <span className="navbar-search-icon material-symbols-outlined">search</span>
            <input
              type="text"
              placeholder={t('Search for products, brands or stores...')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={handleSearchKeyDown}
            />
          </form>

          {/* Desktop Nav Links */}
          <div className="navbar-links">
            <button onClick={onNavigateHome}>{t('Home')}</button>
          </div>

          {/* Action Buttons */}
          <div className="navbar-actions">
            {/* Theme Toggle */}
            <button
              className="navbar-theme-toggle"
              onClick={toggleTheme}
              title={theme === 'dark' ? t('Switch to Light Mode') : t('Switch to Dark Mode')}
            >
              <span className="material-symbols-outlined">
                {theme === 'dark' ? 'light_mode' : 'dark_mode'}
              </span>
            </button>

            {/* Settings or My Products */}
            {accountType === 'Shop' ? (
              <button
                className="navbar-action-btn"
                onClick={onNavigateStoreProducts}
                title={t('My Products')}
              >
                <span className="material-symbols-outlined">inventory_2</span>
              </button>
            ) : (
              <button
                className="navbar-action-btn"
                onClick={onNavigateSettings}
                title={t('Settings')}
              >
                <span className="material-symbols-outlined">settings</span>
              </button>
            )}

            {/* Favorites, Store Analytics, Admin Panel, or Delivery Jobs */}
            {accountType === 'Shop' ? (
              <button
                className="navbar-action-btn"
                onClick={onNavigateStoreAnalytics}
                title={t('Store Analytics')}
              >
                <span className="material-symbols-outlined">leaderboard</span>
              </button>
            ) : accountType === 'Admin' ? (
              <button
                className="navbar-action-btn"
                onClick={onNavigateAdmin}
                title={t('Admin Panel')}
              >
                <span className="material-symbols-outlined">shield</span>
              </button>
            ) : accountType === 'Delivery' ? (
              <button
                className="navbar-action-btn"
                onClick={onNavigateDeliveryJobs}
                title={t('Delivery Jobs')}
              >
                <span className="material-symbols-outlined">local_shipping</span>
              </button>
            ) : (
              <button
                className="navbar-action-btn hide-mobile"
                onClick={onNavigateFavorites}
                title={t('Favorites')}
              >
                <span className="material-symbols-outlined">favorite</span>
              </button>
            )}

            {/* Notifications or Store Orders */}
            {accountType === 'Shop' ? (
              <button
                className="navbar-action-btn"
                onClick={onNavigateStoreOrders}
                title={t('Store Orders')}
              >
                <span className="material-symbols-outlined">receipt_long</span>
              </button>
            ) : (loggedIn && !isGuest && (
              <button
                className="navbar-action-btn hide-mobile"
                onClick={onNavigateNotifications}
                title={t('Notifications')}
              >
                <span className="material-symbols-outlined">notifications</span>
              </button>
            ))}

            {/* Cart */}
            {(!accountType || accountType === 'User') && (
              <button
                className="navbar-action-btn"
                onClick={onNavigateCart}
                title={t('Cart')}
              >
                <span className="material-symbols-outlined">shopping_cart</span>
                {cartCount > 0 && (
                  <span className="nav-badge">{cartCount > 9 ? '9+' : cartCount}</span>
                )}
              </button>
            )}

            {/* Profile Avatar */}
            {loggedIn && !isGuest ? (
              <div
                className="navbar-avatar"
                onClick={onNavigateProfile}
                title={t('Profile')}
              >
                <span className="material-symbols-outlined" style={{ fontSize: 20 }}>person</span>
              </div>
            ) : (
              <button
                className="navbar-action-btn"
                onClick={onGuestSignIn || onNavigateProfile}
                title={t('Sign In')}
              >
                <span className="material-symbols-outlined">login</span>
              </button>
            )}

            {/* Hamburger (Mobile) */}
            <button className="navbar-hamburger" onClick={onToggleSidebar}>
              <span className="material-symbols-outlined">menu</span>
            </button>
          </div>
        </div>
      </nav>
      <div className="navbar-spacer" />
    </>
  )
}
