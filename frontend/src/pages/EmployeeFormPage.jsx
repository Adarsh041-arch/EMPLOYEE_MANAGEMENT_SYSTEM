import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { employeesApi, getApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import useLookups from '../hooks/useLookups'
import { employeeSchema, employeeUpdateSchema, localToday } from '../validation/employeeSchema'
import { formatAadhaarInput } from '../utils/format'

const EMPTY = {
  fullName: '',
  email: '',
  mobile: '',
  gender: '',
  aadhaar: '',
  pan: '',
  dateOfBirth: '',
  address: '',
  departmentId: '',
  roleId: '',
  joiningDate: '',
  annualSalary: '',
  status: 'Active',
}

function Field({ label, error, children, wide }) {
  return (
    <label className={`field${wide ? ' field-wide' : ''}${error ? ' has-error' : ''}`}>
      <span>{label}</span>
      {children}
      {error && <small className="error-text">{error.message}</small>}
    </label>
  )
}

export default function EmployeeFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const { isAdmin } = useAuth()
  const { departments, roles } = useLookups()
  const [employee, setEmployee] = useState(null)
  const [serverError, setServerError] = useState('')
  const [loadError, setLoadError] = useState('')

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(isEdit ? employeeUpdateSchema : employeeSchema),
    defaultValues: EMPTY,
  })

  useEffect(() => {
    if (!isEdit) return
    employeesApi
      .get(id)
      .then((emp) => {
        setEmployee(emp)
        const p = emp.personalDetails
        reset({
          fullName: emp.fullName,
          email: emp.email,
          mobile: emp.mobile,
          gender: emp.gender,
          // only admins get the real values back; everyone else leaves them blank to keep them
          aadhaar: p && !p.isMasked ? p.aadhaar : '',
          pan: p && !p.isMasked ? p.pan : '',
          dateOfBirth: p?.dateOfBirth ?? '',
          address: p?.address ?? '',
          departmentId: String(emp.departmentId),
          roleId: String(emp.roleId),
          joiningDate: emp.joiningDate,
          annualSalary: String(emp.annualSalary),
          status: emp.status,
        })
      })
      .catch((err) => setLoadError(getApiError(err).message))
  }, [id, isEdit, reset])

  async function onSubmit(values) {
    setServerError('')
    try {
      const saved = isEdit ? await employeesApi.update(id, values) : await employeesApi.create(values)
      navigate(`/employees/${saved.id}`, { state: { message: isEdit ? 'Employee updated.' : `Employee created with code ${saved.employeeCode}.` } })
    } catch (err) {
      const { message, fields } = getApiError(err)
      // show server-side errors (e.g. duplicate email) next to the right field
      Object.entries(fields).forEach(([name, msg]) => {
        if (name in EMPTY) setError(name, { type: 'server', message: msg })
      })
      setServerError(message)
    }
  }

  if (loadError) return <div className="alert alert-error">{loadError}</div>
  if (isEdit && !employee) return <p className="muted">Loading...</p>

  const masked = isEdit && employee?.personalDetails?.isMasked
  const aadhaarField = register('aadhaar')
  const panField = register('pan')

  return (
    <div>
      <div className="page-header">
        <h2>{isEdit ? `Edit ${employee.fullName}` : 'Add employee'}</h2>
        <Link to={isEdit ? `/employees/${id}` : '/employees'} className="btn btn-light">Cancel</Link>
      </div>

      {serverError && <div className="alert alert-error">{serverError}</div>}

      <form className="card form" onSubmit={handleSubmit(onSubmit)} noValidate>
        <h3 className="form-section">Personal details</h3>
        <div className="form-grid">
          <Field label="Employee code">
            <input value={isEdit ? employee.employeeCode : 'Auto-generated'} readOnly disabled />
          </Field>
          <Field label="Full name *" error={errors.fullName}>
            <input {...register('fullName')} />
          </Field>
          <Field label="Email *" error={errors.email}>
            <input type="email" {...register('email')} />
          </Field>
          <Field label="Mobile number *" error={errors.mobile}>
            <input inputMode="numeric" maxLength={10} {...register('mobile')} />
          </Field>
          <Field label="Date of birth *" error={errors.dateOfBirth}>
            <input type="date" min="1900-01-01" max={localToday({ years: -18 })} {...register('dateOfBirth')} />
          </Field>
          <Field label="Gender *" error={errors.gender}>
            <select {...register('gender')}>
              <option value="">Select</option>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
          </Field>
          <Field label={`Aadhaar number${masked ? '' : ' *'}`} error={errors.aadhaar}>
            <input
              inputMode="numeric"
              placeholder={masked ? `Leave blank to keep ${employee.personalDetails.aadhaar}` : 'XXXX-XXXX-XXXX'}
              maxLength={14}
              {...aadhaarField}
              onChange={(e) => {
                e.target.value = formatAadhaarInput(e.target.value)
                aadhaarField.onChange(e)
              }}
            />
          </Field>
          <Field label={`PAN${masked ? '' : ' *'}`} error={errors.pan}>
            <input
              placeholder={masked ? `Leave blank to keep ${employee.personalDetails.pan}` : 'ABCDE1234F'}
              maxLength={10}
              {...panField}
              onChange={(e) => {
                e.target.value = e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '')
                panField.onChange(e)
              }}
            />
          </Field>
          <Field label="Address *" error={errors.address} wide>
            <textarea rows={2} {...register('address')} />
          </Field>
        </div>

        <h3 className="form-section">Employment details</h3>
        <div className="form-grid">
          <Field label="Department *" error={errors.departmentId}>
            {isEdit ? (
              <>
                {/* disabled inputs are left out of the form values, so keep the id in a hidden field */}
                <input type="hidden" {...register('departmentId')} />
                <input value={employee.departmentName} readOnly disabled />
                <small className="hint">Use Transfer to change the department.</small>
              </>
            ) : (
              <select {...register('departmentId')}>
                <option value="">Select</option>
                {departments.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            )}
          </Field>
          <Field label="Role / designation *" error={errors.roleId}>
            <select {...register('roleId')}>
              <option value="">Select</option>
              {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </Field>
          <Field label="Joining date *" error={errors.joiningDate}>
            <input type="date" max={localToday({ years: 1 })} {...register('joiningDate')} />
          </Field>
          <Field label="Annual salary (₹) *" error={errors.annualSalary}>
            <input type="number" min="1" step="1000" {...register('annualSalary')} />
          </Field>
          <Field label="Status *" error={errors.status}>
            <select {...register('status')}>
              <option value="Active">Active</option>
              {/* only admins can deactivate (unless the employee is already inactive) */}
              {(isAdmin || employee?.status === 'Inactive') && <option value="Inactive">Inactive</option>}
              <option value="OnLeave">On leave</option>
            </select>
          </Field>
        </div>

        {isEdit && !isAdmin && (
          <p className="hint">Aadhaar and PAN are hidden for your role. Fill them in only if they need to change.</p>
        )}

        <div className="form-actions">
          <button className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : isEdit ? 'Save changes' : 'Create employee'}
          </button>
        </div>
      </form>
    </div>
  )
}
