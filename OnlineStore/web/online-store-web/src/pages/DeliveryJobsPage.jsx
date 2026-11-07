import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { api } from '../apiClient.js'
import { formatPrice } from '../utils/formatPrice.js'

export default function DeliveryJobsPage({ onBack }) {
    const { t } = useTranslation()
    const [tab, setTab] = useState('available')
    const [availableJobs, setAvailableJobs] = useState([])
    const [myJobs, setMyJobs] = useState([])
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        load()
    }, [tab])

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
            setTab('mine')
            load()
        } catch (err) {
            alert(err?.response?.data?.error || err.message)
        }
    }

    async function updateStatus(jobId, newStatus) {
        try {
            await api.put(`/api/delivery/${jobId}/status`, { status: newStatus })
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

                                    {tab === 'mine' && nextStatus(job.status) && (
                                        <button onClick={() => updateStatus(job.id, nextStatus(job.status))} className="primary">
                                            {t('Mark as')} {t(nextStatus(job.status))}
                                        </button>
                                    )}
                                </div>
                            </div>
                        )
                    })}
                </div>
            )}
        </div>
    )
}
