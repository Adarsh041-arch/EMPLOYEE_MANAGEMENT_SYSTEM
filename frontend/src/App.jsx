import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import EmployeeListPage from './pages/EmployeeListPage'
import EmployeeFormPage from './pages/EmployeeFormPage'
import EmployeeDetailsPage from './pages/EmployeeDetailsPage'
import TransferPage from './pages/TransferPage'

function RequireAuth({ children, editorOnly }) {
  const { user, canEdit } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  if (editorOnly && !canEdit) return <Navigate to="/employees" replace />
  return children
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route path="/employees" element={<EmployeeListPage />} />
        <Route path="/employees/:id" element={<EmployeeDetailsPage />} />
        <Route path="/employees/new" element={<RequireAuth editorOnly><EmployeeFormPage /></RequireAuth>} />
        <Route path="/employees/:id/edit" element={<RequireAuth editorOnly><EmployeeFormPage /></RequireAuth>} />
        <Route path="/employees/:id/transfer" element={<RequireAuth editorOnly><TransferPage /></RequireAuth>} />
      </Route>
      <Route path="*" element={<Navigate to="/employees" replace />} />
    </Routes>
  )
}
