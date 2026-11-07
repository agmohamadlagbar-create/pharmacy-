import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import StoreAnalyticsPage from '../StoreAnalyticsPage'

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn()
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key) => key,
    i18n: {
      language: 'en',
      changeLanguage: vi.fn()
    }
  })
}))

vi.mock('../../apiClient.js', () => ({
  api: {
    defaults: { baseURL: 'https://api.example.com' },
    get: (...args) => mocks.get(...args),
    post: (...args) => mocks.post(...args),
    put: (...args) => mocks.put(...args),
    delete: (...args) => mocks.del(...args)
  }
}))

// Recharts components mock to avoid SVG rendering issues in jsdom environment
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }) => <div>{children}</div>,
  AreaChart: ({ children }) => <div>{children}</div>,
  Area: () => <div>Area</div>,
  Line: () => <div>Line</div>,
  XAxis: () => <div>XAxis</div>,
  YAxis: () => <div>YAxis</div>,
  CartesianGrid: () => <div>CartesianGrid</div>,
  Tooltip: () => <div>Tooltip</div>
}))

// Mock lottie-react to avoid canvas-related JSDOM errors
vi.mock('lottie-react', () => ({
  __esModule: true,
  default: () => <div data-testid="lottie-animation">Lottie</div>
}))

const mockAnalyticsData = {
  summary: {
    totalRevenue: 5000,
    averageDailyRevenue: 150,
    averageMonthlyRevenue: 4500,
    peakDayRevenue: 800,
    totalOrders: 120,
    averageOrderValue: 41.6,
    currencySymbol: '$'
  },
  dailySales: [],
  monthlySales: [],
  topSellingProducts: [],
  productPreviews: [],
  growth: {
    revenueGrowthPercent: 12.5,
    orderGrowthPercent: 8.3,
    previousMonthRevenue: 4400,
    currentMonthRevenue: 5000,
    previousMonthOrders: 110,
    currentMonthOrders: 120
  },
  trendingProducts: [],
  returningCustomers: {
    totalCustomers: 100,
    returningCustomers: 30,
    returningPercentage: 30.0
  },
  ratingStats: {
    overallAverageRating: 4.5,
    totalReviews: 25,
    highestRated: [],
    lowestRated: []
  },
  lowStockProducts: [],
  aiRecommendations: [],
  lowStockThreshold: 10
}

describe('StoreAnalyticsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.get.mockImplementation(async (url) => {
      if (url.includes('/api/analytics/dashboard')) {
        return { data: mockAnalyticsData }
      }
      return { data: {} }
    })
    mocks.post.mockResolvedValue({ data: {} })
    mocks.put.mockResolvedValue({ data: {} })
  })

  it('renders loading state and then data', async () => {
    render(
      <MemoryRouter>
        <StoreAnalyticsPage onBack={vi.fn()} />
      </MemoryRouter>
    )

    expect(screen.getByText('Loading analytics...')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByText('Store Analytics')).toBeInTheDocument()
    })

    expect(screen.getByText('Total Revenue')).toBeInTheDocument()
    expect(screen.getByText('$5,000')).toBeInTheDocument()
  })
})
