// Turns whatever the user typed into XXXX-XXXX-XXXX (digits only, max 12)
export function formatAadhaarInput(value) {
  const digits = (value || '').replace(/\D/g, '').slice(0, 12)
  return digits.match(/.{1,4}/g)?.join('-') ?? ''
}

const rupees = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })

export function formatSalary(amount) {
  return rupees.format(amount)
}

export function formatDate(value) {
  if (!value) return ''
  const [y, m, d] = value.split('-')
  return `${d}/${m}/${y}`
}

// "below 3 LPA" is sent as max 2,99,999.99 because the API treats maxSalary as inclusive
export const SALARY_PRESETS = [
  { value: '1-3', label: '₹1–3 LPA', min: 100000, max: 299999.99 },
  { value: '3-5', label: '₹3–5 LPA', min: 300000, max: 499999.99 },
  { value: '5-10', label: '₹5–10 LPA', min: 500000, max: 999999.99 },
  { value: '10+', label: '₹10+ LPA', min: 1000000, max: undefined },
]

export function salaryRangeFor(preset) {
  const p = SALARY_PRESETS.find((x) => x.value === preset)
  return p ? { minSalary: p.min, maxSalary: p.max } : { minSalary: undefined, maxSalary: undefined }
}

export function presetFromRange(min, max) {
  if (min === undefined && max === undefined) return ''
  const p = SALARY_PRESETS.find((x) => x.min === min && x.max === max)
  return p ? p.value : 'custom'
}

export function cleanParams(params) {
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== '' && v !== undefined && v !== null)
  )
}
