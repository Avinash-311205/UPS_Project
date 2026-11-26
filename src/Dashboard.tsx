import { useState } from 'react'
import type { AuthUser } from './auth/session'
import type { Page } from './types'
import AppLayout from './components/AppLayout'
import OverviewPage from './pages/Overview'
import SystemsPage from './pages/Systems'
import DependenciesPage from './pages/Dependencies'
import BusinessOperationsPage from './pages/BusinessOperations'
import IncidentsPage from './pages/Incidents'
import ImpactAnalysisPage from './pages/ImpactAnalysis'
import IamManagementPage from './pages/IamManagement'
import './styles/commandCentre.css'

interface DashboardAppProps {
  user: AuthUser
  onLogout: () => void
}

export default function DashboardApp({ user, onLogout }: DashboardAppProps) {
  const [currentPage, setCurrentPage] = useState<Page>('Overview')
  const [navigationParam, setNavigationParam] = useState<string | null>(null)

  function handleNavigate(page: Page, param?: string) {
    setCurrentPage(page)
    setNavigationParam(param || null)
    // Scroll workspace to top on navigation
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function handlePageChange(page: Page) {
    setCurrentPage(page)
    setNavigationParam(null)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <AppLayout
      user={user}
      currentPage={currentPage}
      onPageChange={handlePageChange}
      onLogout={onLogout}
    >
      {currentPage === 'Overview' && <OverviewPage onNavigate={handleNavigate} />}
      {currentPage === 'Systems' && <SystemsPage user={user} onNavigate={handleNavigate} />}
      {currentPage === 'Dependencies' && <DependenciesPage user={user} onNavigate={handleNavigate} />}
      {currentPage === 'Operations' && <BusinessOperationsPage user={user} onNavigate={handleNavigate} />}
      {currentPage === 'Incidents' && <IncidentsPage user={user} onNavigate={handleNavigate} />}
      {currentPage === 'ImpactAnalysis' && (
        <ImpactAnalysisPage
          initialSystemId={navigationParam}
          onNavigate={handleNavigate}
        />
      )}
      {currentPage === 'IamManagement' && (
        <IamManagementPage user={user} onNavigate={handleNavigate} onLogout={onLogout} />
      )}
    </AppLayout>
  )
}
