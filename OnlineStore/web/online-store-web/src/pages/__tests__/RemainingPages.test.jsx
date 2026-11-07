import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
  setAuthToken: vi.fn(),
  changeLanguage: vi.fn(),
  infiniteReset: vi.fn(),
  infiniteSetItems: vi.fn(),
  stripeLoad: vi.fn(),
  stripeCardDelete: vi.fn(),
  signalrStart: vi.fn(),
  signalrStop: vi.fn(),
  signalrOn: vi.fn(),
  signalrOff: vi.fn(),
  signalrInvoke: vi.fn()
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key,
    i18n: {
      language: 'en',
      changeLanguage: mocks.changeLanguage
    }
  })
}))

vi.mock('@splinetool/react-spline', () => ({
  default: () => <div data-testid="spline-mock" />
}))

vi.mock('@stripe/stripe-js', () => ({
  loadStripe: (...args) => mocks.stripeLoad(...args)
}))

vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }) => <div data-testid="stripe-elements">{children}</div>
}))

vi.mock('@microsoft/signalr', () => ({
  HttpTransportType: { WebSockets: 1 },
  HubConnectionBuilder: class {
    withUrl() {
      return this
    }
    withAutomaticReconnect() {
      return this
    }
    build() {
      return {
        start: mocks.signalrStart,
        stop: mocks.signalrStop,
        on: mocks.signalrOn,
        off: mocks.signalrOff,
        invoke: mocks.signalrInvoke
      }
    }
  }
}))

vi.mock('react-leaflet', () => ({
  MapContainer: ({ children }) => <div data-testid="leaflet-map">{children}</div>,
  TileLayer: () => <div data-testid="leaflet-tile" />,
  Marker: ({ children }) => <div data-testid="leaflet-marker">{children}</div>,
  useMapEvents: vi.fn()
}))

vi.mock('leaflet', () => ({
  default: {
    Icon: {
      Default: {
        prototype: {},
        mergeOptions: vi.fn()
      }
    }
  }
}))

vi.mock('../../apiClient.js', () => ({
  api: {
    defaults: { baseURL: 'https://api.example.com' },
    get: (...args) => mocks.get(...args),
    post: (...args) => mocks.post(...args),
    put: (...args) => mocks.put(...args),
    delete: (...args) => mocks.del(...args)
  },
  setAuthToken: mocks.setAuthToken
}))

vi.mock('../../apiClient', () => ({
  api: {
    defaults: { baseURL: 'https://api.example.com' },
    get: (...args) => mocks.get(...args),
    post: (...args) => mocks.post(...args),
    put: (...args) => mocks.put(...args),
    delete: (...args) => mocks.del(...args)
  },
  setAuthToken: mocks.setAuthToken
}))

vi.mock('../../hooks/useInfiniteScroll.js', () => ({
  default: () => ({
    items: [],
    isLoading: false,
    hasMore: false,
    error: null,
    total: 0,
    sentinelRef: { current: null },
    retry: vi.fn(),
    reset: mocks.infiniteReset,
    initialLoad: false,
    setItems: mocks.infiniteSetItems
  })
}))

vi.mock('../../components/Skeleton.jsx', () => ({
  default: (props) => <div data-testid="skeleton-mock">{JSON.stringify(props || {})}</div>
}))

vi.mock('../../components/StripeAddCard.jsx', () => ({
  default: ({ onCancel, onSuccess }) => (
    <div data-testid="stripe-add-card">
      <button onClick={() => onSuccess?.({ id: 'card-new', brand: 'visa', last4: '4242' })}>mock-success</button>
      <button onClick={() => onCancel?.()}>mock-cancel</button>
    </div>
  )
}))

vi.mock('../../components/NotificationBell.jsx', () => ({
  default: () => <button data-testid="notification-bell">bell</button>
}))

vi.mock('../../components/RippleAvatar.jsx', () => ({
  default: () => <div data-testid="ripple-avatar" />
}))

vi.mock('../../components/ProfileRippleAvatar.jsx', () => ({
  default: () => <div data-testid="profile-ripple-avatar" />
}))

vi.mock('../../components/OrderTrackingCard.jsx', () => ({
  default: () => <div data-testid="order-tracking-card" />
}))

import AdminPage from '../AdminPage'
import AuthPage from '../AuthPage'
import CategoriesPage from '../CategoriesPage'
import CategoryProductsPage from '../CategoryProductsPage'
import CheckoutPage from '../CheckoutPage'
import DeliveryJobsPage from '../DeliveryJobsPage'
import EditProfilePage from '../EditProfilePage'
import MyShopsPage from '../MyShopsPage'
import NotificationsPage from '../NotificationsPage'
import PaymentPage from '../PaymentPage'
import ProductInfoPage from '../ProductInfoPage'
import PublicStorePage from '../PublicStorePage'
import SearchProductsPage from '../SearchProductsPage'
import StoreOrdersPage from '../StoreOrdersPage'
import StoreProductsPage from '../StoreProductsPage'

