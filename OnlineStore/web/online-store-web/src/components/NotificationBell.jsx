import { useState, useEffect, useRef } from 'react'
import { api } from '../apiClient.js'
import * as signalR from '@microsoft/signalr'

export default function NotificationBell({ onClick }) {
    const [notifications, setNotifications] = useState([])
    const [unreadCount, setUnreadCount] = useState(0)
    const connRef = useRef(null)

    useEffect(() => {
        fetchNotifications()
        setupSignalR()
        return () => { connRef.current?.stop() }
    }, [])

    async function fetchNotifications() {
        try {
            const { data } = await api.get('/api/notifications')
            const items = Array.isArray(data) ? data : (data?.items || [])
            setNotifications(items)
            setUnreadCount(items.filter(n => !n.isRead).length)
        } catch { /* ignore */ }
    }

    function setupSignalR() {
        const token = localStorage.getItem('token')
        if (!token) return

        const baseURL = import.meta.env.VITE_API_BASE_URL
        const conn = new signalR.HubConnectionBuilder()
            .withUrl(`${baseURL}/hubs/notifications?access_token=${token}`)
            .withAutomaticReconnect()
            .build()

        conn.on('ReceiveNotification', (notif) => {
            setNotifications(prev => {
                const arr = Array.isArray(prev) ? prev : [];
                return [notif, ...arr];
            })
            setUnreadCount(prev => (typeof prev === 'number' ? prev : 0) + 1)
        })

        conn.start().catch(console.error)
        connRef.current = conn
    }

    async function markAllRead() {
        try {
            await api.put('/api/notifications/read-all')
            setNotifications(prev => Array.isArray(prev) ? prev.map(n => ({ ...n, isRead: true })) : [])
            setUnreadCount(0)
        } catch { /* ignore */ }
    }

    return (
        <div style={{ position: 'relative', width: '100%' }}>
            <button
                className="sidebar-link"
                onClick={onClick}
                style={{ width: '100%', position: 'relative', justifyContent: 'flex-start' }}
            >
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                    <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                </svg>
                Notifications
                {unreadCount > 0 && (
                    <span style={{
                        position: 'absolute', right: '12px',
                        background: '#ef4444', color: '#fff', borderRadius: '50%',
                        width: '18px', height: '18px', fontSize: '11px', fontWeight: '700',
                        display: 'flex', alignItems: 'center', justifyContent: 'center'
                    }}>
                        {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                )}
            </button>
        </div>
    )
}
