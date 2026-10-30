import { createContext, useContext, useState } from 'react'
import { authApi, loadAuth, saveAuth } from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(loadAuth)

  async function login(username, password) {
    const data = await authApi.login(username, password)
    saveAuth(data)
    setUser(data)
    return data
  }

  function logout() {
    saveAuth(null)
    setUser(null)
  }

  const canEdit = user?.role === 'Admin' || user?.role === 'Manager'
  const isAdmin = user?.role === 'Admin'

  return (
    <AuthContext.Provider value={{ user, login, logout, canEdit, isAdmin }}>
      {children}
    </AuthContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  return useContext(AuthContext)
}
