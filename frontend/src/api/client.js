import axios from 'axios'

const TOKEN_KEY = 'ems_auth'

export function loadAuth() {
  try {
    const saved = JSON.parse(localStorage.getItem(TOKEN_KEY))
    if (saved && new Date(saved.expiresAt) > new Date()) return saved
  } catch {
    // ignore broken value
  }
  return null
}

export function saveAuth(auth) {
  if (auth) localStorage.setItem(TOKEN_KEY, JSON.stringify(auth))
  else localStorage.removeItem(TOKEN_KEY)
}

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL || '/api' })

api.interceptors.request.use((config) => {
  const auth = loadAuth()
  if (auth) config.headers.Authorization = `Bearer ${auth.token}`
  return config
})

// token expired or invalid -> back to login
api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401 && !error.config.url.includes('/auth/login')) {
      saveAuth(null)
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// Pulls a readable message (and field errors, if any) out of a ProblemDetails response
export function getApiError(error) {
  const data = error.response?.data
  if (!error.response) return { message: 'Could not reach the server. Is the API running?', fields: {} }
  const fields = {}
  if (data?.errors) {
    for (const [key, messages] of Object.entries(data.errors)) {
      // backend uses PascalCase property names, the forms use camelCase
      const name = key.charAt(0).toLowerCase() + key.slice(1)
      fields[name] = Array.isArray(messages) ? messages[0] : String(messages)
    }
  }
  const status = error.response.status
  const message =
    status === 403 ? 'You do not have permission to do that.' : data?.title || `Request failed (${status})`
  return { message, fields }
}

export const employeesApi = {
  list: (params) => api.get('/employees', { params }).then((r) => r.data),
  get: (id) => api.get(`/employees/${id}`).then((r) => r.data),
  transfers: (id) => api.get(`/employees/${id}/transfers`).then((r) => r.data),
  create: (data) => api.post('/employees', data).then((r) => r.data),
  update: (id, data) => api.put(`/employees/${id}`, data).then((r) => r.data),
  deactivate: (id) => api.delete(`/employees/${id}`),
  transfer: (id, data) => api.post(`/employees/${id}/transfer`, data).then((r) => r.data),
}

export const lookupsApi = {
  departments: () => api.get('/departments').then((r) => r.data),
  roles: () => api.get('/roles').then((r) => r.data),
}

export const authApi = {
  login: (username, password) => api.post('/auth/login', { username, password }).then((r) => r.data),
}
