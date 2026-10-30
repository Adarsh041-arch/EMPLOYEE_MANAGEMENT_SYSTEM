export default function Pagination({ pageNumber, totalPages, onChange }) {
  if (totalPages <= 1) return null

  // show first, last and a couple of pages around the current one
  const pages = []
  for (let p = 1; p <= totalPages; p++) {
    if (p === 1 || p === totalPages || Math.abs(p - pageNumber) <= 1) pages.push(p)
    else if (pages[pages.length - 1] !== '...') pages.push('...')
  }

  return (
    <nav className="pagination">
      <button className="btn btn-light btn-small" disabled={pageNumber <= 1} onClick={() => onChange(pageNumber - 1)}>
        Prev
      </button>
      {pages.map((p, i) =>
        p === '...' ? (
          <span key={`gap${i}`} className="page-gap">...</span>
        ) : (
          <button
            key={p}
            className={`btn btn-small ${p === pageNumber ? 'btn-primary' : 'btn-light'}`}
            onClick={() => onChange(p)}
          >
            {p}
          </button>
        )
      )}
      <button className="btn btn-light btn-small" disabled={pageNumber >= totalPages} onClick={() => onChange(pageNumber + 1)}>
        Next
      </button>
    </nav>
  )
}