function mockGetResponse(url) {
  const asString = String(url)

  if (asString.includes('/api/payment/config')) {
    return { data: { publishableKey: 'pk_test_12345' } }
  }
  if (asString.includes('/api/payment/cards')) {
    return { data: [{ id: 'card-1', brand: 'visa', last4: '4242' }] }
  }
  if (asString.includes('/api/me/addresses')) {
    return {
      data: [
        {
          id: 1,
          title: 'Home',
          street: 'Main Street',
          buildingNumber: '12',
          city: 'Istanbul',
          postalCode: '34000',
          isDefault: true
        }
      ]
    }
  }
  if (asString.includes('/api/me')) {
    return {
      data: {
        id: 'user-1',
        fullName: 'Test User',
        email: 'test@example.com',
        accountType: 'User',
        city: 'Istanbul',
        street: 'Main Street',
        buildingNumber: '12',
        postalCode: '34000',
        latitude: 41.01,
        longitude: 28.97
      }
    }
  }
  if (asString.includes('/api/products/') && asString.includes('/details')) {
    return {
      data: {
        product: {
          id: 11,
          name: 'Sample Product',
          description: 'Sample description',
          imageUrl: '/uploads/sample.jpg',
          additionalImageUrls: [],
          price: 25,
          storeUserId: 'store-1',
          storeName: 'Sample Store',
          originalQuantity: 10
        },
        totalSales: 5,
        inStock: 8,
        averageRating: 4.2,
        totalReviews: 1,
        latestReviews: [{ id: 1, userName: 'Alex', rating: 4, comment: 'Good' }],
        relatedProducts: [],
        dailySales: [{ day: 'Mon', count: 1 }]
      }
    }
  }
  if (asString.includes('/api/shops/')) {
    return {
      data: {
        id: 'store-1',
        shopName: 'Public Store',
        openingTime: '08:00',
        closingTime: '22:00',
        location: 'Istanbul'
      }
    }
  }
  if (asString.includes('/api/categories/') && asString.includes('/subcategories')) {
    return { data: [{ id: 101, name: 'Subcategory A' }] }
  }
  if (asString.includes('/api/categories')) {
    return { data: [{ id: 1, name: 'Category A' }] }
  }
  if (asString.includes('/api/products/approved')) {
    return { data: [] }
  }
  if (asString.includes('/api/orders/active')) {
    return { data: [] }
  }
  if (asString.includes('/api/delivery/')) {
    return { data: [] }
  }
  if (asString.includes('/api/notifications')) {
    return { data: [] }
  }

  return { data: [] }
}

