const labels = { Active: 'Active', Inactive: 'Inactive', OnLeave: 'On leave' }

export default function StatusBadge({ status }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{labels[status] ?? status}</span>
}
