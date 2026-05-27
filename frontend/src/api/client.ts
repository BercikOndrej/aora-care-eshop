import axios from 'axios'

/**
 * Shared axios instance for all API calls.
 * In development, Vite proxies /api → http://localhost:5238 (no CORS).
 * In production, set VITE_API_BASE_URL to the deployed backend origin.
 */
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

export default apiClient
