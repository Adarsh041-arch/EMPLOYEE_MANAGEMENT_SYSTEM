import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <div className="app">
      <header className="topbar">
        <Link to="/employees" className="brand">Employee Management</Link>
        <div className="topbar-user">
          <span>
            {user.username} <span className="role-tag">{user.role}</span>
          </span>
          <button className="btn btn-light btn-small" onClick={handleLogout}>Log out</button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
