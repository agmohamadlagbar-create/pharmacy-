import axios from 'axios'
import { triggerGlobalError } from './context/ErrorContext'

// Backend default driven by .env variable.
const baseURL = import.meta.env.VITE_API_BASE_URL || ''

export const api = axios.create({
  baseURL,
  headers: { 
    'Content-Type': 'application/json'
  }
})

export function setAuthToken(token) {
  if (token) api.defaults.headers.common['Authorization'] = `Bearer ${token}`
  else delete api.defaults.headers.common['Authorization']
}

// ── Refresh token interceptor ──
let isRefreshing = false
let failedQueue = []

function processQueue(error, token = null) {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error)
    else resolve(token)
  })
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      const refreshToken = localStorage.getItem('refreshToken')
      const accessToken = localStorage.getItem('token')

      if (!refreshToken || !accessToken) {
        // No refresh token – force logout
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        localStorage.removeItem('accountType')
        window.location.reload()
        return Promise.reject(error)
      }

      if (isRefreshing) {
        // Queue this request until refresh completes
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          originalRequest.headers['Authorization'] = `Bearer ${token}`
          return api(originalRequest)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const { data } = await axios.post(`${baseURL}/api/auth/refresh`, {
          accessToken,
          refreshToken
        })

        localStorage.setItem('token', data.token)
        localStorage.setItem('refreshToken', data.refreshToken)
        localStorage.setItem('accountType', data.accountType)
        setAuthToken(data.token)

        processQueue(null, data.token)

        originalRequest.headers['Authorization'] = `Bearer ${data.token}`
        return api(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError)
        localStorage.removeItem('token')
        localStorage.removeItem('refreshToken')
        localStorage.removeItem('accountType')
        window.location.reload()
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    // ── Global Error System Integration ──
    const status = error.response?.status;
    const errorData = error.response?.data;

    if (status === 503) {
      triggerGlobalError('toast', { 
        message: 'Service is temporarily unavailable', 
        actionText: 'Retry',
        onAction: () => window.location.reload() 
      });
    } else if (status === 500) {
      triggerGlobalError('banner', { 
        title: 'Application Error', 
        message: 'We encountered an internal server error. Our team has been notified.' 
      });
    } else if (status === 403) {
      triggerGlobalError('modal', {
        title: 'Access Restricted',
        message: errorData?.error || 'You do not have permission to perform this action.',
        resource: error.config?.url,
        actionText: 'Contact Admin'
      });
    }

    return Promise.reject(error)
  }
)
