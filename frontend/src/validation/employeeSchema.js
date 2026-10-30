import { z } from 'zod'

// same rules as the backend validator, so most mistakes are caught before submitting

const requiredId = (label) =>
  z.coerce.number({ invalid_type_error: `${label} is required` }).int().positive(`${label} is required`)

const today = () => new Date().toISOString().slice(0, 10)

export const employeeSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(1, 'Full name is required')
    .max(100, 'Full name is too long')
    .regex(/^[A-Za-z ]+$/, 'Only letters and spaces are allowed'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
  mobile: z.string().regex(/^\d{10}$/, 'Mobile number must be exactly 10 digits'),
  gender: z.string().min(1, 'Gender is required'),
  aadhaar: z.string().regex(/^\d{4}-\d{4}-\d{4}$/, 'Aadhaar must be 12 digits (XXXX-XXXX-XXXX)'),
  pan: z.string().regex(/^[A-Z]{5}[0-9]{4}[A-Z]$/, 'PAN must look like ABCDE1234F'),
  dateOfBirth: z
    .string()
    .min(1, 'Date of birth is required')
    .refine((d) => d <= today(), 'Date of birth cannot be in the future'),
  address: z.string().trim().min(1, 'Address is required').max(300, 'Address is too long'),
  departmentId: requiredId('Department'),
  roleId: requiredId('Role'),
  joiningDate: z.string().min(1, 'Joining date is required'),
  annualSalary: z.coerce
    .number({ invalid_type_error: 'Annual salary is required' })
    .positive('Annual salary must be greater than zero')
    .max(9999999999.99, 'Annual salary is too large')
    .refine((v) => /^\d+(\.\d{1,2})?$/.test(String(v)), 'Annual salary can have at most 2 decimal places'),
  status: z.enum(['Active', 'Inactive', 'OnLeave'], { message: 'Status is required' }),
})

// when editing, Aadhaar / PAN can be left blank to keep what is already saved
export const employeeUpdateSchema = employeeSchema.extend({
  aadhaar: employeeSchema.shape.aadhaar.or(z.literal('')),
  pan: employeeSchema.shape.pan.or(z.literal('')),
})

export const transferSchema = z.object({
  newDepartmentId: requiredId('New department'),
  transferDate: z.string().min(1, 'Transfer date is required'),
  reason: z.string().trim().min(1, 'Reason is required').max(300, 'Reason is too long'),
})
