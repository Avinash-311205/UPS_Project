import React from 'react'

interface ModalProps {
  isOpen: boolean
  title: string
  onClose: () => void
  children: React.ReactNode
  footer?: React.ReactNode
}

export default function Modal({ isOpen, title, onClose, children, footer }: ModalProps) {
  if (!isOpen) return null

  return (
    <div className="cc-modal-overlay" onClick={onClose}>
      <div className="cc-modal" onClick={(e) => e.stopPropagation()}>
        <div className="cc-modal-header">
          <h2 className="cc-modal-title">{title}</h2>
          <button
            type="button"
            className="cc-button cc-button-sm"
            onClick={onClose}
            aria-label="Close dialog"
          >
            ✕
          </button>
        </div>

        <div className="cc-modal-body">{children}</div>

        {footer && <div className="cc-modal-footer">{footer}</div>}
      </div>
    </div>
  )
}
