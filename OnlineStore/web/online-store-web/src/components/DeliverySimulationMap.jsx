import { MapContainer, Marker, TileLayer, useMapEvents } from 'react-leaflet'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

const agentIcon = L.divIcon({
    html: `<div style="
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-radius: 50%;
        width: 36px;
        height: 36px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 3px 12px rgba(102,126,234,0.5);
        border: 3px solid white;
        font-size: 18px;
    ">D</div>`,
    className: 'custom-agent-icon',
    iconSize: [36, 36],
    iconAnchor: [18, 18]
})

const stopIcon = L.divIcon({
    html: `<div style="
        background: linear-gradient(135deg, #f59e0b 0%, #ef4444 100%);
        border-radius: 50%;
        width: 30px;
        height: 30px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 3px 12px rgba(239,68,68,0.35);
        border: 3px solid white;
        font-size: 12px;
        color: white;
        font-weight: 700;
    ">S</div>`,
    className: 'custom-stop-icon',
    iconSize: [30, 30],
    iconAnchor: [15, 30]
})

const customerIcon = L.divIcon({
    html: `<div style="
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
        border-radius: 50%;
        width: 32px;
        height: 32px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 3px 12px rgba(16,185,129,0.35);
        border: 3px solid white;
        font-size: 14px;
        color: white;
        font-weight: 700;
    ">C</div>`,
    className: 'custom-customer-icon',
    iconSize: [32, 32],
    iconAnchor: [16, 32]
})

function ClickToMove({ onMove }) {
    useMapEvents({
        click(e) {
            onMove([e.latlng.lat, e.latlng.lng])
        }
    })

    return null
}

function AutoMoveButton({ setAgentPos, target }) {
    const { t } = useTranslation()
    const [running, setRunning] = useState(false)

    useEffect(() => {
        if (!running || !target) return

        const interval = setInterval(() => {
            setAgentPos(prev => {
                const nextLat = prev[0] + (target[0] - prev[0]) * 0.12
                const nextLng = prev[1] + (target[1] - prev[1]) * 0.12
                return [nextLat, nextLng]
            })
        }, 1800)

        return () => clearInterval(interval)
    }, [running, setAgentPos, target])

    return (
        <button
            onClick={() => setRunning(value => !value)}
            style={{
                flex: 1,
                padding: '12px 16px',
                background: running ? 'linear-gradient(135deg, #dd6b20, #ed8936)' : 'linear-gradient(135deg, #38a169, #48bb78)',
                color: 'white',
                border: 'none',
                borderRadius: 10,
                cursor: 'pointer',
                fontWeight: 600
            }}
        >
            {running ? t('Pause Auto-Move') : t('Auto-Move')}
        </button>
    )
}

