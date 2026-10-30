import { z } from 'zod'

// same rules as the backend validator, so most mistakes are caught before submitting

const requiredId = (label) =>
  z.coerce.number({ invalid_type_error: `${label} is required` }).int().positive(`${label} is required`)

const pad = (n) => String(n).padStart(2, '0')

// yyyy-mm-dd in the user's own timezone (toISOString would give the UTC date,
// which is still "yesterday" in India until 5:30 am)
export function localToday(offset = {}) {
  const d = new Date()
  if (offset.years) d.setFullYear(d.getFullYear() + offset.years)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

const aadhaar = z.string().regex(/^\d{4}-\d{4}-\d{4}$/, 'Aadhaar must be 12 digits (XXXX-XXXX-XXXX)')
const pan = z.string().regex(/^[A-Z]{5}[0-9]{4}[A-Z]$/, 'PAN must look like ABCDE1234F')

const employeeFields = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, 'Full name is required')
    .max(100, 'Full name is too long')
    .regex(/^[A-Za-z ]+$/, 'Only letters and spaces are allowed'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
  mobile: z.string().regex(/^\d{10}$/, 'Mobile number must be exactly 10 digits'),
  gender: z.string().min(1, 'Gender is required'),
  aadhaar,
  pan,
  dateOfBirth: z
    .string()
    .min(1, 'Date of birth is required')
    .refine((d) => d <= localToday(), 'Date of birth cannot be in the future')
    .refine((d) => d > localToday() || d <= localToday({ years: -18 }), 'Employee must be at least 18 years old')
    .refine((d) => d >= '1900-01-01', 'Enter a valid date of birth'),
  address: z.string().trim().min(1, 'Address is required').max(300, 'Address is too long'),
  departmentId: requiredId('Department'),
  roleId: requiredId('Role'),
  joiningDate: z
    .string()
    .min(1, 'Joining date is required')
    .refine((d) => d <= localToday({ years: 1 }), "Joining date can't be more than a year from today"),
  annualSalary: z.coerce
    .number({ invalid_type_error: 'Annual salary is required' })
    .positive('Annual salary must be greater than zero')
    .max(9999999999.99, 'Annual salary is too large')
    .refine((v) => /^\d+(\.\d{1,2})?$/.test(String(v)), 'Annual salary can have at most 2 decimal places'),
  status: z.enum(['Active', 'Inactive', 'OnLeave'], { message: 'Status is required' }),
})

// rules that compare two fields
function checkDates(data, ctx) {
  if (data.dateOfBirth && data.joiningDate && data.joiningDate <= data.dateOfBirth) {
    ctx.addIssue({ code: 'custom', path: ['joiningDate'], message: 'Joining date must be after the date of birth' })
  }
}

export const employeeSchema = employeeFields.superRefine(checkDates)

// when editing, Aadhaar / PAN can be left blank to keep what is already saved
export const employeeUpdateSchema = employeeFields
  .extend({
    aadhaar: aadhaar.or(z.literal('')),
    pan: pan.or(z.literal('')),
  })
  .superRefine(checkDates)

export function makeTransferSchema(joiningDate) {
  return z.object({
    newDepartmentId: requiredId('New department'),
    transferDate: z
      .string()
      .min(1, 'Transfer date is required')
      .refine((d) => !joiningDate || d >= joiningDate, "Transfer date can't be before the joining date"),
    reason: z.string().trim().min(1, 'Reason is required').max(300, 'Reason is too long'),
  })
}

export const transferSchema = makeTransferSchema()
