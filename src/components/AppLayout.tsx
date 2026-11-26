import React, { useMemo, useState } from 'react'
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
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const visibleNavItems = useMemo(() => {
    const items = [
      { page: 'Overview' as Page, index: '01', label: 'OVERVIEW' },
      { page: 'Systems' as Page, index: '02', label: 'SYSTEMS' },
      { page: 'Dependencies' as Page, index: '03', label: 'DEPENDENCIES' },
      { page: 'Operations' as Page, index: '04', label: 'OPERATIONS' },
      { page: 'Incidents' as Page, index: '05', label: 'INCIDENTS' },
      { page: 'ImpactAnalysis' as Page, index: '06', label: 'IMPACT ANALYSIS' },
    ]
    if (user.accountType === 'Root') {
      items.push({ page: 'IamManagement' as Page, index: '07', label: 'IAM' })
    }
    return items
  }, [user.accountType])

  const currentNav = visibleNavItems.find((item) => item.page === currentPage) || visibleNavItems[0]

  function handleSelectPage(page: Page) {
    onPageChange(page)
    setMobileMenuOpen(false)
  }

  return (
    <div className="ed-shell">
      {/* Editorial Horizontal Header */}
      <header className="ed-header" role="banner">
        {/* Brand Lockup */}
        <div
          className="ed-brand"
          onClick={() => handleSelectPage('Overview')}
          title="ResilienceX Operational Intelligence"
        >
          <span className="ed-brand-title">
            ResilienceX<span className="ed-brand-dot">.</span>
          </span>
          <span className="ed-brand-sub">OPERATIONAL INTELLIGENCE</span>
        </div>

        {/* Minimal Horizontal Navigation Matrix (Desktop) */}
        <nav className="ed-nav-desktop" aria-label="Primary Navigation">
          <ul className="ed-nav-list">
            {visibleNavItems.map((item) => {
              const isActive = currentPage === item.page
              return (
                <li key={item.page} className="ed-nav-item">
                  <button
                    type="button"
                    className={`ed-nav-link ${isActive ? 'active' : ''}`}
                    onClick={() => handleSelectPage(item.page)}
                    aria-current={isActive ? 'page' : undefined}
                  >
                    <span className="ed-nav-index">{item.index}</span>
                    <span className="ed-nav-label">{item.label}</span>
                    {isActive && <span className="ed-nav-active-pip" aria-hidden="true" />}
                  </button>
                </li>
              )
            })}
          </ul>
        </nav>

        {/* Utility / Telemetry / User Cluster */}
        <div className="ed-utilities">
          {/* Live Telemetry Pulse */}
          <div className="ed-telemetry-pill" title="Live Telemetry Link Active">
            <span className="ed-pulse-dot" />
            <span className="ed-telemetry-text">LIVE STREAM</span>
          </div>

          {/* Tenant / Cluster Monospace Readout */}
          <div className="ed-tenant-meta">
            <span className="ed-cluster-tag">CLUSTER-US-EAST-01</span>
            <span className="ed-sep">/</span>
            <span className="ed-tenant-tag">
              {user.tenantId ? `TENANT-${user.tenantId.slice(0, 8).toUpperCase()}` : 'ENTERPRISE'}
            </span>
          </div>

          {/* User Account Role Pill */}
          <div className="ed-user-badge">
            <span
              className={`ed-role-tag ${user.accountType === 'Root' ? 'root' : 'iam'}`}
            >
              {user.accountType === 'Root' ? 'ROOT' : 'IAM'}
            </span>
            <span className="ed-user-name" title={user.email}>
              {user.name}
            </span>
          </div>

          {/* Signout Button */}
          <button
            type="button"
            className="ed-button ed-button-sm"
            onClick={onLogout}
            title="Terminate session"
          >
            [ SIGNOUT ]
          </button>

          {/* Mobile Hamburger Trigger */}
          <button
            type="button"
            className="ed-mobile-toggle"
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            aria-label="Toggle navigation menu"
          >
            {mobileMenuOpen ? '[ ✕ ]' : '[ MENU ]'}
          </button>
        </div>
      </header>

      {/* Editorial Sub-bar: Breadcrumb & Contextual Monospace Track */}
      <div className="ed-subbar">
        <div className="ed-breadcrumbs">
          <span className="ed-crumb-prefix">RESILIENCEX</span>
          <span className="ed-crumb-divider">/</span>
          <span className="ed-crumb-section">INTELLIGENCE MATRIX</span>
          <span className="ed-crumb-divider">/</span>
          <span className="ed-crumb-current">
            {currentNav.index} {currentNav.label}
          </span>
        </div>

        <div className="ed-subbar-status">
          <span className="ed-status-indicator">SYSTEM STATUS: OPTIMAL</span>
          <span className="ed-sep">|</span>
          <span>LATENCY: 4.2ms</span>
          <span className="ed-sep">|</span>
          <span>GRAPH ENG: ACTIVE</span>
        </div>
      </div>

      {/* Restrained Mobile Navigation Drawer */}
      {mobileMenuOpen && (
        <div className="ed-mobile-drawer" role="dialog" aria-modal="true">
          <div className="ed-mobile-drawer-header">
            <span className="ed-brand-title">
              ResilienceX<span className="ed-brand-dot">.</span>
            </span>
            <button
              type="button"
              className="ed-button ed-button-sm"
              onClick={() => setMobileMenuOpen(false)}
            >
              [ ✕ CLOSE ]
            </button>
          </div>

          <nav className="ed-mobile-nav">
            {visibleNavItems.map((item) => {
              const isActive = currentPage === item.page
              return (
                <button
                  key={item.page}
                  type="button"
                  className={`ed-mobile-nav-link ${isActive ? 'active' : ''}`}
                  onClick={() => handleSelectPage(item.page)}
                >
                  <span className="ed-mobile-index">{item.index}</span>
                  <span className="ed-mobile-label">{item.label}</span>
                  {isActive && <span className="ed-mobile-active-marker">●</span>}
                </button>
              )
            })}
          </nav>

          <div className="ed-mobile-drawer-footer">
            <div className="ed-mobile-meta">
              <div>CLUSTER: US-EAST-01</div>
              <div>TENANT: {user.tenantId ? user.tenantId.slice(0, 16) + '...' : 'DEFAULT'}</div>
              <div>OPERATOR: {user.name} ({user.accountType})</div>
            </div>
            <button
              type="button"
              className="ed-button ed-button-primary"
              style={{ width: '100%', marginTop: '16px' }}
              onClick={onLogout}
            >
              [ SIGNOUT ]
            </button>
          </div>
        </div>
      )}

      {/* Dynamic Editorial Workspace */}
      <main className="ed-workspace" role="main">
        {children}
      </main>
    </div>
  )
}
