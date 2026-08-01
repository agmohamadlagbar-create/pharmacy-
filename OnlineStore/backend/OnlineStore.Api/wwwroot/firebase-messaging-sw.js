/* eslint-disable no-undef */
// Service Worker for background push notifications

// Load Firebase SDK FIRST - must happen before any event listeners
// so that firebase.messaging() is available when push events arrive
importScripts('https://www.gstatic.com/firebasejs/10.8.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.8.0/firebase-messaging-compat.js');

// Immediately activate service worker
self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(clients.claim());
});

const firebaseConfig = {
  apiKey: "AIzaSyAVcm1pQpLnhsHWGKwTYryaOag-oDTp30k",
  authDomain: "online-store-cbba7.firebaseapp.com",
  projectId: "online-store-cbba7",
  storageBucket: "online-store-cbba7.firebasestorage.app",
  messagingSenderId: "350491431086",
  appId: "1:350491431086:web:7b67e80d567123cc90dd3f",
  measurementId: "G-RKXBNX81BV"
};

try {
  firebase.initializeApp(firebaseConfig);
  const messaging = firebase.messaging();

  // Single handler for background messages (data-only).
  // This is the ONLY place that displays OS notifications.
  // No duplicate "push" event listener or "notification" auto-display.
  messaging.onBackgroundMessage((payload) => {
    console.log('[firebase-messaging-sw.js] Received background message: ', payload);

    const data = payload.data || {};
    const title = data.title || 'Online Store Update';
    const options = {
      body: data.body || 'You have a new update from Online Store.',
      icon: data.icon || '/logo192.png',
      badge: data.badge || '/favicon.ico',
      data: data
    };

    self.registration.showNotification(title, options);
  });
} catch (e) {
  console.warn('[firebase-messaging-sw.js] Firebase SW initialization fallback: ', e.message);
}

// Handle notification click event (open app/order detail)
self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const clickData = event.notification.data || {};

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if (client.url.includes(self.location.origin) && 'focus' in client) {
          return client.focus();
        }
      }
      if (clients.openWindow) {
        const targetUrl = clickData.orderId ? `/notifications` : '/';
        return clients.openWindow(targetUrl);
      }
    })
  );
});
