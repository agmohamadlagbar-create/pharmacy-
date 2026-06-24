import { useEffect, useMemo, useState } from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import './OrderTrackingCard.css'
import { api } from '../apiClient'
import { countReadyStores, countTotalItems, getTrackingInfo } from '../utils/orderTracking'

const customerIcon = L.divIcon({
    html: `<div class="map-icon customer-icon"><svg width="24" height="24" viewBox="0 0 24 24" fill="#E8593C" stroke="white" stroke-width="1.5"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg></div>`,
    className: 'custom-leaflet-icon',
    iconSize: [32, 32],
    iconAnchor: [16, 32]
})

const storeIcon = L.divIcon({
    html: `<div class="map-icon store-icon"><svg width="24" height="24" viewBox="0 0 24 24" fill="#ef9f27" stroke="white" stroke-width="1.5"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect><path d="M7 11V7a5 5 0 0 1 10 0v4"></path></svg></div>`,
    className: 'custom-leaflet-icon',
    iconSize: [32, 32],
    iconAnchor: [16, 32]
})

const driverIcon = L.divIcon({
    html: `<div class="map-icon driver-icon"><svg width="28" height="28" viewBox="0 0 24 24" fill="#3B8BD4" stroke="white" stroke-width="1.5"><circle cx="5" cy="18" r="3"></circle><circle cx="19" cy="18" r="3"></circle><path d="M10 18h4"></path><path d="M12 18V9l7 3"></path><path d="M12 13H4a2 2 0 0 1-2-2V4h3l2 7"></path></svg></div>`,
    className: 'custom-leaflet-icon',
    iconSize: [34, 34],
    iconAnchor: [17, 34]
})

const STATUS_STEPS = [
    { key: 'pending', label: 'Placed' },
    { key: 'preparing', label: 'Preparing' },
    { key: 'readyforpickup', label: 'Ready' },
    { key: 'assigned', label: 'Assigned' },
    { key: 'pickedup', label: 'Picked up' },
    { key: 'ontheway', label: 'On way' },
    { key: 'delivered', label: 'Delivered!' }
]

function StatusBar({ currentStatus }) {
    const normalized = (currentStatus || '').toLowerCase()
    if (normalized === 'cancelled') return null

    const currentIndex = STATUS_STEPS.findIndex(step => step.key === normalized)

    return (
        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '16px 0 24px', overflowX: 'auto', gap: '8px' }}>
            {STATUS_STEPS.map((step, index) => (
                <div key={step.key} style={{ textAlign: 'center', fontSize: 11, flex: 1, minWidth: 54 }}>
                    <div style={{
                        width: 14,
                        height: 14,
                        borderRadius: '50%',
                        background: index <= currentIndex ? 'var(--primary, #3B8BD4)' : 'var(--muted, #ccc)',
                        margin: '0 auto 8px',
                        transform: index === currentIndex ? 'scale(1.3)' : 'scale(1)',
                        transition: 'all 0.3s ease',
                        boxShadow: index === currentIndex ? '0 0 8px rgba(59, 139, 212, 0.5)' : 'none'
                    }} />
                    <div style={{
                        color: index <= currentIndex ? 'var(--text-primary)' : 'var(--muted)',
                        fontWeight: index === currentIndex ? 'bold' : 'normal',
                        whiteSpace: 'nowrap'
                    }}>
                        {step.label}
                    </div>
                </div>
            ))}
        </div>
    )
}

function MapBoundsSetter({ points }) {
    const map = useMap()

    useEffect(() => {
        const validPoints = points.filter(point =>
            point &&
            point.length === 2 &&
            typeof point[0] === 'number' &&
            typeof point[1] === 'number' &&
            isFinite(point[0]) &&
            isFinite(point[1])
        )

        if (validPoints.length > 0) {
            map.fitBounds(L.latLngBounds(validPoints), { padding: [30, 30] })
        }
    }, [points, map])

    return null
}

