import { useState, useEffect, useRef, lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, useNavigate, useParams, useLocation, Navigate, Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import './i18n'
import CategoriesPage from './pages/CategoriesPage.jsx'
import Skeleton from './components/Skeleton.jsx'
import AnimationOverlay from './components/AnimationOverlay.jsx'
import RippleAvatar from './components/RippleAvatar.jsx'
import NavBar from './components/NavBar.jsx'
import addToCartAnim from './assets/lottie/add-to-cart.json'
import paymentSuccessAnim from './assets/lottie/payment-success.json'
import { api, setAuthToken } from './apiClient.js'
import { ErrorProvider, useError } from './context/ErrorContext'
import ErrorOverlay from './components/ErrorOverlay'
import AiChatWidget from './components/AiChatWidget.jsx'
import ShopAiChatWidget from './components/ShopAiChatWidget.jsx'
import AdminAiChatWidget from './components/AdminAiChatWidget.jsx'
import NotificationPermissionBanner from './components/NotificationPermissionBanner.jsx'
import { requestNotificationPermissionAndGetToken, unregisterTokenFromBackend } from './utils/firebase'

// Lazy-loaded pages for optimization
const AuthPage = lazy(() => import('./pages/AuthPage.jsx'))
const ProfilePage = lazy(() => import('./pages/ProfilePage.jsx'))
const EditProfilePage = lazy(() => import('./pages/EditProfilePage.jsx'))
const CategoryProductsPage = lazy(() => import('./pages/CategoryProductsPage.jsx'))
const StoreProductsPage = lazy(() => import('./pages/StoreProductsPage.jsx'))
const StoreOrdersPage = lazy(() => import('./pages/StoreOrdersPage.jsx'))
const StoreAnalyticsPage = lazy(() => import('./pages/StoreAnalyticsPage.jsx'))
const DeliveryJobsPage = lazy(() => import('./pages/DeliveryJobsPage.jsx'))
const AdminPage = lazy(() => import('./pages/AdminPage.jsx'))
const NotificationsPage = lazy(() => import('./pages/NotificationsPage.jsx'))
const ProductInfoPage = lazy(() => import('./pages/ProductInfoPage.jsx'))
const CartPage = lazy(() => import('./pages/CartPage.jsx'))
const PublicStorePage = lazy(() => import('./pages/PublicStorePage.jsx'))
const SettingsPage = lazy(() => import('./pages/SettingsPage.jsx'))
const CheckoutPage = lazy(() => import('./pages/CheckoutPage.jsx'))
const PaymentPage = lazy(() => import('./pages/PaymentPage.jsx'))
const FavoritesPage = lazy(() => import('./pages/FavoritesPage.jsx'))
const SearchProductsPage = lazy(() => import('./pages/SearchProductsPage.jsx'))
const MyShopsPage = lazy(() => import('./pages/MyShopsPage.jsx'))


// Helper Wrappers for Parameterized Routes
function CategoryProductsWrapper({ categoryProductsProps }) {
  const { id, categoryName } = useParams();
  const navigate = useNavigate();
  const [resolvedCategoryId, setResolvedCategoryId] = useState(null);
  const [resolvedCategoryName, setResolvedCategoryName] = useState(categoryName || '');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    async function resolveCategory() {
      // If we have an explicit numeric ID from `/category/:id`, we can use it directly
      if (id) {
        const parsedId = parseInt(id, 10);
        if (!isNaN(parsedId)) {
          try {
            setLoading(true);
            const { data } = await api.get(`/api/categories?t=${Date.now()}`);
            const matched = data.find(c => c.id === parsedId);
            if (matched) {
              setResolvedCategoryId(matched.id);
              setResolvedCategoryName(matched.name);
              setError(null);
            } else {
              setError('Category not found');
            }
          } catch (err) {
            console.error('Failed to load categories', err);
            setError('Failed to load category');
          } finally {
            setLoading(false);
          }
          return;
        }
      }

      // If we have a categoryName from `/:categoryName`
      if (categoryName) {
        try {
          setLoading(true);
          const { data } = await api.get(`/api/categories?t=${Date.now()}`);
          // Find category with matching name (case insensitive)
          const decodedName = decodeURIComponent(categoryName);
          const matched = data.find(c => c.name.toLowerCase() === decodedName.toLowerCase());
          if (matched) {
            setResolvedCategoryId(matched.id);
            setResolvedCategoryName(matched.name);
            setError(null);
          } else {
            setError('Category not found');
          }
        } catch (err) {
          console.error('Failed to resolve category name', err);
          setError('Failed to load category');
        } finally {
          setLoading(false);
        }
        return;
      }

      setLoading(false);
      setError('Invalid category parameters');
    }
    resolveCategory();
  }, [id, categoryName]);

  if (loading) {
    return (
      <div className="category-products-container" style={{ padding: '24px' }}>
        <header className="category-page-header" style={{ marginBottom: '24px' }}>
          <button className="back-btn-circular" onClick={() => navigate(-1)}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="15 18 9 12 15 6"></polyline>
            </svg>
          </button>
          <h1>{resolvedCategoryName || 'Loading...'}</h1>
        </header>
        <div className="products-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '24px' }}>
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="product-card-web skeleton-card" style={{ background: 'var(--card)', borderRadius: '24px', overflow: 'hidden', border: '1px solid var(--border)' }}>
              <div style={{ height: '240px', background: 'rgba(0,0,0,0.05)' }}>
                <Skeleton width="100%" height="100%" borderRadius="0" />
              </div>
              <div style={{ padding: '24px' }}>
                <Skeleton width="80px" height="20px" borderRadius="8px" style={{ marginBottom: '12px' }} />
                <Skeleton width="70%" height="22px" style={{ marginBottom: '6px' }} />
                <Skeleton width="90%" height="14px" style={{ marginBottom: '16px' }} />
                <Skeleton width="100%" height="44px" borderRadius="12px" />
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error || !resolvedCategoryId) {
    // If not a valid category name/ID, let it redirect to home
    return <Navigate to="/" replace />;
  }

  return (
    <CategoryProductsPage
      categoryId={resolvedCategoryId}
      categoryName={resolvedCategoryName}
      onBack={() => navigate(-1)}
      {...categoryProductsProps}
    />
  );
}