describe('Remaining Untested Pages', () => {
  beforeEach(() => {
    localStorage.clear()
    window.alert = vi.fn()
    window.confirm = vi.fn(() => true)
    mocks.get.mockImplementation(async (url) => mockGetResponse(url))
    mocks.post.mockResolvedValue({ data: {} })
    mocks.put.mockResolvedValue({ data: {} })
    mocks.del.mockResolvedValue({ data: {} })
    mocks.stripeLoad.mockResolvedValue({ id: 'stripe-instance' })
    mocks.signalrStart.mockResolvedValue()
    mocks.signalrStop.mockResolvedValue()
    mocks.signalrOn.mockImplementation(() => {})
    mocks.signalrOff.mockImplementation(() => {})
    mocks.signalrInvoke.mockResolvedValue()
    mocks.infiniteReset.mockReset()
    mocks.infiniteSetItems.mockReset()
  })

  it('renders AdminPage', async () => {
    render(<AdminPage onBack={vi.fn()} />)
    expect(await screen.findByText('Admin Panel')).toBeInTheDocument()
  })

  it('renders AuthPage and triggers guest callback', async () => {
    const onGuestLogin = vi.fn()
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <AuthPage onLoginSuccess={vi.fn()} onGuestLogin={onGuestLogin} theme="light" toggleTheme={vi.fn()} />
      </MemoryRouter>
    )
    expect(screen.getByPlaceholderText('name@example.com')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /continue as guest/i }))
    expect(onGuestLogin).toHaveBeenCalledTimes(1)
  })

  it('renders CategoriesPage', async () => {
    render(
      <CategoriesPage
        onLogout={vi.fn()}
        theme="light"
        onToggleTheme={vi.fn()}
        accountType="User"
        isGuest={false}
        onNavigateProfile={vi.fn()}
        onNavigateStoreProducts={vi.fn()}
        onNavigateStoreOrders={vi.fn()}
        onNavigateDeliveryJobs={vi.fn()}
        onNavigateAdmin={vi.fn()}
        onNavigateNotifications={vi.fn()}
        onNavigateSettings={vi.fn()}
        onNavigateCart={vi.fn()}
        onNavigateFavorites={vi.fn()}
        onNavigateMyShops={vi.fn()}
        onNavigateSearch={vi.fn()}
        cartCount={0}
        onSelectCategory={vi.fn()}
      />
    )
    expect(await screen.findByText('Shop by Category')).toBeInTheDocument()
  })

  it('renders CategoryProductsPage', async () => {
    render(
      <CategoryProductsPage
        categoryId={1}
        categoryName="Category A"
        onBack={vi.fn()}
        onSelectProduct={vi.fn()}
        onNavigateCart={vi.fn()}
        cartCount={0}
        onAddToCart={vi.fn()}
        onNavigateStore={vi.fn()}
      />
    )
    expect(await screen.findByText('Category A')).toBeInTheDocument()
  })

  it('renders CheckoutPage and allows proceed action', async () => {
    const onProceed = vi.fn()
    const user = userEvent.setup()
    render(
      <CheckoutPage
        cart={{ items: [{ id: 1 }], totalAmount: 20 }}
        onBack={vi.fn()}
        onProceed={onProceed}
        userLocation={{ city: 'Istanbul', country: 'Turkey' }}
        onChangeLocation={vi.fn()}
      />
    )
    expect(await screen.findByText('Checkout')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Proceed to payment' }))
    expect(onProceed).toHaveBeenCalledTimes(1)
  })

  it('renders DeliveryJobsPage', async () => {
    render(<DeliveryJobsPage onBack={vi.fn()} />)
    expect(await screen.findByText('Delivery Jobs')).toBeInTheDocument()
  })

  it('renders EditProfilePage', async () => {
    render(<EditProfilePage onBack={vi.fn()} onSave={vi.fn()} />)
    expect(await screen.findByText('Edit Profile')).toBeInTheDocument()
  })

  it('renders MyShopsPage', async () => {
    render(
      <MyShopsPage
        onBack={vi.fn()}
        onNavigateStore={vi.fn()}
        onAddToCart={vi.fn()}
        onNavigateCart={vi.fn()}
      />
    )
    expect(await screen.findByText('Delivered Orders')).toBeInTheDocument()
  })

  it('renders NotificationsPage', async () => {
    render(<NotificationsPage onBack={vi.fn()} />)
    expect(await screen.findByText('Notifications')).toBeInTheDocument()
  })

  it('renders PaymentPage with credit card flow', async () => {
    render(
      <PaymentPage
        cart={{ items: [{ id: 1 }], totalAmount: 22.5 }}
        checkoutData={{ addressString: 'Home', paymentMethod: 'Credit Card' }}
        onBack={vi.fn()}
        onSuccess={vi.fn()}
      />
    )
    expect(await screen.findByRole('heading', { name: 'Payment details' })).toBeInTheDocument()
    await waitFor(() => {
      expect(mocks.stripeLoad).toHaveBeenCalled()
    })
  })

  it('renders ProductInfoPage', async () => {
    render(
      <ProductInfoPage
        productId={11}
        onBack={vi.fn()}
        onSelectRelatedProduct={vi.fn()}
        onAddToCart={vi.fn()}
        onNavigateCart={vi.fn()}
        cartCount={0}
        onNavigateStore={vi.fn()}
        currentUserId="user-2"
      />
    )
    expect(await screen.findByText('Product Info')).toBeInTheDocument()
  })

  it('renders PublicStorePage and loads store details', async () => {
    render(
      <PublicStorePage
        storeUserId="store-1"
        onBack={vi.fn()}
        onSelectProduct={vi.fn()}
      />
    )
    expect(await screen.findByText('Public Store')).toBeInTheDocument()
  })

  it('renders SearchProductsPage', async () => {
    render(
      <SearchProductsPage
        initialQuery="milk"
        onBack={vi.fn()}
        onSelectProduct={vi.fn()}
        onNavigateCart={vi.fn()}
        cartCount={0}
        onAddToCart={vi.fn()}
        onNavigateStore={vi.fn()}
      />
    )
    expect(await screen.findByPlaceholderText('Search products...')).toBeInTheDocument()
    expect(await screen.findByText('Filters')).toBeInTheDocument()
  })

  it('renders StoreOrdersPage', async () => {
    render(<StoreOrdersPage onBack={vi.fn()} />)
    expect(await screen.findByText('Store Orders')).toBeInTheDocument()
  })

  it('renders StoreProductsPage', async () => {
    render(
      <MemoryRouter>
        <StoreProductsPage onBack={vi.fn()} />
      </MemoryRouter>
    )
    expect(await screen.findByText('My Products')).toBeInTheDocument()
  })
})
