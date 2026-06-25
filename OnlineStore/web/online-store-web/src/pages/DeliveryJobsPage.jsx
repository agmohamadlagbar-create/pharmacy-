import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'
import * as signalR from '@microsoft/signalr'
import DeliverySimulationMap from '../components/DeliverySimulationMap.jsx'

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

    const statusColor = status => ({
        Unassigned: '#f59e0b',
        Assigned: '#3b82f6',
        PickedUp: '#8b5cf6',
        OnTheWay: '#06b6d4',
        Delivered: '#22c55e'
    }[status] || '#888')

    const nextStatus = status => ({
        Assigned: 'PickedUp',
        PickedUp: 'OnTheWay',
        OnTheWay: 'Delivered'
    }[status])

    const jobs = tab === 'available' ? availableJobs : myJobs

    return (
        <div style={{ padding: '20px', maxWidth: '900px', margin: '0 auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
                <button onClick={onBack} className="glass-btn">← {t('Back')}</button>
                <h2 style={{ margin: 0, color: 'var(--text-primary)' }}>{t('Delivery Jobs')}</h2>
                <button onClick={load} className="glass-btn">↻</button>
            </div>

            <div className="seg" style={{ marginBottom: '20px' }}>
                <button className={tab === 'available' ? 'active' : ''} onClick={() => setTab('available')}>{t('Available')}</button>
                <button className={tab === 'mine' ? 'active' : ''} onClick={() => setTab('mine')}>{t('My Jobs')}</button>
            </div>

            {loading ? (
                <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '40px' }}>{t('Loading...')}</div>
            ) : jobs.length === 0 ? (
                <div style={{ textAlign: 'center', color: 'var(--muted)', padding: '40px' }}>
                    {tab === 'available' ? t('No grouped deliveries available right now.') : t('No assigned jobs.')}
                </div>
            ) : (
                <div style={{ display: 'grid', gap: '12px' }}>
                    {jobs.map(job => {
                        const group = job.orderGroup
                        const stores = group?.stores || []
                        const storeNames = stores.map(store => store.storeName || store.storeUserId).join(', ')

                        return (
                            <div key={job.id} style={{ background: 'var(--card)', backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)', borderRadius: '12px', padding: '16px', border: '1px solid var(--border)', boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px', gap: 12 }}>
                                    <strong style={{ color: 'var(--text-primary)' }}>
                                        {t('Job')} #{job.id} · {t('Group')} #{job.orderGroupId || group?.orderGroupId || job.orderId}
                                    </strong>
                                    <span style={{ fontSize: '12px', fontWeight: '600', color: statusColor(job.status), background: `${statusColor(job.status)}15`, padding: '4px 10px', borderRadius: '12px' }}>
                                        {t(job.status)}
                                    </span>
                                </div>

                                {group && (
                                    <div style={{ fontSize: '13px', color: 'var(--muted)', marginBottom: '12px', display: 'grid', gap: 6 }}>
                                        <div>{t('{{count}} pickup stops', { count: stores.length })}</div>
                                        <div>{t('Stores:')} {storeNames}</div>
                                        <div>{t('Total:')} {formatPrice(group.totalAmount || 0, stores[0]?.currencyCode || 'USD')}</div>
                                        {group.deliveryAddress && <div>{t('Customer:')} {group.deliveryAddress}</div>}
                                    </div>
                                )}

                                {group && stores.length > 0 && (
                                    <div style={{ display: 'grid', gap: 8, marginBottom: '12px' }}>
                                        {stores.map(store => (
                                            <div
                                                key={store.orderId}
                                                style={{
                                                    display: 'flex',
                                                    justifyContent: 'space-between',
                                                    gap: 12,
                                                    alignItems: 'center',
                                                    padding: '10px 12px',
                                                    borderRadius: 10,
                                                    background: 'rgba(255,255,255,0.03)'
                                                }}
                                            >
                                                <div>
                                                    <div style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{store.storeName || store.storeUserId}</div>
                                                    <div style={{ fontSize: 12, color: 'var(--muted)' }}>{store.storeLocation || t('Pickup location not set')}</div>
                                                </div>
                                                <span style={{ fontSize: 11, color: 'var(--muted)' }}>{t(store.status)}</span>
                                            </div>
                                        ))}
                                    </div>
                                )}

                                <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                                    {tab === 'available' && job.status === 'Unassigned' && (
                                        <button onClick={() => acceptJob(job.id)} className="primary">{t('Accept Job')}</button>
                                    )}

                                    {tab === 'mine' && (
                                        <>
                                            {(job.status === 'Assigned' || job.status === 'PickedUp' || job.status === 'OnTheWay') && (
                                                <button
                                                    onClick={() => setSimJob(job)}
                                                    className="glass-btn"
                                                    style={{ border: simJob?.id === job.id ? '1px solid #22c55e' : '1px solid var(--border)', fontWeight: 600 }}
                                                >
                                                    {t('Open Route Map')}
                                                </button>
                                            )}
                                            {nextStatus(job.status) && (
                                                <button onClick={() => updateStatus(job.id, nextStatus(job.status))} className="primary">
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
    )
}
