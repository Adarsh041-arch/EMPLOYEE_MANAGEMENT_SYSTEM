import { describe, it, expect } from 'vitest'
import { employeeSchema, employeeUpdateSchema, transferSchema } from './employeeSchema'

const valid = {
  fullName: 'Rahul Sharma',
  email: 'rahul@example.com',
  mobile: '9876543210',
  gender: 'Male',
  aadhaar: '1234-5678-9012',
  pan: 'ABCDE1234F',
  dateOfBirth: '1998-05-14',
  address: 'MG Road, Pune',
  departmentId: '1',
  roleId: '2',
  joiningDate: '2023-01-10',
  annualSalary: '450000',
  status: 'Active',
}

const errorsFor = (data) => {
  const result = employeeSchema.safeParse(data)
  return result.success ? [] : result.error.issues.map((i) => i.path[0])
}

describe('employeeSchema', () => {
  it('accepts a valid employee and converts numbers', () => {
    const result = employeeSchema.safeParse(valid)
    expect(result.success).toBe(true)
    expect(result.data.departmentId).toBe(1)
    expect(result.data.annualSalary).toBe(450000)
  })

  it.each([
    ['fullName', 'Rahul 2'],
    ['fullName', ''],
    ['email', 'not-an-email'],
    ['mobile', '98765'],
    ['mobile', '98765432101'],
    ['aadhaar', '1234-5678-901'],
    ['pan', 'abcde1234f'],
    ['pan', 'ABCDE12345'],
    ['annualSalary', '0'],
    ['annualSalary', '-5'],
    ['annualSalary', '0.001'],
    ['annualSalary', '450000.555'],
    ['departmentId', ''],
    ['roleId', ''],
    ['gender', ''],
    ['address', ''],
    ['joiningDate', ''],
  ])('rejects bad %s (%s)', (field, value) => {
    expect(errorsFor({ ...valid, [field]: value })).toContain(field)
  })

  it('rejects a date of birth in the future', () => {
    const next = new Date()
    next.setDate(next.getDate() + 2)
    expect(errorsFor({ ...valid, dateOfBirth: next.toISOString().slice(0, 10) })).toContain('dateOfBirth')
  })
})

describe('employeeUpdateSchema', () => {
  it('allows Aadhaar and PAN to be left empty (keeps stored values)', () => {
    expect(employeeUpdateSchema.safeParse({ ...valid, aadhaar: '', pan: '' }).success).toBe(true)
  })

  it('still checks them when they are filled in', () => {
    const result = employeeUpdateSchema.safeParse({ ...valid, aadhaar: '1234', pan: 'abc' })
    const messages = Object.fromEntries(result.error.issues.map((i) => [i.path[0], i.message]))
    expect(messages.aadhaar).toBe('Aadhaar must be 12 digits (XXXX-XXXX-XXXX)')
    expect(messages.pan).toBe('PAN must look like ABCDE1234F')
  })
})

describe('transferSchema', () => {
  it('needs department, date and reason', () => {
    const result = transferSchema.safeParse({ newDepartmentId: '', transferDate: '', reason: ' ' })
    const fields = result.error.issues.map((i) => i.path[0])
    expect(fields).toEqual(expect.arrayContaining(['newDepartmentId', 'transferDate', 'reason']))
  })
})
