import { useState, useEffect, useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { api } from '../apiClient.js'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Area, AreaChart } from 'recharts'
import AnimationOverlay from '../components/AnimationOverlay.jsx'
import successAnimation from '../assets/lottieflow-success-01-000000-easey.json'
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
const EditIcon = ({ size = 14 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
    <path d="M18.5 2.5a2.121 2.121 0 1 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
  </svg>
)


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
  const { t } = useTranslation()
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
        <span className="returning-circle-label">{t('Returning')}</span>
      </div>
    </div>
  )
}

// ═══════════════════════════════════════════════════
//  AI Recommendation translation helper
// ═══════════════════════════════════════════════════

function getTranslatedRecommendation(rec, t) {
  if (!rec || !rec.message) return ''
  
  let match = rec.message.match(/^"(.+)" demand increased by (\d+)% in the last 7 days\. Consider increasing stock\.$/)
  if (match) {
    return t('rec_demand_increased', { name: match[1], percent: match[2] })
  }

  match = rec.message.match(/^"(.+)" has very low stock \((\d+) remaining\)\. Restock soon to avoid lost sales\.$/)
  if (match) {
    return t('rec_low_stock', { name: match[1], remaining: match[2] })
  }

  match = rec.message.match(/^"(.+)" has excellent ratings \((\d+(?:\.\d+)?)★ from (\d+) reviews\)\. Feature it prominently\.$/)
  if (match) {
    return t('rec_excellent_ratings', { name: match[1], rating: match[2], reviews: match[3] })
  }

  match = rec.message.match(/^"(.+)" has low ratings \((\d+(?:\.\d+)?)★\)\. Consider improving quality or updating the product\.$/)
  if (match) {
    return t('rec_low_ratings', { name: match[1], rating: match[2] })
  }

  match = rec.message.match(/^Revenue decreased by (\d+)% compared to the previous period\. Consider running promotions\.$/)
  if (match) {
    return t('rec_revenue_decreased', { percent: match[1] })
  }

  match = rec.message.match(/^Monthly revenue grew by (\d+)%! Great performance — consider expanding your product range\.$/)
  if (match) {
    return t('rec_revenue_grew', { percent: match[1] })
  }

  match = rec.message.match(/^"(.+)" is your best seller with (\d+) units sold \(([^0-9.]+)([\d,]+) revenue\)\. Ensure adequate stock\.$/)
  if (match) {
    return t('rec_best_seller', { name: match[1], units: match[2], currency: match[3], revenue: match[4] })
  }

  return rec.message
}

// ═══════════════════════════════════════════════════
//  Main Component
// ═══════════════════════════════════════════════════

