import { initializeApp } from 'firebase/app';
import { getMessaging, getToken, onMessage } from 'firebase/messaging';
import axios from 'axios';

const firebaseConfig = {
  apiKey: "AIzaSyAVcm1pQpLnhsHWGKwTYryaOag-oDTp30k",
  authDomain: "online-store-cbba7.firebaseapp.com",
  projectId: "online-store-cbba7",
  storageBucket: "online-store-cbba7.firebasestorage.app",
  messagingSenderId: "350491431086",
  appId: "1:350491431086:web:7b67e80d567123cc90dd3f",
  measurementId: "G-RKXBNX81BV"
};

const VAPID_KEY = "BMf_Fibk7nVWi_Dg5Rz14bQ_MoSgMOpoAqAZGbP5ydEI7TDgWyuZiHILM_HMD3T5yot4vAUt_LCQI3JQzhl09RY";

let app = null;
let messaging = null;

try {
  app = initializeApp(firebaseConfig);
  if ('serviceWorker' in navigator && 'Notification' in window) {
    messaging = getMessaging(app);
  }
} catch (err) {
  console.warn('Firebase initialization warning:', err.message);
}

export async function requestNotificationPermissionAndGetToken() {
  if (!('Notification' in window) || !('serviceWorker' in navigator)) {
    console.warn('This browser does not support web push notifications.');
    return null;
  }

  try {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      console.info('Notification permission was not granted by user:', permission);
      return null;
    }

    // Register background service worker
    let registration = null;
    try {
      registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
      console.log('Service Worker registered successfully:', registration);
      // Wait for the service worker to be ready
      await navigator.serviceWorker.ready;
      console.log('Service Worker is ready.');
    } catch (swErr) {
      console.error('[Push Setup] Service Worker registration FAILED:', swErr);
      return null;
    }

    if (!messaging) {
      console.error('[Push Setup] Firebase Messaging is not initialized. Push notifications will not work.');
      return null;
    }

    let token = null;

    try {
      token = await getToken(messaging, {
        vapidKey: VAPID_KEY,
        serviceWorkerRegistration: registration
      });
    } catch (tokenErr) {
      console.warn('[Push Setup] Could not obtain FCM token from Firebase:', tokenErr.message);
      return null;
    }

    if (!token) {
      console.warn('[Push Setup] getToken() returned null/empty. No FCM token available.');
      return null;
    }

    console.log('✅ Obtained REAL FCM Web Push Token from Firebase:', token);
    localStorage.setItem('fcm_web_token', token);

    // Register the real token with backend API
    await registerTokenWithBackend(token);

    // Listen for foreground FCM notifications (data-only messages)
    onMessage(messaging, (payload) => {
      console.log('Foreground FCM notification received:', payload);
      const data = payload.data || {};
      const title = data.title || payload.notification?.title || 'Notification';
      const options = {
        body: data.body || payload.notification?.body || '',
        icon: data.icon || '/logo192.png'
      };
      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification(title, options);
      }
    });

    return token;
  } catch (error) {
    console.error('[Push Setup] Unexpected error enabling push notifications:', error);
    return null;
  }
}

export async function registerTokenWithBackend(token) {
  const authToken = localStorage.getItem('token');
  if (!authToken || !token) return;

  try {
    await axios.post('/api/devicetokens', {
      token: token,
      platform: 'Web'
    }, {
      headers: { Authorization: `Bearer ${authToken}` }
    });
    console.log('Successfully registered Web FCM token with backend API.');
  } catch (err) {
    console.warn('Failed to register FCM device token with backend:', err.message);
  }
}

export async function unregisterTokenFromBackend() {
  const token = localStorage.getItem('fcm_web_token');
  const authToken = localStorage.getItem('token');
  if (!authToken || !token) return;

  try {
    await axios.delete(`/api/devicetokens/${encodeURIComponent(token)}`, {
      headers: { Authorization: `Bearer ${authToken}` }
    });
    console.log('Unregistered Web FCM token from backend API.');
  } catch (err) {
    console.warn('Failed to unregister FCM token from backend:', err.message);
  }
}