export default function DeliverySimulationMap({ order, hubConnection, onClose }) {
    const { t } = useTranslation()
    const groupedOrder = order.orderGroup ?? order.groupedOrder ?? order
    const stores = groupedOrder?.stores || []

    const firstStoreWithCoords = stores.find(store => store.storeLat != null && store.storeLng != null)
    const customerTarget = groupedOrder?.customerLat != null && groupedOrder?.customerLng != null
        ? [groupedOrder.customerLat, groupedOrder.customerLng]
        : null

    const [agentPos, setAgentPos] = useState([
        firstStoreWithCoords?.storeLat ?? customerTarget?.[0] ?? 41.0082,
        firstStoreWithCoords?.storeLng ?? customerTarget?.[1] ?? 28.9784
    ])

    useEffect(() => {
        if (!hubConnection || !groupedOrder?.orderGroupId) return

        const sendLoc = async () => {
            try {
                if (hubConnection.state === 'Disconnected') {
                    await hubConnection.start().catch(() => {})
                }

                while (hubConnection.state === 'Connecting') {
                    await new Promise(resolve => setTimeout(resolve, 100))
                }

                if (hubConnection.state === 'Connected') {
                    await hubConnection.invoke('SendGroupLocation', groupedOrder.orderGroupId, agentPos[0], agentPos[1])
                }
            } catch (err) {
                console.error('[DeliverySimulationMap] SendGroupLocation error:', err)
            }
        }

        sendLoc()
    }, [agentPos, groupedOrder?.orderGroupId, hubConnection])

    const pickupSummary = useMemo(
        () => stores.map(store => store.storeName || store.storeUserId).join(' -> '),
        [stores]
    )

    return (
        <div style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.65)',
            backdropFilter: 'blur(6px)',
            WebkitBackdropFilter: 'blur(6px)',
            zIndex: 9999,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center'
        }}>
            <div style={{
                background: 'var(--card, #ffffff)',
                borderRadius: 16,
                padding: 20,
                width: '92%',
                maxWidth: 700,
                boxShadow: '0 20px 60px rgba(0,0,0,0.3)'
            }}>
                <div style={{ marginBottom: 12 }}>
                    <h3 style={{ margin: 0, color: 'var(--text-primary, #111827)' }}>
                        {t('Delivery Route for #{{id}}', { id: groupedOrder?.orderGroupId })}
                    </h3>
                    <p style={{ margin: '6px 0 0', color: 'var(--muted, #6b7280)', fontSize: 13 }}>
                        {t('Pickup stops:')} {pickupSummary || t('No store coordinates yet')}
                    </p>
                </div>

                <div style={{ position: 'relative', borderRadius: 10, overflow: 'hidden', marginTop: 8 }}>
                    <MapContainer center={agentPos} zoom={13} style={{ height: 400, borderRadius: 10 }}>
                        <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                        <ClickToMove onMove={setAgentPos} />
                        <Marker position={agentPos} icon={agentIcon} />

                        {stores.map(store => (
                            store.storeLat != null && store.storeLng != null ? (
                                <Marker
                                    key={store.orderId}
                                    position={[store.storeLat, store.storeLng]}
                                    icon={stopIcon}
                                />
                            ) : null
                        ))}

                        {customerTarget && (
                            <Marker position={customerTarget} icon={customerIcon} />
                        )}
                    </MapContainer>

                    <div style={{
                        position: 'absolute',
                        top: 10,
                        right: 10,
                        zIndex: 1000,
                        background: 'rgba(0,0,0,0.7)',
                        color: '#4ade80',
                        padding: '4px 10px',
                        borderRadius: 20,
                        fontSize: 12,
                        fontWeight: 600
                    }}>
                        {t('LIVE')}
                    </div>
                </div>

                <div style={{ display: 'grid', gap: 8, marginTop: 14, marginBottom: 14 }}>
                    {stores.map(store => (
                        <div
                            key={`stop-${store.orderId}`}
                            style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                gap: 12,
                                padding: '10px 12px',
                                borderRadius: 10,
                                background: 'rgba(255,255,255,0.04)',
                                border: '1px solid var(--border)'
                            }}
                        >
                            <div>
                                <strong style={{ color: 'var(--text-primary)' }}>{store.storeName || store.storeUserId}</strong>
                                <div style={{ fontSize: 12, color: 'var(--muted)' }}>{store.storeLocation || t('Store location not set')}</div>
                            </div>
                            <span style={{ fontSize: 12, color: 'var(--muted)' }}>{t(store.status)}</span>
                        </div>
                    ))}
                </div>

                <div style={{ marginTop: 14, display: 'flex', gap: 10 }}>
                    <AutoMoveButton setAgentPos={setAgentPos} target={customerTarget} />
                    <button
                        onClick={onClose}
                        style={{
                            flex: 1,
                            padding: '12px 16px',
                            background: 'linear-gradient(135deg, #e53e3e, #f56565)',
                            color: 'white',
                            border: 'none',
                            borderRadius: 10,
                            cursor: 'pointer',
                            fontWeight: 600
                        }}
                    >
                        {t('Close Map')}
                    </button>
                </div>
            </div>
        </div>
    )
}
