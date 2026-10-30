import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { employeesApi, getApiError } from '../api/client'
import useLookups from '../hooks/useLookups'
import { transferSchema } from '../validation/employeeSchema'

export default function TransferPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { departments } = useLookups()
  const [employee, setEmployee] = useState(null)
  const [error, setError] = useState('')

  const {
    register,
    handleSubmit,
    setError: setFieldError,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(transferSchema),
    defaultValues: { newDepartmentId: '', transferDate: new Date().toISOString().slice(0, 10), reason: '' },
  })

  useEffect(() => {
    employeesApi
      .get(id)
      .then(setEmployee)
      .catch((err) => setError(getApiError(err).message))
  }, [id])

  async function onSubmit(values) {
    setError('')
    try {
      const result = await employeesApi.transfer(id, values)
      navigate(`/employees/${id}`, {
        state: {
          message: `Transferred from ${result.oldDepartment} to ${result.newDepartment}. Employee code changed from ${result.oldEmployeeCode} to ${result.newEmployeeCode}.`,
        },
      })
    } catch (err) {
      const { message, fields } = getApiError(err)
      if (fields.newDepartmentId) setFieldError('newDepartmentId', { message: fields.newDepartmentId })
      setError(message)
    }
  }

  if (!employee) return error ? <div className="alert alert-error">{error}</div> : <p className="muted">Loading...</p>

  const otherDepartments = departments.filter((d) => d.id !== employee.departmentId)

  return (
    <div className="narrow">
      <div className="page-header">
        <h2>Transfer employee</h2>
        <Link to={`/employees/${id}`} className="btn btn-light">Cancel</Link>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      <form className="card form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="form-grid">
          <label className="field">
            <span>Employee name</span>
            <input value={employee.fullName} readOnly disabled />
          </label>
          <label className="field">
            <span>Current employee code</span>
            <input value={employee.employeeCode} readOnly disabled />
          </label>
          <label className="field">
            <span>Current department</span>
            <input value={employee.departmentName} readOnly disabled />
          </label>
          <label className={`field${errors.newDepartmentId ? ' has-error' : ''}`}>
            <span>New department *</span>
            <select {...register('newDepartmentId')}>
              <option value="">Select</option>
              {otherDepartments.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
            {errors.newDepartmentId && <small className="error-text">{errors.newDepartmentId.message}</small>}
          </label>
          <label className={`field${errors.transferDate ? ' has-error' : ''}`}>
            <span>Transfer date *</span>
            <input type="date" {...register('transferDate')} />
            {errors.transferDate && <small className="error-text">{errors.transferDate.message}</small>}
          </label>
          <label className={`field field-wide${errors.reason ? ' has-error' : ''}`}>
            <span>Transfer reason *</span>
            <textarea rows={3} {...register('reason')} />
            {errors.reason && <small className="error-text">{errors.reason.message}</small>}
          </label>
        </div>
        <p className="hint">The employee will get the next available code for the new department. Their internal ID does not change.</p>
        <div className="form-actions">
          <button className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Transferring...' : 'Transfer'}
          </button>
        </div>
      </form>
    </div>
  )
}
