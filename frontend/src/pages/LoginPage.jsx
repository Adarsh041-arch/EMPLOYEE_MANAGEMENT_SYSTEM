import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { getApiError } from '../api/client'

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (user) return <Navigate to="/employees" replace />

  async function handleSubmit(e) {
    e.preventDefault()
    if (!username || !password) {
      setError('Enter your username and password.')
      return
    }
    setLoading(true)
    setError('')
    try {
      await login(username, password)
      navigate('/employees')
    } catch (err) {
      setError(getApiError(err).message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <form className="card login-card" onSubmit={handleSubmit} noValidate>
        <h1>Employee Management</h1>
        <p className="muted">Sign in to continue</p>

        {error && <div className="alert alert-error">{error}</div>}

        <label className="field">
          <span>Username</span>
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoFocus autoComplete="username" />
        </label>
        <label className="field">
          <span>Password</span>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </label>

        <button className="btn btn-primary btn-block" disabled={loading}>
          {loading ? 'Signing in...' : 'Sign in'}
        </button>

        <div className="demo-logins">
          Demo users: <code>admin / Admin@123</code>, <code>manager / Manager@123</code>, <code>employee / Employee@123</code>
        </div>
      </form>
    </div>
  )
}
