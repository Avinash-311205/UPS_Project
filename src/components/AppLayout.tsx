import React, { useMemo } from 'react'
import type { AuthUser } from '../auth/session'
import type { Page } from '../types'

interface AppLayoutProps {
  user: AuthUser
  currentPage: Page
  onPageChange: (page: Page) => void
  onLogout: () => void
  children: React.ReactNode
}

export default function AppLayout({
  user,
  currentPage,
  onPageChange,
  onLogout,
  children,
}: AppLayoutProps) {
  const visibleNavItems = useMemo(() => {
    const items = [
      { page: 'Overview' as Page, index: '01', label: 'Overview' },
      { page: 'Systems' as Page, index: '02', label: 'Systems' },
      { page: 'Dependencies' as Page, index: '03', label: 'Dependencies' },
      { page: 'Operations' as Page, index: '04', label: 'Business Ops' },
      { page: 'Incidents' as Page, index: '05', label: 'Incidents' },
      { page: 'ImpactAnalysis' as Page, index: '06', label: 'Impact Analysis' },
    ]
    if (user.accountType === 'Root') {
      items.push({ page: 'IamManagement' as Page, index: '07', label: 'IAM Management' })
    }
    return items
  }, [user.accountType])

  const currentNav = visibleNavItems.find((item) => item.page === currentPage) || visibleNavItems[0]

  return (
    <div className="cc-app">
      {/* Sidebar */}
      <aside className="cc-sidebar">
        <div className="cc-sidebar-header">
          <span className="cc-brand-badge">RX</span>
          <div>
            <div className="cc-brand-title">ResilienceX</div>
            <span className="cc-brand-sub">CYBER OPS COMMAND</span>
          </div>
        </div>

        <div className="cc-nav-section-title">// NAVIGATION MATRIX</div>

        <ul className="cc-nav-list">
          {visibleNavItems.map((item) => {
            const isActive = currentPage === item.page
            return (
              <li key={item.page} className="cc-nav-item">
                <button
                  type="button"
                  className={`cc-nav-button ${isActive ? 'active' : ''}`}
                  onClick={() => onPageChange(item.page)}
                >
                  <span className="cc-nav-index">{item.index} //</span>
                  <span>{item.label}</span>
                </button>
              </li>
            )
          })}
        </ul>

        <div className="cc-sidebar-footer">
          <div className="cc-tenant-pill">
            <span className="cc-pulse-dot" />
            <span>LIVE TELEMETRY ACTIVE</span>
          </div>
          <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>
            NODE: CLUSTER-US-EAST-01
          </div>
        </div>
      </aside>

      {/* Main Workspace Area */}
      <div className="cc-main-container">
        {/* Topbar */}
        <header className="cc-topbar">
          <div className="cc-breadcrumbs">
            <span>RESILIENCEX</span>
            <span>/</span>
            <span>COMMAND CENTRE</span>
            <span>/</span>
            <span className="highlight">{currentNav.index} // {currentNav.label.toUpperCase()}</span>
          </div>

          <div className="cc-topbar-actions">
            <div className="cc-user-badge">
              <span
                style={{
                  display: 'inline-block',
                  padding: '1px 6px',
                  borderRadius: '3px',
                  fontSize: '9px',
                  fontWeight: 700,
                  fontFamily: 'var(--cc-font-mono)',
                  letterSpacing: '0.06em',
                  background: user.accountType === 'Root' ? 'rgba(239, 68, 68, 0.2)' : 'rgba(59, 130, 246, 0.2)',
                  color: user.accountType === 'Root' ? '#f87171' : '#60a5fa',
                  border: user.accountType === 'Root' ? '1px solid rgba(239, 68, 68, 0.4)' : '1px solid rgba(59, 130, 246, 0.4)',
                }}
              >
                {user.accountType === 'Root' ? 'ROOT' : 'IAM'}
              </span>
              <span>OP:</span>
              <strong>{user.name}</strong>
              <span style={{ color: '#555555' }}>|</span>
              <span style={{ color: '#888888', fontSize: '10px' }}>{user.email}</span>
            </div>

            <button
              type="button"
              className="cc-button cc-button-sm"
              onClick={onLogout}
              title="Terminate session"
            >
              [ SIGNOUT ]
            </button>
          </div>
        </header>

        {/* Dynamic Page Content */}
        <main className="cc-workspace">
          {children}
        </main>
      </div>
    </div>
  )
}