function ProductInfoWrapper({ productInfoProps }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const productId = parseInt(id, 10);
  return (
    <ProductInfoPage
      productId={productId}
      onBack={() => navigate(-1)}
      {...productInfoProps}
    />
  );
}

function PublicStoreWrapper({ publicStoreProps }) {
  const { storeId } = useParams();
  const navigate = useNavigate();
  return (
    <PublicStorePage
      storeUserId={storeId}
      onBack={() => navigate(-1)}
      {...publicStoreProps}
    />
  );
}

function SearchProductsWrapper({ searchProductsProps }) {
  const location = useLocation();
  const navigate = useNavigate();
  const queryParams = new URLSearchParams(location.search);
  const q = queryParams.get('q') || '';
  return (
    <SearchProductsPage
      initialQuery={q}
      onBack={() => navigate(-1)}
      {...searchProductsProps}
    />
  );
}

// Route Guard for Guests trying to access Authenticated User-only pages
function UserProtectedRoute({ loggedIn, children }) {
  const { showToast } = useError();
  if (!loggedIn) {
    // Show toast and redirect to main categories
    setTimeout(() => showToast('Please sign in to access this page'), 10);
    return <Navigate to="/" replace />;
  }
  return children;
}

// Role-based Route Guard
function RoleProtectedRoute({ loggedIn, accountType, requiredRole, children }) {
  const { showToast } = useError();
  if (!loggedIn) {
    return <Navigate to="/" replace />;
  }
  if (accountType !== requiredRole) {
    setTimeout(() => showToast(`Access denied: Requires ${requiredRole} role`), 10);
    return <Navigate to="/" replace />;
  }
  return children;
}