export default function OrderTrackingCard({ order, onDismiss, trackingConnection }) {
    const stores = order.stores || []
    const [driverPos, setDriverPos] = useState(null)
    const [trackingStatus, setTrackingStatus] = useState((order.status || '').toLowerCase())
    const [isCancelling, setIsCancelling] = useState(false)
    const [showCancelModal, setShowCancelModal] = useState(false)

    useEffect(() => {
        setTrackingStatus((order.status || '').toLowerCase())
    }, [order.status, order.orderGroupId])

    const readyStoreCount = useMemo(() => countReadyStores(stores), [stores])

    const totalItems = useMemo(() => countTotalItems(stores), [stores])

    const info = useMemo(
        () => getTrackingInfo(trackingStatus, readyStoreCount, stores.length),
        [readyStoreCount, stores.length, trackingStatus]
    )

    useEffect(() => {
        const status = (trackingStatus || '').toLowerCase()
        if ((status === 'delivered' || status === 'cancelled') && onDismiss) {
            const delay = status === 'cancelled' ? 2000 : 10000
            const timer = setTimeout(() => onDismiss(order.orderGroupId), delay)
            return () => clearTimeout(timer)
        }
    }, [onDismiss, order.orderGroupId, trackingStatus])

    useEffect(() => {
        if (!trackingConnection || !order.orderGroupId) return

        const handleLocationUpdate = (data) => {
            const returnedGroupId = data.orderGroupId ?? data.OrderGroupId
            const lat = data.latitude ?? data.Latitude ?? data.lat ?? data.Lat
            const lng = data.longitude ?? data.Longitude ?? data.lng ?? data.Lng

            if (String(returnedGroupId) === String(order.orderGroupId) && lat != null && lng != null) {
                setDriverPos([lat, lng])
            }
        }

        const handleStatusUpdate = (data) => {
            const returnedGroupId = data.orderGroupId ?? data.OrderGroupId
            const status = ((data.status ?? data.Status) || '').toLowerCase()

            if (String(returnedGroupId) === String(order.orderGroupId) && status) {
                setTrackingStatus(status)
            }
        }

        trackingConnection.on('location:updated', handleLocationUpdate)
        trackingConnection.on('order:status_updated', handleStatusUpdate)

        const joinGroup = async () => {
            while (trackingConnection.state === 'Connecting') {
                await new Promise(resolve => setTimeout(resolve, 100))
            }

            if (trackingConnection.state === 'Connected') {
                await trackingConnection.invoke('JoinGroupedOrderGroup', order.orderGroupId).catch(err => {
                    console.error('JoinGroupedOrderGroup error:', err)
                })
            }
        }

        joinGroup()

        trackingConnection.onreconnected(() => {
            trackingConnection.invoke('JoinGroupedOrderGroup', order.orderGroupId).catch(() => {})
        })

        return () => {
            trackingConnection.off('location:updated', handleLocationUpdate)
            trackingConnection.off('order:status_updated', handleStatusUpdate)

            if (trackingConnection.state === 'Connected') {
                trackingConnection.invoke('LeaveGroupedOrderGroup', order.orderGroupId).catch(() => {})
            }
        }
    }, [order.orderGroupId, trackingConnection])

    async function confirmCancelOrder() {
        setIsCancelling(true)
        try {
            await api.put(`/api/orders/${order.representativeOrderId}/cancel`)
            setTrackingStatus('cancelled')
            setShowCancelModal(false)
        } catch (error) {
            alert(error.response?.data?.error || 'Failed to cancel order')
        } finally {
            setIsCancelling(false)
        }
    }

    const points = []
    stores.forEach(store => {
        if (store.storeLat != null && store.storeLng != null) {
            points.push([store.storeLat, store.storeLng])
        }
    })
    if (order.customerLat != null && order.customerLng != null) points.push([order.customerLat, order.customerLng])
    if (driverPos) points.push(driverPos)

    const mapCenter = points.length > 0 ? points[0] : [41.0082, 28.9784]

    return (
        <div className="order-tracking-card">
            <div className="tracking-header">
                <div className="header-top">
                    <div />
                    <span className="eta-label">{info.eta}</span>
                </div>
                <div className="header-bottom">
                    <span className="status-text">{info.label}</span>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        {(trackingStatus === 'pending' || trackingStatus === 'preparing') && (
                            <button
                                onClick={() => setShowCancelModal(true)}
                                disabled={isCancelling}
                                style={{
                                    background: 'transparent',
                                    border: '1px solid #E8593C',
                                    color: '#E8593C',
                                    borderRadius: 6,
                                    padding: '4px 8px',
                                    fontSize: 12,
                                    cursor: isCancelling ? 'not-allowed' : 'pointer'
                                }}
                            >
                                {isCancelling ? 'Cancelling...' : 'Cancel Order'}
                            </button>
                        )}
                        <span
                            className="status-dot"
                            style={{
                                backgroundColor: info.color,
                                animation: trackingStatus === 'delivered' ? 'none' : 'tracking-pulse 1.2s infinite'
                            }}
                        />
                    </div>
                </div>
            </div>

            <StatusBar currentStatus={trackingStatus} />

            <div style={{
                display: 'grid',
                gap: 10,
                marginBottom: 18,
                padding: 14,
                borderRadius: 14,
                background: 'rgba(255,255,255,0.04)',
                border: '1px solid var(--border)'
            }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                    <strong style={{ color: 'var(--text-primary)' }}>{stores.length} stores, {totalItems} items</strong>
                    <strong style={{ color: 'var(--text-primary)' }}>${Number(order.totalAmount || 0).toFixed(2)}</strong>
                </div>
                <div style={{ fontSize: 12, color: 'var(--muted)' }}>{order.deliveryAddress}</div>
                <div style={{ display: 'grid', gap: 8 }}>
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
                                <div style={{ color: 'var(--text-primary)', fontWeight: 600 }}>
                                    {(store.items || []).map(item => `x${item.quantity} ${item.productName}`).join(', ')}
                                </div>
                            </div>
                            <span style={{
                                fontSize: 11,
                                fontWeight: 700,
                                color: 'var(--text-primary)',
                                padding: '4px 8px',
                                borderRadius: 999,
                                background: 'rgba(255,255,255,0.08)'
                            }}>
                                {store.status}
                            </span>
                        </div>
                    ))}
                </div>
            </div>

            <div className="map-area">
                <MapContainer center={mapCenter} zoom={13} scrollWheelZoom={false} zoomControl={false} className="leaflet-tracking-map">
                    <TileLayer
                        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                        attribution="&copy; OpenStreetMap contributors"
                    />

                    {stores.map(store => (
                        store.storeLat != null && store.storeLng != null ? (
                            <Marker key={store.orderId} position={[store.storeLat, store.storeLng]} icon={storeIcon}>
                                <Popup>
                                    {store.storeName || 'Store'}
                                    <br />
                                    {store.status}
                                </Popup>
                            </Marker>
                        ) : null
                    ))}

                    {order.customerLat != null && order.customerLng != null && (
                        <Marker position={[order.customerLat, order.customerLng]} icon={customerIcon}>
                            <Popup>Delivery Address</Popup>
                        </Marker>
                    )}

                    {driverPos && (
                        <Marker position={driverPos} icon={driverIcon}>
                            <Popup>Courier</Popup>
                        </Marker>
                    )}

                    <MapBoundsSetter points={points} />
                </MapContainer>
                <div className="map-badge">Live tracking</div>
            </div>

            {showCancelModal && (
                <div className="cancel-modal-overlay">
                    <div className="cancel-modal-content">
                        <div className="cancel-icon-wrapper">
                            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#E8593C" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                <circle cx="12" cy="12" r="10" />
                                <line x1="15" y1="9" x2="9" y2="15" />
                                <line x1="9" y1="9" x2="15" y2="15" />
                            </svg>
                        </div>
                        <h4 className="cancel-modal-title">Cancel Grouped Order?</h4>
                        <p className="cancel-modal-text">
                            This will cancel every store order inside #{order.orderGroupId}.
                        </p>
                        <div className="cancel-modal-actions">
                            <button className="btn-cancel-keep" onClick={() => setShowCancelModal(false)} disabled={isCancelling}>
                                No, Keep It
                            </button>
                            <button className="btn-cancel-confirm" onClick={confirmCancelOrder} disabled={isCancelling}>
                                {isCancelling ? 'Cancelling...' : 'Yes, Cancel'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}
