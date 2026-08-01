import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import Skeleton from '../components/Skeleton.jsx'
import * as signalR from '@microsoft/signalr'
import DeliverySimulationMap from '../components/DeliverySimulationMap.jsx'
import './DeliveryJobsPage.css'

const NORMALIZE_STATUS_MAP = {
    assigned: 'Assigned',
    pickedup: 'PickedUp',
    picked_up: 'PickedUp',
    ontheway: 'OnTheWay',
    on_the_way: 'OnTheWay',
    delivered: 'Delivered',
    unassigned: 'Unassigned'
}

function normalizeToPascal(raw) {
    if (!raw) return raw
    const key = raw.toLowerCase().replace(/\s+/g, '')
    return NORMALIZE_STATUS_MAP[key] || raw
}

export default function DeliveryJobsPage({ onBack }) {
    const { t } = useTranslation()
    const [tab, setTab] = useState('available')
    const [availableJobs, setAvailableJobs] = useState([])
    const [myJobs, setMyJobs] = useState([])
    const [loading, setLoading] = useState(true)
    const [simJob, setSimJob] = useState(null)
    const trackingConnRef = useRef(null)

    useEffect(() => {
        const token = localStorage.getItem('token')
        if (!token) return

        const baseURL = import.meta.env.VITE_API_BASE_URL
        const conn = new signalR.HubConnectionBuilder()
            .withUrl(`${baseURL}/hubs/tracking?access_token=${token}`)
            .withAutomaticReconnect()
            .build()

        conn.start().catch(err => console.error('TrackingHub connect error:', err))
        trackingConnRef.current = conn

        return () => {
            conn.stop()
        }
    }, [])

    useEffect(() => {
        const conn = trackingConnRef.current
        if (!conn) return

        const handleStatusUpdate = (data) => {
            const orderGroupId = data.orderGroupId ?? data.OrderGroupId
            const status = normalizeToPascal(data.status ?? data.Status)

            if (!orderGroupId || !status) return

            const applyUpdate = prev => prev.map(job =>
                String(job.orderGroupId) === String(orderGroupId)
                    ? {
                        ...job,
                        status: job.status === 'Unassigned' ? job.status : status,
                        orderGroup: job.orderGroup ? { ...job.orderGroup, status } : job.orderGroup
                    }
                    : job
            )

            setMyJobs(applyUpdate)
            setAvailableJobs(applyUpdate)
            setSimJob(prev => (
                prev && String(prev.orderGroupId) === String(orderGroupId)
                    ? {
                        ...prev,
                        status: prev.status === 'Unassigned' ? prev.status : status,
                        orderGroup: prev.orderGroup ? { ...prev.orderGroup, status } : prev.orderGroup
                    }
                    : prev
            ))
        }

        conn.on('order:status_updated', handleStatusUpdate)

        return () => {
            conn.off('order:status_updated', handleStatusUpdate)
        }
    }, [])

    useEffect(() => {
        load()
    }, [tab])

    useEffect(() => {
        const conn = trackingConnRef.current
        if (!conn) return

        const activeJobs = myJobs.filter(job => job.orderGroupId && job.status !== 'Delivered')

        const joinGroups = async () => {
            while (conn.state === 'Connecting') {
                await new Promise(resolve => setTimeout(resolve, 100))
            }

            if (conn.state !== 'Connected') return

            for (const job of activeJobs) {
                await conn.invoke('JoinGroupedOrderGroup', job.orderGroupId).catch(() => {})
            }
        }

        joinGroups()

        return () => {
            if (conn.state === 'Connected') {
                activeJobs.forEach(job => {
                    conn.invoke('LeaveGroupedOrderGroup', job.orderGroupId).catch(() => {})
                })
            }
        }
    }, [myJobs])

    async function load() {
        setLoading(true)
        try {
            if (tab === 'available') {
                const { data } = await api.get('/api/delivery/available')
                setAvailableJobs(Array.isArray(data) ? data : [])
            } else {
                const { data } = await api.get('/api/delivery/my-jobs')
                setMyJobs(Array.isArray(data) ? data : [])
            }
        } catch {
            if (tab === 'available') setAvailableJobs([])
            else setMyJobs([])
        } finally {
            setLoading(false)
        }
    }

    async function acceptJob(jobId) {
        try {
            await api.post(`/api/delivery/${jobId}/accept`)
            const { data } = await api.get('/api/delivery/my-jobs')
            const jobs = Array.isArray(data) ? data : []
            setMyJobs(jobs)
            const accepted = jobs.find(job => job.id === jobId)
            if (accepted) {
                setSimJob(accepted)
                setTab('mine')
            }
            load()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        }
    }

    async function updateStatus(jobId, newStatus) {
        try {
            await api.put(`/api/delivery/${jobId}/status`, { status: newStatus })
            if (newStatus === 'Delivered' && simJob?.id === jobId) {
                setSimJob(null)
            }
            load()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        }
    }

    const nextStatus = status => ({
        Assigned: 'PickedUp',
        PickedUp: 'OnTheWay',
        OnTheWay: 'Delivered'
    }[status])

    const jobs = tab === 'available' ? availableJobs : myJobs

    return (
        <div className="delivery-jobs-page">
            <div className="dj-container">
                {/* Header */}
                <div className="dj-header">
                    <button onClick={onBack} className="dj-back-btn">
                        <span className="material-symbols-outlined">arrow_back</span>
                        {t('Back')}
                    </button>
                    <div className="dj-title-area">
                        <span className="material-symbols-outlined dj-title-icon">local_shipping</span>
                        <h2 className="dj-title">{t('Delivery Jobs')}</h2>
                    </div>
                    <button onClick={load} className="dj-refresh-btn">
                        <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>refresh</span>
                        {t('Refresh')}
                    </button>
                </div>

                {/* Navigation Tabs */}
                <div className="dj-tabs-row">
                    <button className={`dj-tab-btn ${tab === 'available' ? 'active' : ''}`} onClick={() => setTab('available')}>
                        <span>{t('Available Jobs')}</span>
                        {tab === 'available' && <span className="dj-tab-badge">{availableJobs.length}</span>}
                    </button>
                    <button className={`dj-tab-btn ${tab === 'mine' ? 'active' : ''}`} onClick={() => setTab('mine')}>
                        <span>{t('My Deliveries')}</span>
                        {tab === 'mine' && <span className="dj-tab-badge">{myJobs.length}</span>}
                    </button>
                </div>

                {/* Content List */}
                {loading ? (
                    <div className="dj-jobs-grid">
                        {Array.from({ length: 3 }).map((_, i) => (
                            <div key={i} className="dj-job-card">
                                <div className="dj-job-header">
                                    <Skeleton width="160px" height="24px" />
                                    <Skeleton width="90px" height="24px" borderRadius="12px" />
                                </div>
                                <Skeleton width="100%" height="60px" borderRadius="12px" />
                                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                                    <Skeleton width="120px" height="36px" borderRadius="8px" />
                                </div>
                            </div>
                        ))}
                    </div>
                ) : jobs.length === 0 ? (
                    <div className="dj-empty-state">
                        <span className="material-symbols-outlined dj-empty-icon">local_shipping</span>
                        <div>
                            {tab === 'available' ? t('No grouped deliveries available right now.') : t('No assigned jobs in your queue.')}
                        </div>
                    </div>
                ) : (
                    <div className="dj-jobs-grid">
                        {jobs.map(job => {
                            const group = job.orderGroup
                            const stores = group?.stores || []
                            const storeNames = stores.map(store => store.storeName || store.storeUserId).join(', ')

                            return (
                                <div key={job.id} className="dj-job-card">
                                    <div className="dj-job-header">
                                        <div className="dj-job-title-box">
                                            <h3 className="dj-job-id">{t('Job')} #{job.id}</h3>
                                            <span className="dj-job-sub">• {t('Group')} #{job.orderGroupId || group?.orderGroupId || job.orderId}</span>
                                        </div>
                                        <span className={`dj-badge dj-badge-${(job.status || 'unassigned').toLowerCase()}`}>
                                            {t(job.status)}
                                        </span>
                                    </div>

                                    {group && (
                                        <div className="dj-overview-info">
                                            <div className="dj-info-item">
                                                <span className="dj-info-label">{t('Pickup Stops')}</span>
                                                <span className="dj-info-value">{stores.length} {t('Stops')}</span>
                                            </div>
                                            <div className="dj-info-item">
                                                <span className="dj-info-label">{t('Total Payout')}</span>
                                                <span className="dj-info-value">{formatPrice(group.totalAmount || 0, stores[0]?.currencyCode || 'USD')}</span>
                                            </div>
                                            <div className="dj-info-item">
                                                <span className="dj-info-label">{t('Stores')}</span>
                                                <span className="dj-info-value" style={{ fontSize: '13px' }}>{storeNames || '—'}</span>
                                            </div>
                                            {group.deliveryAddress && (
                                                <div className="dj-info-item" style={{ gridColumn: '1 / -1' }}>
                                                    <span className="dj-info-label">{t('Delivery Address')}</span>
                                                    <div className="dj-info-value" style={{ lineHeight: '1.6', fontSize: '13px', marginTop: '4px' }}>
                                                        {group.deliveryAddress.includes(' | ') ? (
                                                            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px 12px' }}>
                                                                {group.deliveryAddress.split(' | ').map((part, idx) => (
                                                                    <span key={idx} style={{ background: 'var(--surface-container-high)', padding: '2px 8px', borderRadius: '6px', border: '1px solid var(--outline-variant)', fontSize: '12px' }}>
                                                                        {part}
                                                                    </span>
                                                                ))}
                                                            </div>
                                                        ) : (
                                                            group.deliveryAddress
                                                        )}
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    )}

                                    {group && stores.length > 0 && (
                                        <div className="dj-stores-list">
                                            {stores.map(store => (
                                                <div key={store.orderId} className="dj-store-row">
                                                    <div>
                                                        <div className="dj-store-name">{store.storeName || store.storeUserId}</div>
                                                        <div className="dj-store-loc">{store.storeLocation || t('Pickup location not set')}</div>
                                                    </div>
                                                    <span className="dj-badge dj-badge-assigned" style={{ fontSize: '11px' }}>{t(store.status)}</span>
                                                </div>
                                            ))}
                                        </div>
                                    )}

                                    <div className="dj-card-actions">
                                        {tab === 'available' && job.status === 'Unassigned' && (
                                            <button onClick={() => acceptJob(job.id)} className="dj-btn-primary">
                                                <span className="material-symbols-outlined">check_circle</span>
                                                {t('Accept Job')}
                                            </button>
                                        )}

                                        {tab === 'mine' && (
                                            <>
                                                {(job.status === 'Assigned' || job.status === 'PickedUp' || job.status === 'OnTheWay') && (
                                                    <button
                                                        onClick={() => setSimJob(job)}
                                                        className="dj-btn-map"
                                                    >
                                                        <span className="material-symbols-outlined">map</span>
                                                        {t('Open Route Map')}
                                                    </button>
                                                )}
                                                {nextStatus(job.status) && (
                                                    <button onClick={() => updateStatus(job.id, nextStatus(job.status))} className="dj-btn-primary">
                                                        <span className="material-symbols-outlined">local_shipping</span>
                                                        {t('Mark as')} {t(nextStatus(job.status))}
                                                    </button>
                                                )}
                                            </>
                                        )}
                                    </div>
                                </div>
                            )
                        })}
                    </div>
                )}

                {simJob && (
                    <DeliverySimulationMap
                        order={simJob}
                        hubConnection={trackingConnRef.current}
                        onClose={() => setSimJob(null)}
                    />
                )}
            </div>
        </div>
    )
}