function RequireAuth({ loggedIn, isGuest }) {
  if (!loggedIn && !isGuest) {
    return <Navigate to="/login" replace />;
  }
  return <Outlet />;
}

function AppContent() {
  const { showToast } = useError()
  const navigate = useNavigate()
  const location = useLocation()

  const [loggedIn, setLoggedIn] = useState(() => {
    const token = localStorage.getItem('token')
    if (token) {
      setAuthToken(token)
      return true
    }
    return false
  })
  const [isGuest, setIsGuest] = useState(() => localStorage.getItem('isGuest') === 'true')
  const [accountType, setAccountType] = useState(() => localStorage.getItem('accountType'))
  const [currentUserId, setCurrentUserId] = useState(null)
  const [cart, setCart] = useState({ items: [], totalAmount: 0 })
  const [favorites, setFavorites] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem('favorites')) || []
    } catch {
      return []
    }
  })
  const [checkoutData, setCheckoutData] = useState(null)
  const [theme, setTheme] = useState(localStorage.getItem('theme') || 'light')
  const [activeAnimation, setActiveAnimation] = useState(null)
  const [userLocation, setUserLocation] = useState({ country: null, city: null })

  // Global Sidebar/Profile States
  const { t } = useTranslation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [profileData, setProfileData] = useState({ name: 'User', avatarUrl: null })
  const fileInputRef = useRef(null)

  useEffect(() => {
    fetchProfile()
  }, [loggedIn])

  async function fetchProfile() {
    if (!loggedIn) {
      setUserLocation({ country: null, city: null })
      setProfileData({ name: 'User', avatarUrl: null })
      return
    }
    try {
      const { data } = await api.get('/api/me')
      setUserLocation({ country: data.country, city: data.city })
      setCurrentUserId(data.id)
      setProfileData({
        name: data.fullName || data.shopName || data.email || 'User',
        avatarUrl: data.profilePictureUrl ? `${api.defaults.baseURL}${data.profilePictureUrl}` : null
      })
    } catch (err) {
      console.error('Failed to fetch profile', err)
    }
  }

  function handleAvatarClick() {
    if (fileInputRef.current) fileInputRef.current.click()
  }

  async function handleAvatarUpload(file) {
    const formData = new FormData()
    formData.append('file', file)
    try {
      const res = await api.post('/api/me/profile-picture', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      const data = res.data
      setProfileData(prev => ({
        ...prev,
        avatarUrl: `${api.defaults.baseURL}${data.profilePictureUrl}`
      }))
    } catch (err) {
      console.error('Upload error', err)
    }
  }

  useEffect(() => {
    document.documentElement.dataset.theme = theme
  }, [theme])

  useEffect(() => {
    localStorage.setItem('favorites', JSON.stringify(favorites))
  }, [favorites])

  function toggleTheme() {
    const nextTheme = theme === 'light' ? 'dark' : 'light'
    setTheme(nextTheme)
    localStorage.setItem('theme', nextTheme)
  }

  useEffect(() => {
    if (loggedIn && accountType === 'User') {
      fetchCart()
    } else {
      setCart({ items: [], totalAmount: 0 })
    }
  }, [loggedIn, accountType])

  async function fetchCart() {
    const currentAccountType = accountType || localStorage.getItem('accountType');
    console.log('[App] fetchCart called. AccountType:', currentAccountType);

    if (currentAccountType !== 'User') return
    try {
      const { data } = await api.get(`/api/cart?t=${Date.now()}`)
      console.log('[App] fetchCart success. Items:', data.items?.length || 0);
      setCart(data)
    } catch (err) {
      console.error('Failed to fetch cart', err)
    }
  }

  async function addToCart(productId, quantity = 1) {
    if (isGuest) {
      showToast('Please sign in to add items to cart')
      return
    }
    try {
      const { data } = await api.post('/api/cart/items', { productId, quantity })
      setCart(data)
      setActiveAnimation('cart')
    } catch (err) {
      showToast(err.response?.data?.error || 'Failed to add to cart')
    }
  }

  async function removeFromCart(productId) {
    try {
      const { data } = await api.delete(`/api/cart/items/${productId}`)
      setCart(data)
    } catch (err) {
      showToast('Failed to remove item')
    }
  }

  async function updateCartQuantity(productId, quantity) {
    try {
      const { data } = await api.put(`/api/cart/items/${productId}`, quantity, {
        headers: { 'Content-Type': 'application/json' }
      })
      setCart(data)
    } catch (err) {
      showToast(err.response?.data?.error || 'Failed to update quantity')
      fetchCart()
    }
  }

  function toggleFavorite(product) {
    setFavorites(prev => {
      const isFav = prev.find(p => p.id === product.id)
      if (isFav) {
        return prev.filter(p => p.id !== product.id)
      } else {
        return [...prev, product]
      }
    })
  }

  async function handleLogout() {
    await unregisterTokenFromBackend()
    const refreshToken = localStorage.getItem('refreshToken')
    if (refreshToken) {
      try { await api.post('/api/auth/logout', { refreshToken }) } catch { /* ignore */ }
    }
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('accountType')
    localStorage.removeItem('isGuest')
    setAuthToken(null)
    setLoggedIn(false)
    setIsGuest(false)
    setAccountType(null)
    setCurrentUserId(null)
    setCart({ items: [], totalAmount: 0 })
    setUserLocation({ country: null, city: null })
    navigate('/login')
  }

  function handleLoginSuccess() {
    setLoggedIn(true)
    setIsGuest(false)
    localStorage.removeItem('isGuest')
    const at = localStorage.getItem('accountType')
    setAccountType(at)
    if (at === 'User') {
      fetchCart()
    }
    fetchProfile() // Fetch location after login
    requestNotificationPermissionAndGetToken() // Silently register device token if permission already granted
    navigate('/')
  }

  function handleGuestLogin() {
    localStorage.setItem('isGuest', 'true')
    setIsGuest(true)
    navigate('/')
  }

  function handleGuestSignIn() {
    localStorage.removeItem('isGuest')
    setIsGuest(false)
    navigate('/login')
  }



  // Pre-configured shared prop lists for the pages
  const searchProductsProps = {
    onSelectProduct: (p) => navigate(`/product/${p.id}`),
    onNavigateCart: () => navigate('/cart'),
    cartCount: cart.items.length,
    favorites: favorites,
    onToggleFavorite: toggleFavorite,
    onAddToCart: addToCart,
    onNavigateStore: (storeId) => navigate(`/store/${storeId}`),
    userLocation: userLocation
  };

  const publicStoreProps = {
    onSelectProduct: (p) => navigate(`/product/${p.id}`),
    userLocation: userLocation
  };

  const favoritesProps = {
    onSelectProduct: (p) => navigate(`/product/${p.id}`),
    onAddToCart: addToCart,
    onToggleFavorite: toggleFavorite,
    onNavigateCart: () => navigate('/cart'),
    cartCount: cart.items.length
  };

  const myShopsProps = {
    onNavigateStore: (storeId) => navigate(`/store/${storeId}`),
    onAddToCart: addToCart,
    onNavigateCart: () => navigate('/cart')
  };

  const productInfoProps = {
    onSelectRelatedProduct: (p) => navigate(`/product/${p.id}`),
    onAddToCart: addToCart,
    onNavigateCart: () => navigate('/cart'),
    onNavigateStore: (storeId) => navigate(`/store/${storeId}`),
    cartCount: cart.items.length,
    favorites: favorites,
    onToggleFavorite: toggleFavorite,
    currentUserId: currentUserId
  };

  const categoryProductsProps = {
    onSelectProduct: (p) => navigate(`/product/${p.id}`),
    onNavigateCart: () => navigate('/cart'),
    onNavigateStore: (storeId) => navigate(`/store/${storeId}`),
    onAddToCart: addToCart,
    cartCount: cart.items.length,
    favorites: favorites,
    onToggleFavorite: toggleFavorite,
    userLocation: userLocation
  };

  const categoriesProps = {
    isGuest: isGuest,
    accountType: accountType,
    onLogout: isGuest ? handleGuestSignIn : handleLogout,
    onNavigateCart: () => navigate('/cart'),
    cartCount: cart.items.length,
    onSelectCategory: (cat) => navigate(`/${cat.name}`),
    onNavigateSettings: () => navigate('/settings'),
    onNavigateFavorites: () => navigate('/favorites'),
    onNavigateMyShops: () => navigate('/my-shops'),
    onNavigateSearch: (query) => navigate(`/search?q=${encodeURIComponent(query)}`),
    onNavigateProfile: () => navigate('/profile'),
    onNavigateStoreProducts: () => navigate('/store-products'),
    onNavigateStoreOrders: () => navigate('/store-orders'),
    onNavigateStoreAnalytics: () => navigate('/store-analytics'),
    onNavigateDeliveryJobs: () => navigate('/delivery-jobs'),
    onNavigateAdmin: () => navigate('/admin'),
    onNavigateNotifications: () => navigate('/notifications')
  };

  const showGlobalNav = (loggedIn || isGuest) && !['/login', '/signup'].includes(location.pathname);

  return (
    <>
      {showGlobalNav && (
        <>
          <NavBar
            loggedIn={loggedIn}
            isGuest={isGuest}
            accountType={accountType}
            cartCount={cart.items.length}
            theme={theme}
            toggleTheme={toggleTheme}
            userLocation={userLocation}
            onNavigateHome={() => navigate('/')}
            onNavigateCart={() => navigate('/cart')}
            onNavigateFavorites={() => navigate('/favorites')}
            onNavigateProfile={() => navigate('/profile')}
            onNavigateEditProfile={() => navigate('/edit-profile')}
            onNavigateNotifications={() => navigate('/notifications')}
            onNavigateSettings={() => navigate('/settings')}
            onNavigateSearch={(query) => navigate(`/search?q=${encodeURIComponent(query)}`)}
            onToggleSidebar={() => setSidebarOpen(true)}
            onNavigateStoreAnalytics={() => navigate('/store-analytics')}
            onNavigateAdmin={() => navigate('/admin')}
            onNavigateDeliveryJobs={() => navigate('/delivery-jobs')}
            onNavigateStoreProducts={() => navigate('/store-products')}
            onNavigateStoreOrders={() => navigate('/store-orders')}
            onGuestSignIn={handleGuestSignIn}
          />
          <NotificationPermissionBanner />

          {/* Global Sidebar Overlay & Drawer */}
          <div className={`sidebar-overlay ${sidebarOpen ? 'open' : ''}`} onClick={() => setSidebarOpen(false)} />
          <aside className={`sidebar ${sidebarOpen ? 'open' : ''}`}>
              <div className="sidebar-header">
                  {isGuest ? (
                      <>
                          <div style={{ width: 80, height: 80, borderRadius: '50%', border: '2px solid var(--outline-variant)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                              <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="var(--outline)" strokeWidth="1.5"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" /></svg>
                          </div>
                          <h3 className="sidebar-name">{t('Guest')}</h3>
                          <span className="sidebar-role-badge">{t('Browse Only')}</span>
                      </>
                  ) : (
                      <>
                          <div style={{ position: 'relative', display: 'inline-block' }}>
                              <RippleAvatar
                                  src={profileData.avatarUrl}
                                  onClick={handleAvatarClick}
                                  title={t('Upload a picture')}
                                  size={80}
                                  glowThickness={3}
                              />
                              <button 
                                  onClick={handleAvatarClick}
                                  style={{
                                      position: 'absolute',
                                      bottom: 0,
                                      right: 0,
                                      background: 'var(--primary)',
                                      color: 'var(--on-primary)',
                                      border: '2px solid var(--surface-container)',
                                      borderRadius: '50%',
                                      width: 28,
                                      height: 28,
                                      display: 'flex',
                                      alignItems: 'center',
                                      justifyContent: 'center',
                                      cursor: 'pointer',
                                      padding: 0,
                                      zIndex: 20,
                                      boxShadow: 'var(--shadow-sm)'
                                  }}
                                  title={t('Upload a picture')}
                              >
                                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>
                              </button>
                          </div>
                          <input
                              type="file"
                              accept="image/*"
                              ref={fileInputRef}
                              onChange={(e) => {
                                const file = e.target.files?.[0];
                                if (file) handleAvatarUpload(file);
                              }}
                              style={{ display: 'none' }}
                          />
                          <h3 className="sidebar-name" style={{ marginTop: '12px' }}>{profileData.name}</h3>
                          {accountType && <span className="sidebar-role-badge">{accountType}</span>}
                      </>
                  )}
              </div>

              <nav className="sidebar-nav">
                  <button className={`sidebar-link ${location.pathname === '/' || location.pathname === '/categories' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/'); }}>
                      <span className="material-symbols-outlined">home</span> {t('Home')}
                  </button>

                  {!isGuest && (
                      <>
                          <button className={`sidebar-link ${location.pathname === '/profile' ? 'active' : ''}`} onClick={() => {
                              setSidebarOpen(false)
                              navigate('/profile')
                          }}>
                              <span className="material-symbols-outlined">person</span> {t('Profile')}
                          </button>

                          {accountType === 'User' && (
                              <>
                                  <button className={`sidebar-link ${location.pathname === '/favorites' ? 'active' : ''}`} onClick={() => {
                                      setSidebarOpen(false)
                                      navigate('/favorites')
                                  }}>
                                      <span className="material-symbols-outlined">favorite</span> {t('Favorites')}
                                  </button>

                                  <button className={`sidebar-link ${location.pathname === '/my-shops' ? 'active' : ''}`} onClick={() => {
                                      setSidebarOpen(false)
                                      navigate('/my-shops')
                                  }}>
                                      <span className="material-symbols-outlined">local_shipping</span> {t('Delivered Orders')}
                                  </button>
                              </>
                          )}

                          {accountType === 'Admin' && (
                              <button className={`sidebar-link ${location.pathname === '/admin' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/admin'); }}>
                                  <span className="material-symbols-outlined">shield</span> {t('Admin Panel')}
                              </button>
                          )}

                          {accountType === 'Shop' && (
                              <>
                                  <button className={`sidebar-link ${location.pathname === '/store-products' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/store-products'); }}>
                                      <span className="material-symbols-outlined">inventory_2</span> {t('My Products')}
                                  </button>
                                  <button className={`sidebar-link ${location.pathname === '/store-orders' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/store-orders'); }}>
                                      <span className="material-symbols-outlined">receipt_long</span> {t('Store Orders')}
                                  </button>
                                  <button className={`sidebar-link ${location.pathname === '/store-analytics' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/store-analytics'); }}>
                                      <span className="material-symbols-outlined">leaderboard</span> {t('Store Analytics')}
                                  </button>
                              </>
                          )}

                          {accountType === 'Delivery' && (
                              <button className={`sidebar-link ${location.pathname === '/delivery-jobs' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/delivery-jobs'); }}>
                                  <span className="material-symbols-outlined">local_shipping</span> {t('Delivery Jobs')}
                              </button>
                          )}

                          <button className={`sidebar-link ${location.pathname === '/notifications' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/notifications'); }}>
                              <span className="material-symbols-outlined">notifications</span> {t('Notifications')}
                          </button>

                          <button className={`sidebar-link ${location.pathname === '/settings' ? 'active' : ''}`} onClick={() => { setSidebarOpen(false); navigate('/settings'); }}>
                              <span className="material-symbols-outlined">settings</span> {t('Settings')}
                          </button>
                      </>
                  )}
              </nav>

              <div className="sidebar-footer">
                  <button className="logout-btn" onClick={() => { setSidebarOpen(false); if (isGuest) { handleGuestSignIn(); } else { handleLogout(); } }}>
                      <span className="material-symbols-outlined">logout</span> {isGuest ? t('Sign In') : t('Log out')}
                  </button>
              </div>
          </aside>
        </>
      )}

      <Suspense fallback={
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '80vh' }}>
          <div className="spinner" style={{ width: '40px', height: '40px', border: '4px solid var(--border)', borderTop: '4px solid var(--primary)', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
          <style>{`
            @keyframes spin {
              0% { transform: rotate(0deg); }
              100% { transform: rotate(360deg); }
            }
          `}</style>
        </div>
      }>
        <Routes>
          {/* Auth pages, accessible only when NOT logged in and NOT guest */}
          <Route path="/login" element={
            loggedIn || isGuest ? <Navigate to="/" replace /> : (
              <div className="auth-layout">
                <AuthPage
                  initialMode="Login"
                  onLoginSuccess={handleLoginSuccess}
                  onGuestLogin={handleGuestLogin}
                  theme={theme}
                  toggleTheme={toggleTheme}
                />
              </div>
            )
          } />

          <Route path="/signup" element={
            loggedIn || isGuest ? <Navigate to="/" replace /> : (
              <div className="auth-layout">
                <AuthPage
                  initialMode="Sign Up"
                  onLoginSuccess={handleLoginSuccess}
                  onGuestLogin={handleGuestLogin}
                  theme={theme}
                  toggleTheme={toggleTheme}
                />
              </div>
            )
          } />

          {/* All other pages, protected by RequireAuth */}
          <Route element={<RequireAuth loggedIn={loggedIn} isGuest={isGuest} />}>
            <Route path="/" element={<CategoriesPage {...categoriesProps} />} />
            <Route path="/categories" element={<CategoriesPage {...categoriesProps} />} />
            <Route path="/home" element={<CategoriesPage {...categoriesProps} />} />
            <Route path="/search" element={<SearchProductsWrapper searchProductsProps={searchProductsProps} />} />
            <Route path="/category/:id" element={<CategoryProductsWrapper categoryProductsProps={categoryProductsProps} />} />
            <Route path="/:categoryName" element={<CategoryProductsWrapper categoryProductsProps={categoryProductsProps} />} />
            <Route path="/product/:id" element={<ProductInfoWrapper productInfoProps={productInfoProps} />} />
            <Route path="/store/:storeId" element={<PublicStoreWrapper publicStoreProps={publicStoreProps} />} />
            <Route path="/favorites" element={<FavoritesPage favorites={favorites} onBack={() => navigate(-1)} {...favoritesProps} />} />
            <Route path="/my-shops" element={<MyShopsPage onBack={() => navigate(-1)} {...myShopsProps} />} />
            <Route path="/settings" element={<SettingsPage onBack={() => navigate(-1)} theme={theme} toggleTheme={toggleTheme} onLogout={handleLogout} />} />

            {/* Authenticated User Guarded Pages */}
            <Route path="/cart" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                <CartPage
                  cart={cart}
                  onBack={() => navigate(-1)}
                  onUpdateQuantity={updateCartQuantity}
                  onRemoveItem={removeFromCart}
                  onNavigateStore={(storeId) => navigate(`/store/${storeId}`)}
                  onCheckout={() => {
                    if (cart.items.length === 0) return;
                    navigate('/checkout');
                  }}
                />
              </UserProtectedRoute>
            } />

            <Route path="/checkout" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                <CheckoutPage
                  cart={cart}
                  onBack={() => navigate(-1)}
                  onProceed={(data) => {
                    setCheckoutData(data);
                    navigate('/payment');
                  }}
                  userLocation={userLocation}
                  onChangeLocation={async (newCity, shouldRedirect = true) => {
                    try {
                      const { data: profile } = await api.get('/api/me');
                      await api.put('/api/me/profile', {
                        fullName: profile.fullName,
                        country: profile.country,
                        street: profile.street,
                        buildingNumber: profile.buildingNumber,
                        city: newCity,
                        postalCode: profile.postalCode,
                        latitude: profile.latitude,
                        longitude: profile.longitude,
                        description: profile.description,
                        openingTime: profile.openingTime,
                        closingTime: profile.closingTime
                      });
                      await fetchProfile();
                      if (shouldRedirect) showToast(`Profile location updated to ${newCity}`);
                    } catch {
                      if (shouldRedirect) showToast(`Location updated to ${newCity} (locally)`);
                    }
                    if (shouldRedirect) {
                      navigate('/');
                    }
                  }}
                  showToast={showToast}
                  onNavigateEditProfile={() => navigate('/edit-profile#address')}
                />
              </UserProtectedRoute>
            } />

            <Route path="/payment" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                <PaymentPage
                  cart={cart}
                  checkoutData={checkoutData}
                  onBack={() => navigate(-1)}
                  onSuccess={async () => {
                    setActiveAnimation('payment');
                    await fetchCart();
                  }}
                  showToast={showToast}
                />
              </UserProtectedRoute>
            } />

            <Route path="/profile" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                <ProfilePage 
                  onBack={() => navigate(-1)} 
                  onEdit={() => navigate('/edit-profile')} 
                  onNavigateSettings={() => navigate('/settings')}
                  onLogout={handleLogout}
                  onNavigateStore={(storeId) => navigate(`/store/${storeId}`)}
                />
              </UserProtectedRoute>
            } />

            <Route path="/edit-profile" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                 <EditProfilePage 
                   onBack={() => navigate(-1)} 
                   onSave={async () => {
                     await fetchProfile();
                     showToast(t('Profile updated successfully!'));
                     navigate(-1);
                   }}
                 />
              </UserProtectedRoute>
            } />

            <Route path="/notifications" element={
              <UserProtectedRoute loggedIn={loggedIn}>
                 <NotificationsPage onBack={() => navigate(-1)} />
              </UserProtectedRoute>
            } />

            {/* Shop Owner Guarded Pages */}
            <Route path="/store-products" element={
              <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
                <StoreProductsPage onBack={() => navigate(-1)} />
              </RoleProtectedRoute>
            } />

            <Route path="/store-orders" element={
              <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
                <StoreOrdersPage onBack={() => navigate(-1)} />
              </RoleProtectedRoute>
            } />

            <Route path="/store-analytics" element={
              <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
                <StoreAnalyticsPage onBack={() => navigate(-1)} />
              </RoleProtectedRoute>
            } />

            {/* Delivery Driver Guarded Pages */}
            <Route path="/delivery-jobs" element={
              <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Delivery">
                <DeliveryJobsPage onBack={() => navigate('/')} />
              </RoleProtectedRoute>
            } />

            {/* Admin Guarded Pages */}
            <Route path="/admin" element={
              <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Admin">
                <AdminPage onBack={() => navigate('/')} />
              </RoleProtectedRoute>
            } />
          </Route>

          {/* Fallback route */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>

      {activeAnimation === 'cart' && (
        <AnimationOverlay
          animationData={addToCartAnim}
          onComplete={() => setActiveAnimation(null)}
          duration={1500}
        />
      )}
      {activeAnimation === 'payment' && (
        <AnimationOverlay
          animationData={paymentSuccessAnim}
          onComplete={() => {
            setActiveAnimation(null);
            navigate('/');
          }}
          duration={2000}
        />
      )}
      {loggedIn && accountType === 'User' && (
        <AiChatWidget
          onCartUpdate={fetchCart}
          onAddToCart={() => setActiveAnimation('cart')}
        />
      )}
      {loggedIn && accountType === 'Shop' && (
        <ShopAiChatWidget />
      )}
      {loggedIn && accountType === 'Admin' && (
        <AdminAiChatWidget />
      )}
    </>
  )
}

export default function App() {
  return (
    <ErrorProvider>
      <ErrorOverlay />
      <BrowserRouter>
        <AppContent />
      </BrowserRouter>
    </ErrorProvider>
  )
}
