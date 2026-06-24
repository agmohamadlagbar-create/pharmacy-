import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Area, AreaChart } from 'recharts'
import './StoreAnalyticsPage.css'

// ═══════════════════════════════════════════════════
//  Icons
// ═══════════════════════════════════════════════════

const ChartIcon = () => <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>
const DollarIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="12" y1="1" x2="12" y2="23"></line><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path></svg>
const CalendarIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
const TrendUpIcon = () => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline><polyline points="17 6 23 6 23 12"></polyline></svg>
const TrendDownIcon = () => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><polyline points="23 18 13.5 8.5 8.5 13.5 1 6"></polyline><polyline points="17 18 23 18 23 12"></polyline></svg>
const PackageIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="16.5" y1="9.4" x2="7.5" y2="4.21"></line><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path><polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline><line x1="12" y1="22.08" x2="12" y2="12"></line></svg>
const UsersIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
const StarIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="1"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
const AlertIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
const LightbulbIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 18h6"></path><path d="M10 22h4"></path><path d="M15.09 14c.18-.98.65-1.74 1.41-2.5A4.65 4.65 0 0 0 18 8 6 6 0 0 0 6 8c0 1 .23 2.23 1.5 3.5A4.61 4.61 0 0 1 8.91 14"></path></svg>
const DownloadIcon = () => <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
const ShoppingBagIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path><line x1="3" y1="6" x2="21" y2="6"></line><path d="M16 10a4 4 0 0 1-8 0"></path></svg>
const TargetIcon = () => <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"></circle><circle cx="12" cy="12" r="6"></circle><circle cx="12" cy="12" r="2"></circle></svg>

// ═══════════════════════════════════════════════════
//  Date Presets
// ═══════════════════════════════════════════════════

function getDatePreset(key) {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  switch (key) {
    case 'today':
      return { start: today, end: now }
    case '7days':
      return { start: new Date(today.getTime() - 6 * 86400000), end: now }
    case '30days':
      return { start: new Date(today.getTime() - 29 * 86400000), end: now }
    case 'this_month':
      return { start: new Date(now.getFullYear(), now.getMonth(), 1), end: now }
    case 'prev_month': {
      const pm = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      return { start: pm, end: new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59) }
    }
    case 'this_year':
      return { start: new Date(now.getFullYear(), 0, 1), end: now }
    default:
      return null
  }
}

function formatDateParam(d) {
  return d.toISOString().split('T')[0]
}

// ═══════════════════════════════════════════════════
//  Custom Tooltip
// ═══════════════════════════════════════════════════

function ChartTooltip({ active, payload, label, currency }) {
  if (!active || !payload?.length) return null
  return (
    <div style={{
      background: 'var(--card)', border: '1px solid var(--border)',
      borderRadius: 10, padding: '10px 14px', boxShadow: '0 4px 16px rgba(0,0,0,0.12)',
      fontSize: '0.82rem'
    }}>
      <div style={{ fontWeight: 600, marginBottom: 4, color: 'var(--text)' }}>{label}</div>
      {payload.map((p, i) => (
        <div key={i} style={{ color: p.color, display: 'flex', gap: 8, alignItems: 'center' }}>
          <span>{p.name}:</span>
          <span style={{ fontWeight: 700 }}>
            {p.name === 'Orders' ? p.value : `${currency}${Number(p.value).toLocaleString()}`}
          </span>
        </div>
      ))}
    </div>
  )
}

// ═══════════════════════════════════════════════════
//  Star Rating Component
// ═══════════════════════════════════════════════════

function StarRating({ rating, size = 18 }) {
  const stars = []
  for (let i = 1; i <= 5; i++) {
    if (i <= Math.floor(rating)) {
      stars.push(<span key={i} className="star filled" style={{ fontSize: size }}>★</span>)
    } else if (i - 0.5 <= rating) {
      stars.push(<span key={i} className="star half" style={{ fontSize: size }}>★</span>)
    } else {
      stars.push(<span key={i} className="star empty" style={{ fontSize: size }}>★</span>)
    }
  }
  return <div className="ratings-stars">{stars}</div>
}

// ═══════════════════════════════════════════════════
//  Circular Progress
// ═══════════════════════════════════════════════════

