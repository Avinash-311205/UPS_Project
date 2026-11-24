import { useEffect, useState, useCallback } from 'react'
import { api } from '../services/api'
import type {
  Page,
  LogisticsSystemResponseDTO,
  IncidentResponseDTO,
  BusinessOperationResponseDTO,
} from '../types'
import { IncidentSeverity, IncidentStatus, SystemLifecycleStatus } from '../types'
import StatusBadge from '../components/StatusBadge'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface OverviewProps {
  onNavigate: (page: Page, param?: string) => void
}

export default function OverviewPage({ onNavigate }: OverviewProps) {
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [incidents, setIncidents] = useState<IncidentResponseDTO[]>([])
  const [operations, setOperations] = useState<BusinessOperationResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadData = useCallback(async () => {
    try {
      const [sysData, incData, opsData] = await Promise.all([
        api.getSystems(true),
        api.getIncidents(),
        api.getOperations(true),
      ])
      setSystems(sysData)
      setIncidents(incData)
      setOperations(opsData)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Telemetry synchronization failed.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const activeSystemsCount = systems.filter((s) => s.lifecycleStatus === SystemLifecycleStatus.Active).length
  const activeIncidents = incidents.filter(
    (i) => i.status !== IncidentStatus.Resolved && i.status !== IncidentStatus.Closed
  )
  const criticalIncidentsCount = incidents.filter(
    (i) =>
      i.severity === IncidentSeverity.Critical &&
      i.status !== IncidentStatus.Resolved &&
      i.status !== IncidentStatus.Closed
  ).length

  // High & Critical operations
  const criticalOpsCount = operations.filter((o) => o.criticality === 4).length

  return (
    <div>
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>01 // OPERATIONAL SNAPSHOT</h1>
          <p>Real-time cyber resilience posture, system health telemetry and disruption telemetry.</p>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            type="button"
            className="cc-button"
            onClick={() => {
              setLoading(true)
              loadData()
            }}
            title="Refresh operational telemetry"
          >
            REFRESH TELEMETRY
          </button>
          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={() => onNavigate('Incidents')}
          >
            + REPORT DISRUPTION
          </button>
        </div>
      </div>

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {loading ? (
        <LoadingSpinner label="SYNCHRONIZING TELEMETRY STREAM..." />
      ) : (
        <>
          {/* Key Metrics Grid */}
          <div className="cc-grid-stats">
            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>REGISTERED SYSTEMS</span>
                <span>[01]</span>
              </div>
              <div className="cc-stat-value">{systems.length}</div>
              <div className="cc-stat-sub">
                <span style={{ color: 'var(--cc-green)' }}>{activeSystemsCount} ACTIVE</span>
                <span style={{ color: '#555555', margin: '0 4px' }}>|</span>
                <span>{systems.length - activeSystemsCount} INACTIVE</span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>ACTIVE INCIDENTS</span>
                <span>[02]</span>
              </div>
              <div className="cc-stat-value" style={{ color: activeIncidents.length > 0 ? 'var(--cc-amber)' : 'var(--cc-text-primary)' }}>
                {activeIncidents.length}
              </div>
              <div className="cc-stat-sub">
                <span>{incidents.length - activeIncidents.length} RESOLVED / CLOSED</span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>CRITICAL INCIDENTS</span>
                <span>[03]</span>
              </div>
              <div className="cc-stat-value" style={{ color: criticalIncidentsCount > 0 ? 'var(--cc-red)' : 'var(--cc-green)' }}>
                {criticalIncidentsCount}
              </div>
              <div className="cc-stat-sub">
                <span>IMMEDIATE TRIAGE REQUIRED</span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>BUSINESS OPERATIONS</span>
                <span>[04]</span>
              </div>
              <div className="cc-stat-value">{operations.length}</div>
              <div className="cc-stat-sub">
                <span style={{ color: 'var(--cc-amber)' }}>{criticalOpsCount} CRITICAL CLASS</span>
              </div>
            </div>
          </div>

          {/* Active Disruption Incidents Section */}
          <div style={{ marginBottom: '32px' }}>
            <div className="cc-toolbar">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', fontWeight: 800 }}>
                  // ACTIVE DISRUPTION INCIDENTS
                </span>
                <span className="cc-badge cc-badge-neutral">{activeIncidents.length}</span>
              </div>
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => onNavigate('Incidents')}
              >
                VIEW ALL INCIDENTS →
              </button>
            </div>

            {activeIncidents.length === 0 ? (
              <EmptyState
                title="ALL SYSTEMS NOMINAL"
                description="No active disruption incidents reported for this workspace. Network telemetry indicates standard operating parameters."
              />
            ) : (
              <div className="cc-table-container">
                <table className="cc-table">
                  <thead>
                    <tr>
                      <th>INCIDENT ID</th>
                      <th>TITLE</th>
                      <th>FAILED SYSTEM</th>
                      <th>SEVERITY</th>
                      <th>STATUS</th>
                      <th>DETECTED AT</th>
                      <th style={{ textAlign: 'right' }}>ACTIONS</th>
                    </tr>
                  </thead>
                  <tbody>
                    {activeIncidents.map((inc) => (
                      <tr key={inc.id}>
                        <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                          {inc.id.substring(0, 8)}...
                        </td>
                        <td style={{ fontWeight: 600 }}>{inc.title}</td>
                        <td>
                          <span style={{ fontFamily: 'var(--cc-font-mono)' }}>{inc.failedSystemName}</span>
                        </td>
                        <td>
                          <StatusBadge type="incidentSeverity" value={inc.severity} />
                        </td>
                        <td>
                          <StatusBadge type="incidentStatus" value={inc.status} />
                        </td>
                        <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                          {new Date(inc.detectedAt).toLocaleString()}
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            type="button"
                            className="cc-button cc-button-sm cc-button-danger"
                            onClick={() => onNavigate('ImpactAnalysis', inc.failedSystemId)}
                            title="Analyze operational blast radius"
                          >
                            BLAST RADIUS →
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Systems Overview Section */}
          <div>
            <div className="cc-toolbar">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', fontWeight: 800 }}>
                  // LOGISTICS INFRASTRUCTURE REGISTRY
                </span>
                <span className="cc-badge cc-badge-neutral">{systems.length} SYSTEMS</span>
              </div>
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => onNavigate('Systems')}
              >
                MANAGE SYSTEMS REGISTRY →
              </button>
            </div>

            {systems.length === 0 ? (
              <EmptyState
                title="NO SYSTEMS REGISTERED"
                description="No logistics systems registered in this tenant database yet."
                action={
                  <button
                    type="button"
                    className="cc-button cc-button-primary"
                    onClick={() => onNavigate('Systems')}
                  >
                    + REGISTER FIRST SYSTEM
                  </button>
                }
              />
            ) : (
              <div className="cc-table-container">
                <table className="cc-table">
                  <thead>
                    <tr>
                      <th>SYSTEM NAME</th>
                      <th>TYPE</th>
                      <th>CRITICALITY</th>
                      <th>ENVIRONMENT</th>
                      <th>STATUS</th>
                      <th style={{ textAlign: 'right' }}>TELEMETRY ACTION</th>
                    </tr>
                  </thead>
                  <tbody>
                    {systems.slice(0, 8).map((sys) => (
                      <tr key={sys.id}>
                        <td>
                          <div style={{ fontWeight: 700 }}>{sys.name}</div>
                          {sys.businessPurpose && (
                            <div style={{ fontSize: '11px', color: '#666666' }}>{sys.businessPurpose}</div>
                          )}
                        </td>
                        <td>
                          <span className="cc-badge cc-badge-neutral">
                            {sys.systemType === 1
                              ? 'TMS'
                              : sys.systemType === 2
                              ? 'WMS'
                              : sys.systemType === 3
                              ? 'YMS'
                              : sys.systemType === 4
                              ? 'Telematics'
                              : sys.systemType === 5
                              ? 'EDI'
                              : sys.systemType === 6
                              ? 'ERP'
                              : 'Other'}
                          </span>
                        </td>
                        <td>
                          <StatusBadge type="systemCriticality" value={sys.criticality} />
                        </td>
                        <td>
                          <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#AAAAAA' }}>
                            {sys.environment === 3 ? 'PROD' : sys.environment === 2 ? 'STAGING' : 'DEV'}
                          </span>
                        </td>
                        <td>
                          <StatusBadge type="systemStatus" value={sys.lifecycleStatus} />
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            type="button"
                            className="cc-button cc-button-sm"
                            onClick={() => onNavigate('ImpactAnalysis', sys.id)}
                          >
                            RUN IMPACT ANALYSIS →
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
