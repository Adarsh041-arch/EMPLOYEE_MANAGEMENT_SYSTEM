import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { employeesApi, getApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import useLookups from '../hooks/useLookups'
import Pagination from '../components/Pagination'
import StatusBadge from '../components/StatusBadge'
import { SALARY_PRESETS, cleanParams, formatSalary, presetFromRange, salaryRangeFor } from '../utils/format'

const COLUMNS = [
  { key: 'code', label: 'Code' },
  { key: 'name', label: 'Name' },
  { key: 'department', label: 'Department' },
  { key: 'role', label: 'Role' },
  { key: 'email', label: 'Email' },
  { key: 'mobile', label: 'Mobile' },
  { key: 'salary', label: 'Salary' },
  { key: 'status', label: 'Status' },
]

const FILTER_KEYS = ['departmentId', 'roleId', 'status', 'gender', 'joiningFrom', 'joiningTo', 'minSalary', 'maxSalary']

const toNumber = (v) => (v === null || v === '' ? undefined : Number(v))

export default function EmployeeListPage() {
  const { canEdit, isAdmin } = useAuth()
  const { departments, roles } = useLookups()
  // all filters live in the URL so they survive a refresh and can be shared
  const [params, setParams] = useSearchParams()

  const query = {
    search: params.get('search') ?? '',
    departmentId: params.get('departmentId') ?? '',
    roleId: params.get('roleId') ?? '',
    status: params.get('status') ?? '',
    gender: params.get('gender') ?? '',
    joiningFrom: params.get('joiningFrom') ?? '',
    joiningTo: params.get('joiningTo') ?? '',
    minSalary: toNumber(params.get('minSalary')),
    maxSalary: toNumber(params.get('maxSalary')),
    sortBy: params.get('sortBy') ?? 'code',
    sortDir: params.get('sortDir') ?? 'asc',
    pageNumber: Number(params.get('pageNumber') ?? 1),
    pageSize: Number(params.get('pageSize') ?? 10),
  }
  const queryKey = params.toString()

  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [searchText, setSearchText] = useState(query.search)
  const [showFilters, setShowFilters] = useState(() => FILTER_KEYS.some((k) => params.get(k)))
  // "custom" picked but no min/max typed yet - the URL alone can't tell us that
  const [customSalary, setCustomSalary] = useState(false)
  const urlPreset = presetFromRange(query.minSalary, query.maxSalary)
  const salaryMode = customSalary && urlPreset === '' ? 'custom' : urlPreset
  // last search value this page wrote to the URL, so we can tell when the URL
  // changed from outside (back button, header link) and update the box to match
  const pushedSearch = useRef(query.search)

  // update some params and go back to page 1 unless the page itself is changing
  const updateParams = useCallback(
    (changes) => {
      setParams((prev) => {
        const next = Object.fromEntries(prev.entries())
        Object.assign(next, changes)
        if (!('pageNumber' in changes)) next.pageNumber = 1
        return cleanParams(next)
      })
    },
    [setParams]
  )

  const load = useCallback(() => {
    setLoading(true)
    setError('')
    employeesApi
      .list(cleanParams(Object.fromEntries(new URLSearchParams(queryKey))))
      .then(setData)
      .catch((err) => setError(getApiError(err).message))
      .finally(() => setLoading(false))
  }, [queryKey])

  useEffect(load, [load])

  useEffect(() => {
    if (query.search !== pushedSearch.current) {
      pushedSearch.current = query.search
      setSearchText(query.search)
    }
  }, [query.search])

  // wait until the user stops typing before searching
  useEffect(() => {
    const value = searchText.trim()
    if (value === pushedSearch.current) return
    const t = setTimeout(() => {
      pushedSearch.current = value
      updateParams({ search: value })
    }, 400)
    return () => clearTimeout(t)
  }, [searchText, updateParams])

  function handleSort(key) {
    const dir = query.sortBy === key && query.sortDir === 'asc' ? 'desc' : 'asc'
    updateParams({ sortBy: key, sortDir: dir })
  }

  function handleSalaryPreset(value) {
    setCustomSalary(value === 'custom')
    if (value !== 'custom') updateParams(salaryRangeFor(value))
  }

  function clearFilters() {
    pushedSearch.current = ''
    setSearchText('')
    setCustomSalary(false)
    setParams(cleanParams({ pageSize: query.pageSize, sortBy: query.sortBy, sortDir: query.sortDir }))
  }

  async function handleDeactivate(emp) {
    if (!window.confirm(`Deactivate ${emp.fullName} (${emp.employeeCode})?`)) return
    try {
      await employeesApi.deactivate(emp.id)
      setMessage(`${emp.fullName} has been deactivated.`)
      load()
    } catch (err) {
      setError(getApiError(err).message)
    }
  }

  const activeFilterCount = FILTER_KEYS.filter((k) => params.get(k)).length
  const from = data && data.totalCount > 0 ? (data.pageNumber - 1) * data.pageSize + 1 : 0
  const to = data ? Math.min(data.pageNumber * data.pageSize, data.totalCount) : 0

  return (
    <div>
      <div className="page-header">
        <h2>Employees</h2>
        {canEdit && <Link to="/employees/new" className="btn btn-primary">+ Add employee</Link>}
      </div>

      {message && <div className="alert alert-success" onClick={() => setMessage('')}>{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="toolbar">
        <input
          className="search-input"
          type="search"
          placeholder="Search by code, name, email, mobile, department, role, address or status"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <button className="btn btn-light" onClick={() => setShowFilters((s) => !s)}>
          Filters{activeFilterCount > 0 ? ` (${activeFilterCount})` : ''}
        </button>
        {(activeFilterCount > 0 || query.search) && (
          <button className="btn btn-link" onClick={clearFilters}>Clear all</button>
        )}
      </div>

      {showFilters && (
        <div className="filters card">
          <label className="field">
            <span>Department</span>
            <select value={query.departmentId} onChange={(e) => updateParams({ departmentId: e.target.value })}>
              <option value="">All</option>
              {departments.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </label>
          <label className="field">
            <span>Role</span>
            <select value={query.roleId} onChange={(e) => updateParams({ roleId: e.target.value })}>
              <option value="">All</option>
              {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </label>
          <label className="field">
            <span>Status</span>
            <select value={query.status} onChange={(e) => updateParams({ status: e.target.value })}>
              <option value="">All</option>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
              <option value="OnLeave">On leave</option>
            </select>
          </label>
          <label className="field">
            <span>Gender</span>
            <select value={query.gender} onChange={(e) => updateParams({ gender: e.target.value })}>
              <option value="">All</option>
              <option value="Male">Male</option>
              <option value="Female">Female</option>
              <option value="Other">Other</option>
            </select>
          </label>
          <label className="field">
            <span>Joined from</span>
            <input type="date" value={query.joiningFrom} onChange={(e) => updateParams({ joiningFrom: e.target.value })} />
          </label>
          <label className="field">
            <span>Joined to</span>
            <input type="date" value={query.joiningTo} onChange={(e) => updateParams({ joiningTo: e.target.value })} />
          </label>
          <label className="field">
            <span>Annual salary</span>
            <select value={salaryMode} onChange={(e) => handleSalaryPreset(e.target.value)}>
              <option value="">Any</option>
              {SALARY_PRESETS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
              <option value="custom">Custom</option>
            </select>
          </label>
          {salaryMode === 'custom' && (
            <div className="field salary-custom">
              <span>Custom range (₹)</span>
              {/* key makes the inputs pick up new values when the URL changes (e.g. back button) */}
              <div className="inline-inputs" key={`${query.minSalary}-${query.maxSalary}`}>
                <input
                  type="number"
                  min="0"
                  placeholder="Min"
                  defaultValue={query.minSalary ?? ''}
                  onBlur={(e) => updateParams({ minSalary: e.target.value })}
                />
                <input
                  type="number"
                  min="0"
                  placeholder="Max"
                  defaultValue={query.maxSalary ?? ''}
                  onBlur={(e) => updateParams({ maxSalary: e.target.value })}
                />
              </div>
            </div>
          )}
        </div>
      )}

      <div className="table-meta">
        <span>
          {data ? (data.totalCount === 0 ? 'No matching employees' : `Showing ${from}–${to} of ${data.totalCount}`) : ''}
        </span>
        <label className="page-size">
          Rows per page
          <select value={query.pageSize} onChange={(e) => updateParams({ pageSize: e.target.value })}>
            {[5, 10, 20].map((n) => <option key={n} value={n}>{n}</option>)}
          </select>
        </label>
      </div>

      <div className="table-wrap card">
        <table className="table">
          <thead>
            <tr>
              {COLUMNS.map((c) => (
                <th key={c.key}>
                  <button className="sort-btn" onClick={() => handleSort(c.key)}>
                    {c.label}
                    <span className="sort-arrow">
                      {query.sortBy === c.key ? (query.sortDir === 'asc' ? '▲' : '▼') : ''}
                    </span>
                  </button>
                </th>
              ))}
              <th>Actions</th>
            </tr>
          </thead>
          <tbody className={loading ? 'is-loading' : ''}>
            {data?.items.map((e) => (
              <tr key={e.id}>
                <td data-label="Code"><Link to={`/employees/${e.id}`} className="code-link">{e.employeeCode}</Link></td>
                <td data-label="Name">{e.fullName}</td>
                <td data-label="Department">{e.departmentName}</td>
                <td data-label="Role">{e.roleName}</td>
                <td data-label="Email">{e.email}</td>
                <td data-label="Mobile">{e.mobile}</td>
                <td data-label="Salary" className="num">{formatSalary(e.annualSalary)}</td>
                <td data-label="Status"><StatusBadge status={e.status} /></td>
                <td data-label="Actions" className="actions">
                  <Link to={`/employees/${e.id}`}>View</Link>
                  {canEdit && <Link to={`/employees/${e.id}/edit`}>Edit</Link>}
                  {canEdit && <Link to={`/employees/${e.id}/transfer`}>Transfer</Link>}
                  {isAdmin && e.status !== 'Inactive' && (
                    <button className="link-danger" onClick={() => handleDeactivate(e)}>Deactivate</button>
                  )}
                </td>
              </tr>
            ))}
            {data && data.items.length === 0 && (
              <tr>
                <td colSpan={COLUMNS.length + 1} className="empty">No employees match your search and filters.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {data && (
        <Pagination
          pageNumber={data.pageNumber}
          totalPages={data.totalPages}
          onChange={(p) => updateParams({ pageNumber: p })}
        />
      )}
    </div>
  )
}