export default function StoreAnalyticsPage({ onBack }) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
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
  const [thresholdSavedMsg, setThresholdSavedMsg] = useState(null)
  const [thresholdErrorMsg, setThresholdErrorMsg] = useState(null)

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
      const lang = i18n?.language || 'en'
      let url = ''
      if (type === 'monthly') {
        url = `/api/analytics/reports/monthly/${format}?year=${reportYear}&month=${reportMonth}&lang=${lang}`
      } else {
        url = `/api/analytics/reports/yearly/${format}?year=${reportYear}&lang=${lang}`
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
    setThresholdSavedMsg(null)
    setThresholdErrorMsg(null)
    try {
      await api.put('/api/analytics/settings/low-stock-threshold', { threshold })
      setThresholdSavedMsg(t('Threshold updated successfully!'))
      setTimeout(() => setThresholdSavedMsg(null), 3000)
      await fetchDashboard(activePreset, customStart, customEnd)
    } catch (err) {
      console.error('Failed to save threshold', err)
      setThresholdErrorMsg(err?.response?.data?.error || err?.message || t('Failed to save threshold'))
      setTimeout(() => setThresholdErrorMsg(null), 4000)
    } finally {
      setSavingThreshold(false)
    }
  }

  function resolveImg(url) {
    if (!url) return null
    return url.startsWith('http') ? url : `${baseURL}${url}`
  }

  // ═══════════════════════════════════════════════════
  //  Telegram State & Handlers
  // ═══════════════════════════════════════════════════

  const [tgConnected, setTgConnected] = useState(false)
  const [tgMaskedId, setTgMaskedId] = useState(null)
  const [tgConnectedAt, setTgConnectedAt] = useState(null)
  const [tgChatIdInput, setTgChatIdInput] = useState('')
  const [tgConnecting, setTgConnecting] = useState(false)
  const [tgDisconnecting, setTgDisconnecting] = useState(false)
  const [tgShowConnect, setTgShowConnect] = useState(false)
  const [tgMessage, setTgMessage] = useState(null) // { type: 'success'|'error', text }
  const [tgNotifications, setTgNotifications] = useState([])
  const [tgSaving, setTgSaving] = useState(false)
  const [tgLoaded, setTgLoaded] = useState(false)
  const [tgTesting, setTgTesting] = useState(false)
  const [tgOpen, setTgOpen] = useState(false)
  const [tgSaveSuccess, setTgSaveSuccess] = useState(false)

  const NOTIFICATION_TYPES = [
    { key: 'NewCustomer', emoji: '🎉', label: 'New Customer Registration', desc: 'When a new customer signs up', category: 'events' },
    { key: 'NewOrder', emoji: '🛒', label: 'New Order Received', desc: 'When a new order is placed', category: 'events' },
    { key: 'OrderDelivered', emoji: '✅', label: 'Order Delivered', desc: 'When an order is marked delivered', category: 'events' },
    { key: 'LowStock', emoji: '⚠️', label: 'Low Stock Warning', desc: 'When product stock falls below threshold', category: 'events' },
    { key: 'NegativeReview', emoji: '👎', label: 'Negative Review Alert', desc: 'When a product receives a low rating', category: 'events' },
    { key: 'DailyIncome', emoji: '📊', label: 'Daily Income Report', desc: 'Daily revenue, orders & top seller', category: 'scheduled' },
    { key: 'WeeklyReport', emoji: '📈', label: 'Weekly Income Report', desc: 'Weekly summary with growth %', category: 'scheduled' },
    { key: 'MonthlyReport', emoji: '📅', label: 'Monthly Income Report', desc: 'Full monthly business report', category: 'scheduled' },
    { key: 'RevenueGoal', emoji: '🎯', label: 'Revenue Goal Reached', desc: 'When revenue milestone is hit', category: 'events' },
    { key: 'ReturningCustomer', emoji: '🔄', label: 'Returning Customer Activity', desc: 'When returning customers trend changes', category: 'events' },
    { key: 'TopProducts', emoji: '🏆', label: 'Top Selling Products Report', desc: 'Weekly top 5 products ranking', category: 'scheduled' },
    { key: 'AiInsights', emoji: '🤖', label: 'AI Business Insights', desc: 'AI-powered business recommendations', category: 'scheduled' },
  ]

  const SCHEDULED_TYPES = new Set(['DailyIncome', 'WeeklyReport', 'MonthlyReport', 'TopProducts', 'AiInsights'])
  const FREQUENCIES = [
    { value: 'Daily', label: 'Every Day' },
    { value: 'Every2Days', label: 'Every 2 Days' },
    { value: 'Every3Days', label: 'Every 3 Days' },
    { value: 'Weekly', label: 'Every Week' },
    { value: 'Monthly', label: 'Every Month' },
    { value: 'Custom', label: 'Custom Interval' },
  ]
  const DAYS_OF_WEEK = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
  const TIMEZONES = [
    'UTC', 'Europe/Istanbul', 'Europe/London', 'Europe/Berlin', 'Europe/Paris',
    'America/New_York', 'America/Chicago', 'America/Los_Angeles',
    'Asia/Baghdad', 'Asia/Dubai', 'Asia/Riyadh', 'Asia/Tokyo', 'Asia/Shanghai',
    'Australia/Sydney', 'Africa/Cairo'
  ]

  // Load Telegram settings
  useEffect(() => {
    async function loadTelegram() {
      try {
        const { data: res } = await api.get('/api/telegram/settings')
        setTgConnected(res.connection?.connected || false)
        setTgMaskedId(res.connection?.maskedChatId || null)
        setTgConnectedAt(res.connection?.connectedAt || null)
        setTgNotifications(res.notifications || [])
      } catch (err) {
        // Telegram not configured yet — that's fine
        console.log('Telegram settings not loaded:', err.message)
      } finally {
        setTgLoaded(true)
      }
    }
    loadTelegram()
  }, [])

  async function handleTgConnect() {
    if (!tgChatIdInput.trim()) return
    setTgConnecting(true)
    setTgMessage(null)
    try {
      const { data: res } = await api.post('/api/telegram/connect', { chatId: parseInt(tgChatIdInput) })
      setTgConnected(true)
      setTgMaskedId(res.maskedChatId)
      setTgConnectedAt(res.connectedAt)
      setTgShowConnect(false)
      setTgChatIdInput('')
      setTgMessage({ type: 'success', text: t('Telegram bot connected successfully!') })
      // Reload settings
      const { data: settings } = await api.get('/api/telegram/settings')
      setTgNotifications(settings.notifications || [])
    } catch (err) {
      setTgMessage({ type: 'error', text: err.response?.data?.error || t('Failed to connect Telegram bot') })
    } finally {
      setTgConnecting(false)
    }
  }

  async function handleTgDisconnect() {
    setTgDisconnecting(true)
    setTgMessage(null)
    try {
      await api.post('/api/telegram/disconnect')
      setTgConnected(false)
      setTgMaskedId(null)
      setTgMessage({ type: 'success', text: t('Telegram bot disconnected.') })
    } catch (err) {
      setTgMessage({ type: 'error', text: t('Failed to disconnect') })
    } finally {
      setTgDisconnecting(false)
    }
  }

  async function handleTgTest() {
    setTgTesting(true)
    setTgMessage(null)
    try {
      await api.post('/api/telegram/test')
      setTgMessage({ type: 'success', text: t('Test message sent successfully!') })
    } catch (err) {
      setTgMessage({ type: 'error', text: err.response?.data?.error || t('Failed to send test message') })
    } finally {
      setTgTesting(false)
    }
  }

  function handleTgToggle(notifType) {
    setTgNotifications(prev => prev.map(n =>
      n.notificationType === notifType ? { ...n, enabled: !n.enabled } : n
    ))
  }

  function handleTgScheduleChange(notifType, field, value) {
    setTgNotifications(prev => prev.map(n =>
      n.notificationType === notifType ? { ...n, [field]: value } : n
    ))
  }

  const get12HourFormat = (hour24) => {
    const isPm = hour24 >= 12
    let h12 = hour24 % 12
    if (h12 === 0) h12 = 12
    return { h12, isPm }
  }

  const get24Hour = (h12, isPm) => {
    if (isPm) {
      return h12 === 12 ? 12 : h12 + 12
    } else {
      return h12 === 12 ? 0 : h12
    }
  }

  function adjustTime(notifType, unit, dir) {
    setTgNotifications(prev => prev.map(n => {
      if (n.notificationType !== notifType) return n

      const currentHour = n.hour ?? 9
      const currentMinute = n.minute ?? 0
      const { h12, isPm } = get12HourFormat(currentHour)

      if (unit === 'h') {
        const nextH12 = dir === 'up' ? (h12 % 12) + 1 : (h12 - 1 < 1 ? 12 : h12 - 1)
        const nextHour = get24Hour(nextH12, isPm)
        return { ...n, hour: nextHour }
      } else {
        const nextMinute = dir === 'up' ? (currentMinute + 1) % 60 : (currentMinute - 1 < 0 ? 59 : currentMinute - 1)
        return { ...n, minute: nextMinute }
      }
    }))
  }

  function handleAmPmChange(notifType, newPeriod) {
    setTgNotifications(prev => prev.map(n => {
      if (n.notificationType !== notifType) return n

      const currentHour = n.hour ?? 9
      const isPm = currentHour >= 12
      const isNewPm = newPeriod === 'PM'

      if (isPm === isNewPm) return n // no change

      const nextHour = isNewPm ? currentHour + 12 : currentHour - 12
      return { ...n, hour: nextHour }
    }))
  }

  async function handleTgSave() {
    setTgSaving(true)
    setTgMessage(null)
    setTgSaveSuccess(false)
    try {
      await api.put('/api/telegram/settings', { settings: tgNotifications })
      setTgSaveSuccess(true)
    } catch (err) {
      setTgMessage({ type: 'error', text: err.response?.data?.error || t('Failed to save settings') })
    } finally {
      setTgSaving(false)
    }
  }

  function getTgNotif(type) {
    return tgNotifications.find(n => n.notificationType === type) || {
      notificationType: type, enabled: false, frequency: 'Daily',
      intervalDays: null, weeklyDay: null, hour: 9, minute: 0, timeZone: 'UTC'
    }
  }

  const currency = data?.summary?.currencySymbol || '$'

  const activeLowStockProducts = useMemo(() => {
    if (!data) return []
    const previews = data.productPreviews
    const lowStock = data.lowStockProducts
    if (!previews || previews.length === 0) return lowStock || []
    return previews
      .filter(p => p.currentStock <= threshold)
      .map(p => ({
        productId: p.productId,
        productName: p.productName,
        imageUrl: p.imageUrl,
        remainingQuantity: p.currentStock,
        threshold: threshold
      }))
  }, [data, threshold])

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
            <div className="summary-card-label">{t('Avg Daily Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.averageDailyRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><CalendarIcon /></div>
            <div className="summary-card-label">{t('Avg Monthly Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.averageMonthlyRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="summary-card">
            <div className="summary-card-icon"><CalendarIcon /></div>
            <div className="summary-card-label">{t('Peak Day Revenue')}</div>
            <div className="summary-card-value">{currency}{Number(summary.peakDayRevenue).toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
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
            {t('Products with stock below or equal to this number will be flagged')}
          </span>
        </div>

        {thresholdSavedMsg && (
          <div style={{ margin: '10px 0', padding: '10px 14px', background: 'rgba(16, 185, 129, 0.12)', border: '1px solid #10b981', borderRadius: '8px', color: '#10b981', fontSize: '13px', fontWeight: 600 }}>
            ✅ {thresholdSavedMsg}
          </div>
        )}
        {thresholdErrorMsg && (
          <div style={{ margin: '10px 0', padding: '10px 14px', background: 'rgba(239, 68, 68, 0.12)', border: '1px solid #ef4444', borderRadius: '8px', color: '#ef4444', fontSize: '13px', fontWeight: 600 }}>
            ⚠️ {thresholdErrorMsg}
          </div>
        )}

        {activeLowStockProducts?.length > 0 ? (
          <div className="low-stock-grid">
            {activeLowStockProducts.map(p => (
              <div
                key={p.productId}
                className="low-stock-item"
                style={{ cursor: 'pointer' }}
                onClick={() => navigate(`/store-products?editProductId=${p.productId}`)}
              >
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
                <button
                  className="low-stock-edit-btn"
                  onClick={(e) => {
                    e.stopPropagation()
                    navigate(`/store-products?editProductId=${p.productId}`)
                  }}
                  style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
                >
                  <EditIcon size={14} />
                  <span>{t('Edit')}</span>
                </button>
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
                <div className="ai-rec-message">{getTranslatedRecommendation(rec, t)}</div>
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

      {/* ═══ Section 12: Telegram Reports & Notifications ═══ */}
      {tgLoaded && (
        <div className="analytics-section collapsible-telegram-section" id="telegram-section">
          <div className="telegram-section-header" onClick={() => setTgOpen(!tgOpen)}>
            <div className="telegram-section-title-group">
              <h2 className="analytics-section-title">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21.198 2.433a2.242 2.242 0 0 0-1.022.215l-16.5 7.5a1.5 1.5 0 0 0 .164 2.785l5.61 1.498a1 1 0 0 0 .85-.2l6.3-5.4-4.2 5.9a1 1 0 0 0-.1.9l2.1 5.1a1.5 1.5 0 0 0 2.8-.3l3.9-16.5a2.25 2.25 0 0 0-1.902-1.498z"></path></svg>
                {t('Telegram Reports & Notifications')}
              </h2>
              <p className="telegram-section-subtitle">
                {t('Receive automated reports and alerts on Telegram')}
              </p>
            </div>
            <button className="telegram-toggle-btn" aria-label={tgOpen ? t('Hide Settings') : t('Configure Settings')}>
              {tgOpen ? t('Hide') : t('Configure')}
              <svg className={`toggle-chevron ${tgOpen ? 'open' : ''}`} width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="6 9 12 15 18 9"></polyline>
              </svg>
            </button>
          </div>

          {tgOpen && (
            <div className="telegram-section-content">
              {/* ── Connection Card ── */}
              <div className="telegram-connection-card">
                <div className="telegram-header-row">
                  <div className="telegram-bot-icon">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="22" y1="2" x2="11" y2="13"></line>
                      <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
                    </svg>
                  </div>
                  <div className="telegram-header-text">
                    <h3>{t('Telegram Bot')}</h3>
                    <p>{t('Receive automated reports and alerts on Telegram')}</p>
                  </div>
                </div>

                <div className={`telegram-status-badge ${tgConnected ? 'connected' : 'disconnected'}`}>
                  <div className="telegram-status-dot" />
                  {tgConnected ? t('Connected') : t('Disconnected')}
                </div>

                {tgConnected && tgMaskedId && (
                  <div className="telegram-chat-id">
                    {t('Chat ID')}: <span>{tgMaskedId}</span>
                  </div>
                )}

                <div className="telegram-actions">
                  {tgConnected ? (
                    <>
                      <button className="telegram-btn secondary" onClick={() => { setTgShowConnect(true); setTgMessage(null) }}>
                        🔄 {t('Reconnect')}
                      </button>
                      <button className="telegram-btn secondary" onClick={handleTgTest} disabled={tgTesting}>
                        🔔 {tgTesting ? t('Sending...') : t('Test Message')}
                      </button>
                      <button className="telegram-btn danger" onClick={handleTgDisconnect} disabled={tgDisconnecting}>
                        ✕ {tgDisconnecting ? t('Disconnecting...') : t('Disconnect')}
                      </button>
                    </>
                  ) : (
                    <button className="telegram-btn primary" onClick={() => { setTgShowConnect(true); setTgMessage(null) }}>
                      ✈️ {t('Connect Telegram Bot')}
                    </button>
                  )}
                </div>

                {/* Connect Form */}
                {tgShowConnect && (
                  <>
                    <div className="telegram-connect-form">
                      <div className="form-group">
                        <label>{t('Telegram Chat ID')}</label>
                        <input
                          type="text"
                          placeholder={t('Enter your Chat ID')}
                          value={tgChatIdInput}
                          onChange={e => setTgChatIdInput(e.target.value)}
                          onKeyDown={e => e.key === 'Enter' && handleTgConnect()}
                        />
                      </div>
                      <button className="telegram-btn primary" onClick={handleTgConnect} disabled={tgConnecting || !tgChatIdInput.trim()}>
                        {tgConnecting ? t('Connecting...') : t('Connect')}
                      </button>
                      <button className="telegram-btn secondary" onClick={() => setTgShowConnect(false)}>
                        {t('Cancel')}
                      </button>
                    </div>
                    <div className="telegram-instructions">
                      <strong>{t('How to get your Chat ID')}:</strong>
                      <ol>
                        <li>{t('Open Telegram and search for')} <code>@userinfobot</code></li>
                        <li>{t('Start a chat and it will show your Chat ID')}</li>
                        <li>{t('Copy the ID number and paste it above')}</li>
                        <li>{t('Then search for your store bot and press')} <code>/start</code></li>
                      </ol>
                    </div>
                  </>
                )}

                {/* Status Message (Errors Only) */}
                {tgMessage && tgMessage.type === 'error' && (
                  <div className="telegram-message error">
                    ❌ {tgMessage.text}
                  </div>
                )}
              </div>

              {/* ── Notification Categories ── */}
              {tgConnected && (
                <>
                  {/* Event-Driven Notifications */}
                  <div className="telegram-category">
                    <div className="telegram-category-label">⚡ {t('Real-Time Event Notifications')}</div>
                    <div className="telegram-notifications-grid">
                      {NOTIFICATION_TYPES.filter(n => n.category === 'events').map(nt => {
                        const notif = getTgNotif(nt.key)
                        return (
                          <div key={nt.key} className={`telegram-notif-card ${notif.enabled ? 'enabled' : ''}`}>
                            <div className="telegram-notif-info">
                              <div className="telegram-notif-emoji">{nt.emoji}</div>
                              <div className="telegram-notif-text">
                                <h4>{t(nt.label)}</h4>
                                <p>{t(nt.desc)}</p>
                              </div>
                            </div>
                            <label className="toggle-switch">
                              <input type="checkbox" checked={notif.enabled} onChange={() => handleTgToggle(nt.key)} />
                              <span className="toggle-slider" />
                            </label>
                          </div>
                        )
                      })}
                    </div>
                  </div>

                  {/* Scheduled Reports */}
                  <div className="telegram-category">
                    <div className="telegram-category-label">📅 {t('Scheduled Reports')}</div>
                    <div className="telegram-notifications-grid">
                      {NOTIFICATION_TYPES.filter(n => n.category === 'scheduled').map(nt => {
                        const notif = getTgNotif(nt.key)
                        return (
                          <div key={nt.key} className={`telegram-notif-card ${notif.enabled ? 'enabled' : ''}`}>
                            <div className="telegram-notif-info">
                              <div className="telegram-notif-emoji">{nt.emoji}</div>
                              <div className="telegram-notif-text">
                                <h4>{t(nt.label)}</h4>
                                <p>{t(nt.desc)}</p>
                              </div>
                            </div>
                            <label className="toggle-switch">
                              <input type="checkbox" checked={notif.enabled} onChange={() => handleTgToggle(nt.key)} />
                              <span className="toggle-slider" />
                            </label>
                          </div>
                        )
                      })}
                    </div>
                  </div>

                  {/* Schedule Configuration for enabled scheduled notifications */}
                  {tgNotifications.some(n => SCHEDULED_TYPES.has(n.notificationType) && n.enabled) && (
                    <div className="telegram-schedule-section">
                      <div className="telegram-schedule-title">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={{ marginRight: '8px' }}>
                          <circle cx="12" cy="12" r="10" /><polyline points="12 6 12 12 16 14" />
                        </svg>
                        {t('Schedule configuration')}
                      </div>
                      <div className="telegram-schedule-grid">
                        {NOTIFICATION_TYPES.filter(nt => SCHEDULED_TYPES.has(nt.key) && getTgNotif(nt.key).enabled).map(nt => {
                          const notif = getTgNotif(nt.key)
                          const { h12, isPm } = get12HourFormat(notif.hour)
                          return (
                            <div key={nt.key} className="telegram-schedule-card">
                              <div className="telegram-card-title">
                                <div className="telegram-card-title-icon">
                                  <span>{nt.emoji}</span>
                                </div>
                                {t(nt.label)}
                              </div>

                              {/* Frequency */}
                              <div className="telegram-field">
                                <label>{t('Frequency')}</label>
                                <select
                                  value={notif.frequency}
                                  onChange={e => handleTgScheduleChange(nt.key, 'frequency', e.target.value)}
                                >
                                  {FREQUENCIES.map(f => (
                                    <option key={f.value} value={f.value}>{t(f.label)}</option>
                                  ))}
                                </select>
                              </div>

                              {/* Custom Interval */}
                              {notif.frequency === 'Custom' && (
                                <div className="telegram-field">
                                  <label>{t('Send every X days')}</label>
                                  <input
                                    type="number"
                                    min="1"
                                    max="90"
                                    value={notif.intervalDays || 1}
                                    onChange={e => handleTgScheduleChange(nt.key, 'intervalDays', parseInt(e.target.value) || 1)}
                                  />
                                </div>
                              )}

                              {/* Weekly day */}
                              {notif.frequency === 'Weekly' && (
                                <div className="telegram-field">
                                  <label>{t('Day')}</label>
                                  <select
                                    value={notif.weeklyDay ?? 0}
                                    onChange={e => handleTgScheduleChange(nt.key, 'weeklyDay', parseInt(e.target.value))}
                                  >
                                    {DAYS_OF_WEEK.map((d, i) => (
                                      <option key={i} value={i}>{t(d)}</option>
                                    ))}
                                  </select>
                                </div>
                              )}

                              {/* Time + Timezone Row */}
                              <div className="telegram-field-row">
                                <div className="telegram-field">
                                  <label>{t('Time')}</label>
                                  <div className="telegram-time-picker" role="group" aria-label="Time picker">
                                    {/* Hours */}
                                    <div className="telegram-time-part">
                                      <button type="button" onClick={() => adjustTime(nt.key, 'h', 'up')} aria-label={t('Increase hour')}>
                                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                          <polyline points="18 15 12 9 6 15" />
                                        </svg>
                                      </button>
                                      <span className="telegram-time-value" aria-live="polite" aria-label={t('Hours')}>
                                        {String(h12).padStart(2, '0')}
                                      </span>
                                      <button type="button" onClick={() => adjustTime(nt.key, 'h', 'down')} aria-label={t('Decrease hour')}>
                                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                          <polyline points="6 9 12 15 18 9" />
                                        </svg>
                                      </button>
                                    </div>

                                    <span className="telegram-time-colon">:</span>

                                    {/* Minutes */}
                                    <div className="telegram-time-part">
                                      <button type="button" onClick={() => adjustTime(nt.key, 'm', 'up')} aria-label={t('Increase minute')}>
                                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                          <polyline points="18 15 12 9 6 15" />
                                        </svg>
                                      </button>
                                      <span className="telegram-time-value" aria-live="polite" aria-label={t('Minutes')}>
                                        {String(notif.minute).padStart(2, '0')}
                                      </span>
                                      <button type="button" onClick={() => adjustTime(nt.key, 'm', 'down')} aria-label={t('Decrease minute')}>
                                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                          <polyline points="6 9 12 15 18 9" />
                                        </svg>
                                      </button>
                                    </div>

                                    {/* AM / PM */}
                                    <div className="telegram-ampm-group" role="group" aria-label={t('AM or PM')}>
                                      <button
                                        type="button"
                                        className={`telegram-ampm-btn ${!isPm ? 'active' : ''}`}
                                        onClick={() => handleAmPmChange(nt.key, 'AM')}
                                      >
                                        AM
                                      </button>
                                      <button
                                        type="button"
                                        className={`telegram-ampm-btn ${isPm ? 'active' : ''}`}
                                        onClick={() => handleAmPmChange(nt.key, 'PM')}
                                      >
                                        PM
                                      </button>
                                    </div>
                                  </div>
                                </div>

                                {/* Timezone */}
                                <div className="telegram-field">
                                  <label>{t('Timezone')}</label>
                                  <select
                                    value={notif.timeZone}
                                    onChange={e => handleTgScheduleChange(nt.key, 'timeZone', e.target.value)}
                                  >
                                    {TIMEZONES.map(tz => (
                                      <option key={tz} value={tz}>{tz}</option>
                                    ))}
                                  </select>
                                </div>
                              </div>
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )}

                  {/* Save Bar */}
                  <div className="telegram-save-bar">
                    <button className="telegram-btn primary" onClick={handleTgSave} disabled={tgSaving}>
                      {tgSaving ? t('Saving...') : t('💾 Save Settings')}
                    </button>
                  </div>

                  {/* Status Message at bottom (Errors Only) */}
                  {tgMessage && tgMessage.type === 'error' && (
                    <div className="telegram-message error">
                      ❌ {tgMessage.text}
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>
      )}

      {/* Success Modal Overlay */}
      {tgSaveSuccess && (
        <AnimationOverlay
          animationData={successAnimation}
          onComplete={() => setTgSaveSuccess(false)}
          duration={1500}
        />
      )}

    </div>
  )
}
