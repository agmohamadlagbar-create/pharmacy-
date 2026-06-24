import { useState, useEffect } from 'react'
import { BrowserRouter, Routes, Route, useNavigate, useParams, useLocation, Navigate, Outlet } from 'react-router-dom'
import './i18n'
import AuthPage from './pages/AuthPage.jsx'
import CategoriesPage from './pages/CategoriesPage.jsx'
import ProfilePage from './pages/ProfilePage.jsx'
import EditProfilePage from './pages/EditProfilePage.jsx'
import CategoryProductsPage from './pages/CategoryProductsPage.jsx'
import StoreProductsPage from './pages/StoreProductsPage.jsx'
import StoreOrdersPage from './pages/StoreOrdersPage.jsx'
import StoreAnalyticsPage from './pages/StoreAnalyticsPage.jsx'
import DeliveryJobsPage from './pages/DeliveryJobsPage.jsx'
import AdminPage from './pages/AdminPage.jsx'
import NotificationsPage from './pages/NotificationsPage.jsx'
import ProductInfoPage from './pages/ProductInfoPage.jsx'
import CartPage from './pages/CartPage.jsx'
import PublicStorePage from './pages/PublicStorePage.jsx'
import SettingsPage from './pages/SettingsPage.jsx'
import CheckoutPage from './pages/CheckoutPage.jsx'
import PaymentPage from './pages/PaymentPage.jsx'
import FavoritesPage from './pages/FavoritesPage.jsx'
import SearchProductsPage from './pages/SearchProductsPage.jsx'
import MyShopsPage from './pages/MyShopsPage.jsx'
import Skeleton from './components/Skeleton.jsx'
import AnimationOverlay from './components/AnimationOverlay.jsx'
import addToCartAnim from './assets/lottie/add-to-cart.json'
import paymentSuccessAnim from './assets/lottie/payment-success.json'
import { api, setAuthToken } from './apiClient.js'
import { ErrorProvider, useError } from './context/ErrorContext'
import ErrorOverlay from './components/ErrorOverlay'
import AiChatWidget from './components/AiChatWidget.jsx'

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
          <button className="back-btn-circular" onClick={() => navigate('/')}>
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
      onBack={() => navigate('/')}
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
      onBack={() => navigate('/')}
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
      onBack={() => navigate('/')}
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

  useEffect(() => {
    fetchProfile()
  }, [loggedIn])

  async function fetchProfile() {
    if (!loggedIn) {
      setUserLocation({ country: null, city: null })
      return
    }
    try {
      const { data } = await api.get('/api/me')
      setUserLocation({ country: data.country, city: data.city })
      setCurrentUserId(data.id)
    } catch (err) {
      console.error('Failed to fetch profile', err)
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

  return (
    <>
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
          <Route path="/favorites" element={<FavoritesPage favorites={favorites} onBack={() => navigate('/')} {...favoritesProps} />} />
          <Route path="/my-shops" element={<MyShopsPage onBack={() => navigate('/')} {...myShopsProps} />} />
          <Route path="/settings" element={<SettingsPage onBack={() => navigate('/')} theme={theme} toggleTheme={toggleTheme} />} />

          {/* Authenticated User Guarded Pages */}
          <Route path="/cart" element={
            <UserProtectedRoute loggedIn={loggedIn}>
              <CartPage
                cart={cart}
                onBack={() => navigate('/')}
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
                onBack={() => navigate('/cart')}
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
                  } catch (err) {
                    console.error('Failed to update profile location', err);
                    setUserLocation(prev => ({ ...prev, city: newCity }));
                    if (shouldRedirect) showToast(`Location updated to ${newCity} (locally)`);
                  }
                  if (shouldRedirect) {
                    navigate('/');
                  }
                }}
              />
            </UserProtectedRoute>
          } />

          <Route path="/payment" element={
            <UserProtectedRoute loggedIn={loggedIn}>
              <PaymentPage
                cart={cart}
                checkoutData={checkoutData}
                onBack={() => navigate('/checkout')}
                onSuccess={() => {
                  setActiveAnimation('payment');
                  setTimeout(() => {
                    fetchCart();
                    navigate('/');
                  }, 2300);
                }}
              />
            </UserProtectedRoute>
          } />

          <Route path="/profile" element={
            <UserProtectedRoute loggedIn={loggedIn}>
              <ProfilePage onBack={() => navigate('/')} onEdit={() => navigate('/edit-profile')} />
            </UserProtectedRoute>
          } />

          <Route path="/edit-profile" element={
            <UserProtectedRoute loggedIn={loggedIn}>
              <EditProfilePage onBack={() => navigate('/profile')} onSave={() => { fetchProfile(); navigate('/profile'); }} />
            </UserProtectedRoute>
          } />

          <Route path="/notifications" element={
            <UserProtectedRoute loggedIn={loggedIn}>
              <NotificationsPage onBack={() => navigate('/')} />
            </UserProtectedRoute>
          } />

          {/* Shop Owner Guarded Pages */}
          <Route path="/store-products" element={
            <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
              <StoreProductsPage onBack={() => navigate('/')} />
            </RoleProtectedRoute>
          } />

          <Route path="/store-orders" element={
            <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
              <StoreOrdersPage onBack={() => navigate('/')} />
            </RoleProtectedRoute>
          } />

          <Route path="/store-analytics" element={
            <RoleProtectedRoute loggedIn={loggedIn} accountType={accountType} requiredRole="Shop">
              <StoreAnalyticsPage onBack={() => navigate('/')} />
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
          onComplete={() => setActiveAnimation(null)}
          duration={2000}
        />
      )}
      {loggedIn && (
        <AiChatWidget 
          onCartUpdate={fetchCart} 
          onAddToCart={() => setActiveAnimation('cart')} 
        />
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
