import React from 'react'

export function LoadingSpinner({ label = 'PROCESSING TELEMETRY...' }: { label?: string }) {
  return (
    <div className="cc-loading">
      <span className="cc-pulse-dot" />
      <span>{label}</span>
    </div>
  )
}

export function ErrorBanner({
  message,
  onDismiss,
}: {
  message: string
  onDismiss?: () => void
}) {
  return (
    <div className="cc-error-banner">
      <div>
        <strong style={{ marginRight: '8px' }}>[ALERT // ERROR]</strong>
        {message}
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="cc-button cc-button-sm cc-button-danger"
          style={{ padding: '2px 6px' }}
        >
          DISMISS
        </button>
      )}
    </div>
  )
}

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string
  description: string
  action?: React.ReactNode
}) {
  return (
    <div className="cc-empty-state">
      <div className="cc-empty-title">// {title}</div>
      <p className="cc-empty-desc">{description}</p>
      {action && <div>{action}</div>}
    </div>
  )
}
