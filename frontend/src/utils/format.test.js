import { describe, it, expect } from 'vitest'
import { formatAadhaarInput, formatSalary, SALARY_PRESETS, salaryRangeFor, presetFromRange, cleanParams } from './format'

describe('formatAadhaarInput', () => {
  it('adds dashes while typing', () => {
    expect(formatAadhaarInput('1234')).toBe('1234')
    expect(formatAadhaarInput('12345')).toBe('1234-5')
    expect(formatAadhaarInput('12345678')).toBe('1234-5678')
    expect(formatAadhaarInput('123456789012')).toBe('1234-5678-9012')
  })

  it('drops non-digits and anything past 12 digits', () => {
    expect(formatAadhaarInput('12ab34-56 78')).toBe('1234-5678')
    expect(formatAadhaarInput('1234567890123456')).toBe('1234-5678-9012')
  })
})

describe('formatSalary', () => {
  it('uses Indian grouping and rupee sign', () => {
    expect(formatSalary(1250000)).toBe('₹12,50,000')
    expect(formatSalary(450000)).toBe('₹4,50,000')
  })
})

describe('salary presets', () => {
  it('match the ranges from the brief', () => {
    expect(salaryRangeFor('1-3')).toEqual({ minSalary: 100000, maxSalary: 299999.99 })
    expect(salaryRangeFor('3-5')).toEqual({ minSalary: 300000, maxSalary: 499999.99 })
    expect(salaryRangeFor('5-10')).toEqual({ minSalary: 500000, maxSalary: 999999.99 })
    expect(salaryRangeFor('10+')).toEqual({ minSalary: 1000000, maxSalary: undefined })
    expect(salaryRangeFor('')).toEqual({ minSalary: undefined, maxSalary: undefined })
  })

  it('can work out the preset from min/max in the url', () => {
    for (const p of SALARY_PRESETS) {
      const range = salaryRangeFor(p.value)
      expect(presetFromRange(range.minSalary, range.maxSalary)).toBe(p.value)
    }
    expect(presetFromRange(200000, 800000)).toBe('custom')
    expect(presetFromRange(undefined, undefined)).toBe('')
  })
})

describe('cleanParams', () => {
  it('removes empty values', () => {
    expect(cleanParams({ a: '', b: undefined, c: null, d: 0, e: 'x' })).toEqual({ d: 0, e: 'x' })
  })
})
