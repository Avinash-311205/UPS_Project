import React from 'react'
import type { AuthUser } from '../auth/session'
import type { Page } from '../types'

interface AppLayoutProps {
  user: AuthUser
  currentPage: Page
  onPageChange: (page: Page) => void
  onLogout: () => void
  children: React.ReactNode
}

const navItems: { page: Page; index: string; label: string }[] = [
  { page: 'Overview', index: '01', label: 'Overview' },
  { page: 'Systems', index: '02', label: 'Systems' },
  { page: 'Dependencies', index: '03', label: 'Dependencies' },
  { page: 'Operations', index: '04', label: 'Business Ops' },
  { page: 'Incidents', index: '05', label: 'Incidents' },
  { page: 'ImpactAnalysis', index: '06', label: 'Impact Analysis' },
]

export default function AppLayout({
  user,
  currentPage,
  onPageChange,
  onLogout,
  children,
}: AppLayoutProps) {
  const currentNav = navItems.find((item) => item.page === currentPage) || navItems[0]

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
          {navItems.map((item) => {
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
