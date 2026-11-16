import { useMemo, useState } from 'react'
import type { AuthUser } from './auth/mockAuth'
import { roleLabels } from './auth/mockAuth'
import { currentDateLabel, currentYear, initialShipments, navItems, operatorShipmentIds, pagesByRole, workflowStorageKeys } from './data/mockData'
import { restoreApproval, restoreSystems, roleCanPerformAction } from './lib/workflow'
import type { Modal, Page, Shipment, System } from './types'
import Icon from './components/Icon'
import ActionModal from './components/ActionModal'
import OverviewPage from './pages/Overview'
import SystemsPage from './pages/Systems'
import IncidentsPage from './pages/Incidents'
import ShipmentsPage from './pages/Shipments'
import ContinuityPage from './pages/Continuity'
import RecoveryPage from './pages/Recovery'

export default function DashboardApp({ user, onLogout }: { user: AuthUser; onLogout: () => void }) {
  const [page, setPage] = useState<Page>(() => user.role === 'security-admin' ? 'Systems' : user.role === 'hub-operator' ? 'Shipments' : 'Overview')
  const allowedPages = pagesByRole[user.role]
  const [systems, setSystems] = useState<System[]>(restoreSystems)
  const [shipments, setShipments] = useState<Shipment[]>(initialShipments)
  const [search, setSearch] = useState('')
  const [shipmentFilter, setShipmentFilter] = useState('All shipments')
  const [modal, setModal] = useState<Modal>(null)
  const [isolated, setIsolated] = useState(() => window.sessionStorage.getItem(workflowStorageKeys.isolated) === 'true')
  const [approval, setApproval] = useState<'pending' | 'approved' | 'declined'>(restoreApproval)
  const [continuityActive, setContinuityActive] = useState(() => window.sessionStorage.getItem(workflowStorageKeys.continuityActive) === 'true')
  const [permanentSystemsValidated, setPermanentSystemsValidated] = useState(() => window.sessionStorage.getItem(workflowStorageKeys.permanentSystemsValidated) === 'true')
  const [reconciled, setReconciled] = useState(() => window.sessionStorage.getItem(workflowStorageKeys.reconciled) === 'true')
  const [toast, setToast] = useState('')
  const [showAlerts, setShowAlerts] = useState(false)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const visibleShipments = useMemo(
    () => user.role === 'hub-operator' ? shipments.filter((shipment) => operatorShipmentIds.includes(shipment.id)) : shipments,
    [shipments, user.role],
  )

  const filteredShipments = useMemo(() => visibleShipments.filter((shipment) => {
    const matchesSearch = `${shipment.id} ${shipment.destination} ${shipment.customer}`.toLowerCase().includes(search.toLowerCase())
    const matchesFilter = shipmentFilter === 'All shipments' || shipment.status === shipmentFilter
    return matchesSearch && matchesFilter
  }), [search, shipmentFilter, visibleShipments])

  const notify = (message: string) => {
    setToast(message)
    window.setTimeout(() => setToast(''), 3200)
  }

  const openAction = (action: Modal) => {
    if (!roleCanPerformAction(user.role, action)) {
      notify('Your role does not have permission to perform this action.')
      return
    }
    setModal(action)
  }

  const recordShipmentUpdate = (shipmentId: string) => {
    if (user.role !== 'hub-operator' || !operatorShipmentIds.includes(shipmentId)) {
      notify('You can only update shipments assigned to your hub.')
      return
    }
    if (!continuityActive) {
      notify('Shipment updates are locked until the Operations Manager approves continuity.')
      return
    }
    setShipments((current) => current.map((shipment) =>
      shipment.id === shipmentId ? { ...shipment, lastUpdate: 'Hub activity recorded just now' } : shipment,
    ))
    notify(`Operational update recorded for ${shipmentId}.`)
  }

  const confirmAction = () => {
    if (!modal) {
      setModal(null)
      return
    }
    if (!roleCanPerformAction(user.role, modal)) {
      setModal(null)
      notify('Your role does not have permission to perform this action.')
      return
    }
    if (modal === 'isolate') {
      setIsolated(true)
      window.sessionStorage.setItem(workflowStorageKeys.isolated, 'true')
      setSystems((current) => current.map((system) => system.name === 'EDI Gateway' ? { ...system, status: 'Isolated' } : system))
      notify('EDI Gateway isolated. Dependent connections are now contained.')
    }
    if (modal === 'approve') {
      setApproval('approved')
      setContinuityActive(true)
      window.sessionStorage.setItem(workflowStorageKeys.approval, 'approved')
      window.sessionStorage.setItem(workflowStorageKeys.continuityActive, 'true')
      notify('Continuity plan approved. Secure workspace is active.')
    }
    if (modal === 'decline') {
      setApproval('declined')
      window.sessionStorage.setItem(workflowStorageKeys.approval, 'declined')
      notify('Continuity plan declined. The response team has been notified.')
    }
    if (modal === 'restore') {
      setSystems((current) => current.map((system) => system.name === 'EDI Gateway' ? { ...system, status: 'Operational' } : system))
      setIsolated(false)
      setPermanentSystemsValidated(true)
      window.sessionStorage.setItem(workflowStorageKeys.isolated, 'false')
      window.sessionStorage.setItem(workflowStorageKeys.permanentSystemsValidated, 'true')
      notify('EDI Gateway passed validation and is back online.')
    }
    if (modal === 'reconcile') {
      setReconciled(true)
      window.sessionStorage.setItem(workflowStorageKeys.reconciled, 'true')
      notify('Reconciliation complete. No duplicate transactions were found.')
    }
    setModal(null)
  }

  const changePage = (nextPage: Page) => {
    if (!allowedPages.includes(nextPage)) {
      notify('This page is not available for your role.')
      return
    }
    setPage(nextPage)
    setMobileMenuOpen(false)
  }

  const headingText: Record<Page, string> = {
    Overview: `Good morning, ${user.name.split(' ')[0]}`,
    Systems: 'Connected systems',
    Incidents: 'Incident response',
    Shipments: 'Shipment operations',
    Continuity: 'Continuity workspace',
    Recovery: 'Recovery & reconciliation',
  }

  const pageSubtitle: Record<Page, string> = {
    Overview: 'Here’s your network resilience snapshot for today.',
    Systems: 'Monitor health, dependencies, and integration status across your logistics network.',
    Incidents: 'Investigate threats, assess operational impact, and coordinate a controlled response.',
    Shipments: 'Track impacted loads and keep eligible shipments moving.',
    Continuity: 'Operate securely while permanent systems are unavailable.',
    Recovery: 'Validate trusted infrastructure and reconcile continuity records safely.',
  }
  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileMenuOpen ? 'sidebar-open' : ''}`}>
        <button className="brand" type="button" onClick={() => changePage('Overview')} aria-label="ResilienceX home">
          <span className="brand-mark">R<span>X</span></span>
          <span className="brand-name">resilience<span>x</span><small>LOGISTICS RESILIENCE</small></span>
        </button>
        <div className="workspace-switcher">
          <div className="workspace-avatar">NW</div>
          <div><strong>Northwind Logistics</strong><span>Enterprise workspace</span></div>
          <span className="switcher-chevron">⌄</span>
        </div>
        <nav className="main-nav" aria-label="Main navigation">
          {['WORKSPACE', 'RESILIENCE'].map((group) => (
            <div className="nav-group" key={group}>
              <p className="nav-label">{group}</p>
              {navItems.filter((item) => item.group === group && allowedPages.includes(item.label)).map((item) => (
                <button key={item.label} className={`nav-link ${page === item.label ? 'active' : ''}`} onClick={() => changePage(item.label)}>
                  <Icon name={item.icon} size={17} />
                  <span>{item.label}</span>
                  {item.label === 'Incidents' && !reconciled && <span className="nav-count">1</span>}
                </button>
              ))}
            </div>
          ))}
        </nav>
        <div className="sidebar-bottom">
          <div className="sidebar-help"><span className="help-mark">?</span><div><strong>Need a hand?</strong><small>Visit the resilience guide</small></div><Icon name="chevron" size={15} /></div>
          <div className="profile-card">
            <div className="profile-avatar">{user.name.split(' ').map((part) => part[0]).join('')}</div>
            <div><strong>{user.name}</strong><small>{roleLabels[user.role]}</small></div>
            <button type="button" aria-label="Sign out" className="profile-more" onClick={onLogout}>↪</button>
          </div>
        </div>
      </aside>

      <div className="main-column">
        <header className="topbar">
          <button className="mobile-menu-button icon-button" type="button" aria-label="Toggle navigation" onClick={() => setMobileMenuOpen((open) => !open)}><Icon name="menu" /></button>
          <div className="breadcrumb"><span>Northwind Logistics</span><Icon name="chevron" size={14} /><strong>{page}</strong></div>
          <div className="topbar-actions">
            <div className="network-live"><span className="live-dot" />All systems monitored</div>
            <button className={`icon-button notification-button ${showAlerts ? 'selected' : ''}`} type="button" aria-label="Notifications" onClick={() => setShowAlerts((open) => !open)}><Icon name="bell" size={18} /><i /></button>
            <div className="topbar-avatar" title={`${user.name} · ${roleLabels[user.role]}`}>{user.name.split(' ').map((part) => part[0]).join('')}</div>
          </div>
          {showAlerts && <div className="notification-popover"><strong>Notifications <span>2 new</span></strong><div><span className="alert-mini"><Icon name="warning" size={14} /></span><p><b>Suspicious EDI activity</b><small>Detected 12 minutes ago</small></p></div><div><span className="alert-mini success-mini"><Icon name="check" size={14} /></span><p><b>Backup sync complete</b><small>All 5 systems checked</small></p></div></div>}
        </header>

        <main className="page-content">
          <div className="page-heading">
            <div><div className="eyebrow"><span className="eyebrow-line" />{currentDateLabel} · {roleLabels[user.role].toUpperCase()}</div><h1>{headingText[page]}{page === 'Overview' && <span className="wave">✳</span>}</h1><p>{pageSubtitle[page]}</p></div>
            <button className="button button-outline report-button" type="button" onClick={() => notify('Resilience report is up to date. Last generated just now.')}>
              <Icon name="activity" size={16} /> View resilience report
            </button>
          </div>

          {page === 'Overview' && <OverviewPage role={user.role} systems={systems} isolated={isolated} permanentSystemsValidated={permanentSystemsValidated} reconciled={reconciled} approval={approval} continuityActive={continuityActive} shipments={visibleShipments} onPage={changePage} onAction={openAction} />}
          {page === 'Systems' && <SystemsPage canRestore={user.role === 'security-admin'} systems={systems} onRestore={() => openAction('restore')} />}
          {page === 'Incidents' && <IncidentsPage role={user.role} isolated={isolated} permanentSystemsValidated={permanentSystemsValidated} reconciled={reconciled} approval={approval} continuityActive={continuityActive} onPage={changePage} onAction={openAction} />}
          {page === 'Shipments' && <ShipmentsPage canUpdate={user.role === 'hub-operator'} onRecordUpdate={recordShipmentUpdate} filteredShipments={filteredShipments} search={search} setSearch={setSearch} shipmentFilter={shipmentFilter} setShipmentFilter={setShipmentFilter} continuityActive={continuityActive} />}
          {page === 'Continuity' && <ContinuityPage canApprove={user.role === 'operations-manager'} approval={approval} continuityActive={continuityActive} shipments={visibleShipments} onAction={openAction} onPage={changePage} />}
          {page === 'Recovery' && <RecoveryPage canManageRecovery={user.role === 'security-admin'} isolated={isolated} continuityActive={continuityActive} permanentSystemsValidated={permanentSystemsValidated} reconciled={reconciled} onAction={openAction} onPage={changePage} />}
        </main>
        <footer className="app-footer"><span>© {currentYear} ResilienceX</span><span className="footer-status"><span className="live-dot" /> Platform status: operational</span><button type="button" onClick={() => notify('All services are operating normally.')}>Help & support</button></footer>
      </div>

      {toast && <div className="toast" role="status"><span className="toast-check"><Icon name="check" size={15} /></span>{toast}<button type="button" aria-label="Dismiss notification" onClick={() => setToast('')}><Icon name="close" size={15} /></button></div>}
      {modal && <ActionModal modal={modal} onConfirm={confirmAction} onClose={() => setModal(null)} />}
    </div>
  )
}