function CircularProgress({ percentage, size = 120 }) {
  const strokeWidth = 8
  const radius = (size - strokeWidth) / 2
  const circumference = 2 * Math.PI * radius
  const offset = circumference - (Math.min(percentage, 100) / 100) * circumference

  return (
    <div className="returning-circle" style={{ width: size, height: size }}>
      <svg width={size} height={size}>
        <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="var(--border)" strokeWidth={strokeWidth} />
        <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="#6366f1" strokeWidth={strokeWidth}
          strokeDasharray={circumference} strokeDashoffset={offset}
          strokeLinecap="round" style={{ transition: 'stroke-dashoffset 1s ease' }} />
      </svg>
      <div className="returning-circle-text">
        <span className="returning-circle-pct">{percentage.toFixed(1)}%</span>
        <span className="returning-circle-label">Returning</span>
      </div>
    </div>
  )
}

// ═══════════════════════════════════════════════════
//  Main Component
// ═══════════════════════════════════════════════════

export default function StoreAnalyticsPage({ onBack }) {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [data, setData] = useState(null)

  // Date range state
  const [activePreset, setActivePreset] = useState('this_year')
  const [customStart, setCustomStart] = useState('')
  const [customEnd, setCustomEnd] = useState('')

  // Report state
  const [reportTab, setReportTab] = useState('monthly')
  const [reportYear, setReportYear] = useState(new Date().getFullYear())
  const [reportMonth, setReportMonth] = useState(new Date().getMonth() + 1)
  const [downloading, setDownloading] = useState(null)

  // Low stock threshold
  const [threshold, setThreshold] = useState(10)
  const [savingThreshold, setSavingThreshold] = useState(false)

  const baseURL = api.defaults.baseURL || ''

  const fetchDashboard = useCallback(async (preset, cStart, cEnd) => {
    setLoading(true)
    setError(null)
    try {
      let url = '/api/analytics/dashboard'
      const params = []

      if (preset === 'custom' && cStart && cEnd) {
        params.push(`startDate=${cStart}`, `endDate=${cEnd}`)
      } else if (preset && preset !== 'custom') {
        const range = getDatePreset(preset)
        if (range) {
          params.push(`startDate=${formatDateParam(range.start)}`, `endDate=${formatDateParam(range.end)}`)
        }
      }

      if (params.length) url += '?' + params.join('&')
      const { data: result } = await api.get(url)
      setData(result)
      setThreshold(result.lowStockThreshold || 10)
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to load analytics')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchDashboard(activePreset, customStart, customEnd)
  }, [])

  function handlePresetClick(preset) {
    setActivePreset(preset)
    fetchDashboard(preset, customStart, customEnd)
  }

  function handleCustomRange() {
    if (customStart && customEnd) {
      setActivePreset('custom')
      fetchDashboard('custom', customStart, customEnd)
    }
  }

  async function handleDownload(type, format) {
    const key = `${type}_${format}`
    setDownloading(key)
    try {
      let url = ''
      if (type === 'monthly') {
        url = `/api/analytics/reports/monthly/${format}?year=${reportYear}&month=${reportMonth}`
      } else {
        url = `/api/analytics/reports/yearly/${format}?year=${reportYear}`
      }
      const response = await api.get(url, { responseType: 'blob' })
      const blob = new Blob([response.data])
      const link = document.createElement('a')
      link.href = URL.createObjectURL(blob)
      const ext = format === 'pdf' ? 'pdf' : 'xlsx'
      link.download = `${type === 'monthly' ? 'MonthlySalesReport' : 'YearlySalesReport'}_${reportYear}${type === 'monthly' ? `_${String(reportMonth).padStart(2, '0')}` : ''}.${ext}`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(link.href)
    } catch (err) {
      console.error('Download failed', err)
    } finally {
      setDownloading(null)
    }
  }

  async function handleSaveThreshold() {
    setSavingThreshold(true)
    try {
      await api.put('/api/analytics/settings/low-stock-threshold', { threshold })
      fetchDashboard(activePreset, customStart, customEnd)
    } catch (err) {
      console.error('Failed to save threshold', err)
    } finally {
      setSavingThreshold(false)
    }
  }

  function resolveImg(url) {
    if (!url) return null
    return url.startsWith('http') ? url : `${baseURL}${url}`
  }

  const currency = data?.summary?.currencySymbol || '$'

  // ═══ Render ═══

  if (loading) {
    return (
      <div className="analytics-page">
        <div className="analytics-header">
          <button className="analytics-back-btn" onClick={onBack}>← {t('Back')}</button>
          <h1><ChartIcon /> {t('Store Analytics')}</h1>
        </div>
        <div className="analytics-loading">
          <div className="analytics-spinner" />
          <span>{t('Loading analytics...')}</span>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="analytics-page">
        <div className="analytics-header">
          <button className="analytics-back-btn" onClick={onBack}>← {t('Back')}</button>
          <h1><ChartIcon /> {t('Store Analytics')}</h1>
        </div>
        <div className="analytics-error">
          <p>{error}</p>
          <button className="analytics-back-btn" onClick={() => fetchDashboard(activePreset, customStart, customEnd)}>
            {t('Retry')}
          </button>
        </div>
      </div>
    )
  }

  if (!data) return null

  const { summary, dailySales, monthlySales, topSellingProducts, productPreviews,
    growth, trendingProducts, returningCustomers, ratingStats,
    lowStockProducts, aiRecommendations } = data

  const presets = [
    { key: 'today', label: t('Today') },
    { key: '7days', label: t('7 Days') },
    { key: '30days', label: t('30 Days') },
    { key: 'this_month', label: t('This Month') },
    { key: 'prev_month', label: t('Last Month') },
    { key: 'this_year', label: t('This Year') },
  ]

  const months = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ]

  return (
    <div className="analytics-page">
      {/* ── Header ── */}
      <div className="analytics-header">
        <button className="analytics-back-btn" onClick={onBack}>← {t('Back')}</button>
        <h1><ChartIcon /> {t('Store Analytics')}</h1>
      </div>

      {/* ── Date Range Bar ── */}
      <div className="date-range-bar">
        <label>{t('Period')}:</label>
        {presets.map(p => (
          <button key={p.key}
            className={`date-preset-btn ${activePreset === p.key ? 'active' : ''}`}
            onClick={() => handlePresetClick(p.key)}>
            {p.label}
          </button>
        ))}
        <div className="date-range-inputs">
          <input type="date" value={customStart} onChange={e => setCustomStart(e.target.value)} />
          <span style={{ color: 'var(--muted)' }}>→</span>
          <input type="date" value={customEnd} onChange={e => setCustomEnd(e.target.value)} />
          <button className="date-preset-btn" onClick={handleCustomRange}>{t('Apply')}</button>
        </div>
      </div>

      {/* ═══ Section 1: Summary Cards ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><DollarIcon /> {t('Overview')}</h2>
        <div className="summary-cards">
          <div className="summary-card">
            <div className="summary-card-icon"><DollarIcon /></div>
            <div className="summary-card-label">{t('Total Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.totalRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><CalendarIcon /></div>
            <div className="summary-card-label">{t("Today's Revenue")}</div>
            <div className="summary-card-value">{currency}{Number(summary.todayRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><CalendarIcon /></div>
            <div className="summary-card-label">{t('Monthly Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.monthlyRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><CalendarIcon /></div>
            <div className="summary-card-label">{t('Yearly Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.yearlyRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><ShoppingBagIcon /></div>
            <div className="summary-card-label">{t('Total Orders')}</div>
            <div className="summary-card-value">{summary.totalOrders.toLocaleString()}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><TargetIcon /></div>
            <div className="summary-card-label">{t('Avg Order Value')}</div>
            <div className="summary-card-value">{currency}{Number(summary.averageOrderValue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
        </div>
      </div>

      {/* ═══ Section 5: Growth Percentage ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><TrendUpIcon /> {t('Growth')}</h2>
        <div className="growth-cards">
          <div className="growth-card">
            <div className="growth-card-header">
              <span className="growth-card-label">{t('Revenue Growth')}</span>
              <span className={`growth-badge ${growth.revenueGrowthPercent > 0 ? 'positive' : growth.revenueGrowthPercent < 0 ? 'negative' : 'neutral'}`}>
                {growth.revenueGrowthPercent > 0 ? <TrendUpIcon /> : growth.revenueGrowthPercent < 0 ? <TrendDownIcon /> : null}
                {growth.revenueGrowthPercent > 0 ? '+' : ''}{growth.revenueGrowthPercent.toFixed(1)}%
              </span>
            </div>
            <div className="growth-card-values">
              <div className="growth-value-item">
                <span className="growth-value-label">{t('Previous Month')}</span>
                <span className="growth-value-amount">{currency}{Number(growth.previousMonthRevenue).toLocaleString()}</span>
              </div>
              <div className="growth-value-item">
                <span className="growth-value-label">{t('Current Month')}</span>
                <span className="growth-value-amount">{currency}{Number(growth.currentMonthRevenue).toLocaleString()}</span>
              </div>
            </div>
          </div>
          <div className="growth-card">
            <div className="growth-card-header">
              <span className="growth-card-label">{t('Order Growth')}</span>
              <span className={`growth-badge ${growth.orderGrowthPercent > 0 ? 'positive' : growth.orderGrowthPercent < 0 ? 'negative' : 'neutral'}`}>
                {growth.orderGrowthPercent > 0 ? <TrendUpIcon /> : growth.orderGrowthPercent < 0 ? <TrendDownIcon /> : null}
                {growth.orderGrowthPercent > 0 ? '+' : ''}{growth.orderGrowthPercent.toFixed(1)}%
              </span>
            </div>
            <div className="growth-card-values">
              <div className="growth-value-item">
                <span className="growth-value-label">{t('Previous Month')}</span>
                <span className="growth-value-amount">{growth.previousMonthOrders.toLocaleString()}</span>
              </div>
              <div className="growth-value-item">
                <span className="growth-value-label">{t('Current Month')}</span>
                <span className="growth-value-amount">{growth.currentMonthOrders.toLocaleString()}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ═══ Section 2: Sales Charts ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><ChartIcon /> {t('Sales Charts')}</h2>
        <div className="charts-grid">
          <div className="chart-card">
            <h3>{t('Daily Sales (Last 7 Days)')}</h3>
            <ResponsiveContainer width="100%" height={280}>
              <AreaChart data={dailySales}>
                <defs>
                  <linearGradient id="dailyGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: 'var(--muted)' }} />
                <YAxis tick={{ fontSize: 11, fill: 'var(--muted)' }} />
                <Tooltip content={<ChartTooltip currency={currency} />} />
                <Area type="monotone" dataKey="revenue" name="Revenue" stroke="#6366f1" fill="url(#dailyGrad)" strokeWidth={2.5} dot={{ r: 4, fill: '#6366f1' }} />
                <Line type="monotone" dataKey="orderCount" name="Orders" stroke="#f59e0b" strokeWidth={2} dot={{ r: 3, fill: '#f59e0b' }} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
          <div className="chart-card">
            <h3>{t('Monthly Sales (Last 12 Months)')}</h3>
            <ResponsiveContainer width="100%" height={280}>
              <AreaChart data={monthlySales}>
                <defs>
                  <linearGradient id="monthlyGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10b981" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="month" tick={{ fontSize: 10, fill: 'var(--muted)' }} />
                <YAxis tick={{ fontSize: 11, fill: 'var(--muted)' }} />
                <Tooltip content={<ChartTooltip currency={currency} />} />
                <Area type="monotone" dataKey="revenue" name="Revenue" stroke="#10b981" fill="url(#monthlyGrad)" strokeWidth={2.5} dot={{ r: 4, fill: '#10b981' }} />
                <Line type="monotone" dataKey="orderCount" name="Orders" stroke="#ec4899" strokeWidth={2} dot={{ r: 3, fill: '#ec4899' }} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* ═══ Section 3: Top Selling Products ═══ */}
      {topSellingProducts?.length > 0 && (
        <div className="analytics-section">
          <h2 className="analytics-section-title"><PackageIcon /> {t('Top Selling Products')}</h2>
          <div className="top-products-list">
            {topSellingProducts.map((p, i) => (
              <div key={p.productId} className="top-product-item">
                <div className={`top-product-rank ${i === 0 ? 'gold' : i === 1 ? 'silver' : i === 2 ? 'bronze' : 'default'}`}>
                  {i + 1}
                </div>
                {p.imageUrl ? (
                  <img className="top-product-img" src={resolveImg(p.imageUrl)} alt={p.productName} />
                ) : (
                  <div className="top-product-img-placeholder"><PackageIcon /></div>
                )}
                <div className="top-product-info">
                  <div className="top-product-name">{p.productName}</div>
                  <div className="top-product-stats">{p.quantitySold} {t('sold')}</div>
                </div>
                <div className="top-product-revenue">{currency}{Number(p.revenue).toLocaleString()}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ═══ Section 4: Product Preview ═══ */}
      {productPreviews?.length > 0 && (
        <div className="analytics-section">
          <h2 className="analytics-section-title"><PackageIcon /> {t('Product Overview')}</h2>
          <div className="product-preview-grid">
            {productPreviews.map(p => (
              <div key={p.productId} className="product-preview-card">
                {p.imageUrl ? (
                  <img className="product-preview-img" src={resolveImg(p.imageUrl)} alt={p.productName} />
                ) : (
                  <div className="product-preview-img" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <PackageIcon />
                  </div>
                )}
                <div className="product-preview-info">
                  <div className="product-preview-name">{p.productName}</div>
                  <div className="product-preview-stats">
                    <div className="product-preview-stat">
                      <span className="product-preview-stat-label">{t('Stock')}</span>
                      <span className="product-preview-stat-value" style={{ color: p.currentStock < threshold ? '#ef4444' : 'var(--text)' }}>
                        {p.currentStock}
                      </span>
                    </div>
                    <div className="product-preview-stat">
                      <span className="product-preview-stat-label">{t('Price')}</span>
                      <span className="product-preview-stat-value">{currency}{p.price}</span>
                    </div>
                    <div className="product-preview-stat">
                      <span className="product-preview-stat-label">{t('Sold')}</span>
                      <span className="product-preview-stat-value">{p.totalSoldCount}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ═══ Section 6: Trending Products ═══ */}
      {trendingProducts?.length > 0 && (
        <div className="analytics-section">
          <h2 className="analytics-section-title"><TrendUpIcon /> {t('Trending Products')}</h2>
          <div className="trending-list">
            {trendingProducts.map(p => (
              <div key={p.productId} className="trending-item">
                {p.imageUrl ? (
                  <img className="top-product-img" src={resolveImg(p.imageUrl)} alt={p.productName} />
                ) : (
                  <div className="top-product-img-placeholder"><PackageIcon /></div>
                )}
                <div className="top-product-info">
                  <div className="top-product-name">{p.productName}</div>
                  <div className="top-product-stats">{p.recentQuantity} {t('sold this week')} (was {p.previousQuantity})</div>
                </div>
                <span className="trending-badge">↑ {p.growthPercent.toFixed(0)}%</span>
                <span style={{ fontSize: '0.8rem', color: '#10b981', fontWeight: 600 }}>+{p.quantityIncrease}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ═══ Section 7: Returning Customers ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><UsersIcon /> {t('Returning Customers')}</h2>
        <div className="returning-customers-card">
          <CircularProgress percentage={Number(returningCustomers.returningPercentage)} />
          <div className="returning-stats">
            <div className="returning-stat">
              <span className="returning-stat-value">{returningCustomers.totalCustomers}</span>
              <span className="returning-stat-label">{t('Total Customers')}</span>
            </div>
            <div className="returning-stat">
              <span className="returning-stat-value">{returningCustomers.returningCustomers}</span>
              <span className="returning-stat-label">{t('Returning')}</span>
            </div>
          </div>
        </div>
      </div>

      {/* ═══ Section 8: Product Rating Statistics ═══ */}
      {ratingStats && (
        <div className="analytics-section">
          <h2 className="analytics-section-title"><StarIcon /> {t('Product Ratings')}</h2>
          <div className="ratings-card">
            <div className="ratings-overview">
              <span className="ratings-big-number">{ratingStats.overallAverageRating.toFixed(1)}</span>
              <div>
                <StarRating rating={ratingStats.overallAverageRating} size={22} />
                <div className="ratings-review-count">{ratingStats.totalReviews} {t('reviews')}</div>
              </div>
            </div>
            <div className="ratings-lists">
              <div>
                <h4 style={{ color: '#10b981' }}>⬆ {t('Highest Rated')}</h4>
                {ratingStats.highestRated.map(p => (
                  <div key={p.productId} className="rated-product-item">
                    <span className="rated-product-name">{p.productName}</span>
                    <span className="rated-product-rating" style={{ color: '#f59e0b' }}>
                      ★ {p.averageRating.toFixed(1)}
                      <span style={{ color: 'var(--muted)', fontWeight: 400, fontSize: '0.75rem', marginLeft: 4 }}>({p.reviewCount})</span>
                    </span>
                  </div>
                ))}
              </div>
              <div>
                <h4 style={{ color: '#ef4444' }}>⬇ {t('Lowest Rated')}</h4>
                {ratingStats.lowestRated.map(p => (
                  <div key={p.productId} className="rated-product-item">
                    <span className="rated-product-name">{p.productName}</span>
                    <span className="rated-product-rating" style={{ color: '#ef4444' }}>
                      ★ {p.averageRating.toFixed(1)}
                      <span style={{ color: 'var(--muted)', fontWeight: 400, fontSize: '0.75rem', marginLeft: 4 }}>({p.reviewCount})</span>
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ═══ Section 9: Low Stock Warnings ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><AlertIcon /> {t('Low Stock Warnings')}</h2>
        <div className="threshold-setting">
          <label>{t('Alert Threshold')}:</label>
          <input type="number" min="1" max="1000" value={threshold}
            onChange={e => setThreshold(parseInt(e.target.value) || 1)} />
          <button className="threshold-save-btn" onClick={handleSaveThreshold} disabled={savingThreshold}>
            {savingThreshold ? '...' : t('Save')}
          </button>
          <span style={{ fontSize: '0.75rem', color: 'var(--muted)' }}>
            {t('Products with stock below this number will be flagged')}
          </span>
        </div>
        {lowStockProducts?.length > 0 ? (
          <div className="low-stock-grid">
            {lowStockProducts.map(p => (
              <div key={p.productId} className="low-stock-item">
                {p.imageUrl ? (
                  <img className="low-stock-img" src={resolveImg(p.imageUrl)} alt={p.productName} />
                ) : (
                  <div className="low-stock-img" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <PackageIcon />
                  </div>
                )}
                <div className="low-stock-info">
                  <div className="low-stock-name">{p.productName}</div>
                  <div className="low-stock-qty">{p.remainingQuantity} {t('remaining')}</div>
                </div>
                <div className="low-stock-warning-icon"><AlertIcon /></div>
              </div>
            ))}
          </div>
        ) : (
          <div className="analytics-empty">{t('All products are well-stocked! 🎉')}</div>
        )}
      </div>

      {/* ═══ Section 10: AI Recommendations ═══ */}
      {aiRecommendations?.length > 0 && (
        <div className="analytics-section">
          <h2 className="analytics-section-title"><LightbulbIcon /> {t('AI Recommendations')}</h2>
          <div className="ai-recs-grid">
            {aiRecommendations.map((rec, i) => (
              <div key={i} className="ai-rec-card" data-type={rec.type}>
                <div className="ai-rec-icon">
                  {rec.type === 'sales_up' && <TrendUpIcon />}
                  {rec.type === 'sales_down' && <TrendDownIcon />}
                  {rec.type === 'low_stock' && <AlertIcon />}
                  {rec.type === 'rating_high' && <StarIcon />}
                  {rec.type === 'rating_low' && <StarIcon />}
                  {rec.type === 'stock' && <PackageIcon />}
                </div>
                <div className="ai-rec-message">{rec.message}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ═══ Reports Section ═══ */}
      <div className="analytics-section">
        <h2 className="analytics-section-title"><DownloadIcon /> {t('Reports & Export')}</h2>
        <div className="reports-section">
          <div className="reports-tabs">
            <button className={`reports-tab ${reportTab === 'monthly' ? 'active' : ''}`}
              onClick={() => setReportTab('monthly')}>
              {t('Monthly Report')}
            </button>
            <button className={`reports-tab ${reportTab === 'yearly' ? 'active' : ''}`}
              onClick={() => setReportTab('yearly')}>
              {t('Yearly Report')}
            </button>
          </div>

          <div className="reports-controls">
            <select value={reportYear} onChange={e => setReportYear(parseInt(e.target.value))}>
              {Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i).map(y => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>

            {reportTab === 'monthly' && (
              <select value={reportMonth} onChange={e => setReportMonth(parseInt(e.target.value))}>
                {months.map((m, i) => (
                  <option key={i} value={i + 1}>{t(m)}</option>
                ))}
              </select>
            )}

            <div className="download-btns">
              <button className="download-btn pdf"
                onClick={() => handleDownload(reportTab, 'pdf')}
                disabled={downloading !== null}>
                <DownloadIcon />
                {downloading === `${reportTab}_pdf` ? t('Downloading...') : t('PDF')}
              </button>
              <button className="download-btn excel"
                onClick={() => handleDownload(reportTab, 'excel')}
                disabled={downloading !== null}>
                <DownloadIcon />
                {downloading === `${reportTab}_excel` ? t('Downloading...') : t('Excel')}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
