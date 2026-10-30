import { useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { employeesApi, getApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import StatusBadge from '../components/StatusBadge'
import { formatDate, formatSalary } from '../utils/format'

function Row({ label, children }) {
  return (
    <div className="detail-row">
      <dt>{label}</dt>
      <dd>{children}</dd>
    </div>
  )
}

export default function EmployeeDetailsPage() {
  const { id } = useParams()
  const location = useLocation()
  const { canEdit } = useAuth()
  const [employee, setEmployee] = useState(null)
  const [transfers, setTransfers] = useState([])
  const [error, setError] = useState('')

  useEffect(() => {
    setEmployee(null)
    Promise.all([employeesApi.get(id), employeesApi.transfers(id)])
      .then(([emp, history]) => {
        setEmployee(emp)
        setTransfers(history)
      })
      .catch((err) => setError(getApiError(err).message))
  }, [id])

  if (error) return <div className="alert alert-error">{error}</div>
  if (!employee) return <p className="muted">Loading...</p>

  const p = employee.personalDetails

  return (
    <div>
      <div className="page-header">
        <div>
          <Link to="/employees" className="back-link">&larr; All employees</Link>
          <h2>
            {employee.fullName} <span className="code-pill">{employee.employeeCode}</span>
          </h2>
        </div>
        {canEdit && (
          <div className="header-actions">
            <Link to={`/employees/${id}/edit`} className="btn btn-light">Edit</Link>
            <Link to={`/employees/${id}/transfer`} className="btn btn-primary">Transfer</Link>
          </div>
        )}
      </div>

      {location.state?.message && <div className="alert alert-success">{location.state.message}</div>}

      <div className="details-grid">
        <section className="card">
          <h3>Personal details</h3>
          <dl>
            <Row label="Full name">{employee.fullName}</Row>
            <Row label="Email">{employee.email}</Row>
            <Row label="Mobile">{employee.mobile}</Row>
            <Row label="Gender">{employee.gender}</Row>
            <Row label="Date of birth">{p ? formatDate(p.dateOfBirth) : '-'}</Row>
            <Row label="Aadhaar">{p?.aadhaar ?? '-'}</Row>
            <Row label="PAN">{p?.pan ?? '-'}</Row>
            <Row label="Address">{p?.address ?? '-'}</Row>
          </dl>
          {p?.isMasked && <p className="hint">Aadhaar and PAN are partly hidden for your role.</p>}
        </section>

        <section className="card">
          <h3>Employment details</h3>
          <dl>
            <Row label="Employee code">{employee.employeeCode}</Row>
            <Row label="Department">{employee.departmentName}</Row>
            <Row label="Role">{employee.roleName}</Row>
            <Row label="Joining date">{formatDate(employee.joiningDate)}</Row>
            <Row label="Annual salary">{formatSalary(employee.annualSalary)}</Row>
            <Row label="Status"><StatusBadge status={employee.status} /></Row>
          </dl>
        </section>
      </div>

      <section className="card">
        <h3>Transfer history</h3>
        {transfers.length === 0 ? (
          <p className="muted">No transfers yet.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>From</th>
                  <th>To</th>
                  <th>Old code</th>
                  <th>New code</th>
                  <th>Reason</th>
                  <th>By</th>
                </tr>
              </thead>
              <tbody>
                {transfers.map((t) => (
                  <tr key={t.id}>
                    <td data-label="Date">{formatDate(t.transferDate)}</td>
                    <td data-label="From">{t.oldDepartment}</td>
                    <td data-label="To">{t.newDepartment}</td>
                    <td data-label="Old code">{t.oldEmployeeCode}</td>
                    <td data-label="New code">{t.newEmployeeCode}</td>
                    <td data-label="Reason">{t.reason}</td>
                    <td data-label="By">{t.transferredBy ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
