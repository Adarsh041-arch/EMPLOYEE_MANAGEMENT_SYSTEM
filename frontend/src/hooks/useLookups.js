import { useEffect, useState } from 'react'
import { lookupsApi } from '../api/client'

// departments + roles for dropdowns
export default function useLookups() {
  const [departments, setDepartments] = useState([])
  const [roles, setRoles] = useState([])

  useEffect(() => {
    Promise.all([lookupsApi.departments(), lookupsApi.roles()])
      .then(([d, r]) => {
        setDepartments(d)
        setRoles(r)
      })
      .catch(() => {})
  }, [])

  return { departments, roles }
}
