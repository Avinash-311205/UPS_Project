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
import { LoadingSpinner, ErrorBanner } from '../components/CommonStates'

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

  const criticalOpsCount = operations.filter((o) => o.criticality === 4).length

  return (
    <div>
      {/* Editorial Page Header */}
      <div className="ed-page-header">
        <div>
          <div className="ed-section-tag">
            <span>MODULE 01</span>
            <span style={{ opacity: 0.3 }}>/</span>
            <span>OPERATIONAL INTELLIGENCE MATRIX</span>
          </div>
          <h1 className="ed-page-title">Operational Snapshot</h1>
          <p className="ed-page-subtitle">
            Autonomous supply chain graph intelligence, multi-tier cascade evaluation, and systemic disruption telemetry.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
          <button
            type="button"
            className="ed-button"
            onClick={() => {
              setLoading(true)
              loadData()
            }}
            title="Re-query live telemetry"
          >
            [ REFRESH TELEMETRY ]
          </button>
          <button
            type="button"
            className="ed-button ed-button-primary"
            onClick={() => onNavigate('Incidents')}
          >
            [ + REPORT DISRUPTION ]
          </button>
        </div>
      </div>

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {loading ? (
        <LoadingSpinner label="SYNCHRONIZING TELEMETRY STREAM..." />
      ) : (
        <>
          {/* SECTION 01 // ASYMMETRIC OPERATIONAL POSTURE (65 / 35 SPLIT) */}
          <div className="ed-asym-65-35" style={{ marginBottom: '48px' }}>
            {/* Primary Left Column: Hero Statistic & Architectural Posture */}
            <div>
              <div className="ed-section-tag">
                <span>SECTION 01</span>
                <span style={{ opacity: 0.3 }}>/</span>
                <span>SYSTEMIC POSTURE &amp; ACTIVE TOPOLOGY</span>
              </div>

              {/* Massive Hero Figure */}
              <div style={{ margin: '20px 0 28px 0' }}>
                <div className="ed-stat-massive">
                  {activeSystemsCount}
                  <span style={{ fontSize: '28px', color: 'var(--ed-teal-text)', marginLeft: '12px' }}>
                    NODES
                  </span>
                </div>
                <div className="ed-stat-unit">
                  ACTIVE LOGISTICS NODES OPERATIONAL IN RESILIENCE TOPOLOGY
                </div>
                <div
                  style={{
                    fontFamily: 'var(--ed-font-mono)',
                    fontSize: '11px',
                    color: 'var(--ed-text-secondary)',
                    marginTop: '6px',
                  }}
                >
                  CATALOGUED ASSETS: <strong>{systems.length}</strong> TOTAL /{' '}
                  <span style={{ color: 'var(--ed-amber-text)' }}>
                    {systems.length - activeSystemsCount} STANDBY OR INACTIVE
                  </span>
                </div>
              </div>

              <div className="ed-rule-subtle" />

              {/* Secondary Asymmetric Stat Split (Dual Columns) */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '28px', marginTop: '24px' }}>
                <div>
                  <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '10px', color: 'var(--ed-text-muted)', letterSpacing: '0.1em' }}>
                    CRITICAL DISRUPTIONS
                  </div>
                  <div
                    style={{
                      fontFamily: 'var(--ed-font-display)',
                      fontSize: '38px',
                      fontWeight: 800,
                      color: criticalIncidentsCount > 0 ? 'var(--ed-crimson)' : 'var(--ed-teal-text)',
                      lineHeight: 1.1,
                      margin: '6px 0',
                    }}
                  >
                    {criticalIncidentsCount}
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--ed-text-secondary)' }}>
                    {criticalIncidentsCount > 0
                      ? 'Immediate cascade mitigation active'
                      : 'Zero critical outages across tenant network'}
                  </div>
                </div>

                <div>
                  <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '10px', color: 'var(--ed-text-muted)', letterSpacing: '0.1em' }}>
                    WORKFLOW RESILIENCE
                  </div>
                  <div
                    style={{
                      fontFamily: 'var(--ed-font-display)',
                      fontSize: '38px',
                      fontWeight: 800,
                      color: 'var(--ed-text-primary)',
                      lineHeight: 1.1,
                      margin: '6px 0',
                    }}
                  >
                    {criticalOpsCount}
                    <span style={{ fontSize: '18px', color: 'var(--ed-text-muted)', fontWeight: 400, marginLeft: '6px' }}>
                      / {operations.length}
                    </span>
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--ed-text-secondary)' }}>
                    Tier-1 critical workflows evaluated under strict availability rules
                  </div>
                </div>
              </div>
            </div>

            {/* Secondary Right Column: Telemetry Context & Direct Actions */}
            <div className="ed-context-panel">
              <div className="ed-context-panel-title">
                <span>02 // NETWORK STREAM</span>
                <span className="ed-pulse-dot" />
              </div>

              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-secondary)', lineHeight: 1.8 }}>
                <div>STATUS: <strong style={{ color: 'var(--ed-teal-text)' }}>SYNCHRONIZED</strong></div>
                <div>CLUSTER: <strong style={{ color: 'var(--ed-text-primary)' }}>US-EAST-01</strong></div>
                <div>DISRUPTION INDEX: <strong style={{ color: activeIncidents.length > 0 ? 'var(--ed-amber)' : 'var(--ed-teal-text)' }}>{activeIncidents.length > 0 ? 'ELEVATED' : 'NOMINAL'}</strong></div>
                <div>GRAPH TOPOLOGY: <strong style={{ color: 'var(--ed-text-primary)' }}>ACYCLIC VALIDATED</strong></div>
              </div>

              <div className="ed-rule-subtle" style={{ margin: '12px 0' }} />

              <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)', lineHeight: 1.5 }}>
                ResilienceX Graph Engine evaluates multi-hop dependency impacts in real time. Outages cascade across TMS, WMS, and EDI gateways deterministically.
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '12px' }}>
                <button
                  type="button"
                  className="ed-button ed-button-secondary"
                  onClick={() => onNavigate('Systems')}
                  style={{ justifyContent: 'space-between', width: '100%' }}
                >
                  <span>[ 02 SYSTEMS REGISTRY ]</span>
                  <span>→</span>
                </button>
                <button
                  type="button"
                  className="ed-button ed-button-secondary"
                  onClick={() => onNavigate('Dependencies')}
                  style={{ justifyContent: 'space-between', width: '100%' }}
                >
                  <span>[ 03 DEPENDENCY GRAPH ]</span>
                  <span>→</span>
                </button>
                <button
                  type="button"
                  className="ed-button ed-button-secondary"
                  onClick={() => onNavigate('ImpactAnalysis')}
                  style={{ justifyContent: 'space-between', width: '100%' }}
                >
                  <span>[ 06 BLAST RADIUS SIMULATOR ]</span>
                  <span>→</span>
                </button>
              </div>
            </div>
          </div>

          <div className="ed-rule" />

          {/* SECTION 03 // ACTIVE DISRUPTIONS TIMELINE FEED */}
          <div style={{ marginBottom: '48px' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '18px' }}>
              <div className="ed-section-tag" style={{ margin: 0 }}>
                <span>SECTION 03</span>
                <span style={{ opacity: 0.3 }}>/</span>
                <span>ACTIVE DISRUPTION INCIDENTS ({activeIncidents.length})</span>
              </div>
              <button
                type="button"
                className="ed-button ed-button-sm"
                onClick={() => onNavigate('Incidents')}
              >
                [ VIEW ALL INCIDENTS → ]
              </button>
            </div>

            {activeIncidents.length === 0 ? (
              <div className="cc-empty-state">
                <div className="cc-empty-title">00 // ALL LOGISTICS NODES NOMINAL</div>
                <p className="cc-empty-desc">
                  Zero active disruptions detected across tenant infrastructure. All operational dependencies are functioning within established resilience thresholds.
                </p>
                <button
                  type="button"
                  className="ed-button ed-button-primary"
                  onClick={() => onNavigate('Incidents')}
                >
                  [ + REPORT AN INCIDENT ]
                </button>
              </div>
            ) : (
              <div className="ed-table-container">
                <table className="ed-table">
                  <thead>
                    <tr>
                      <th style={{ width: '40px' }}>#</th>
                      <th>INCIDENT ID</th>
                      <th>TITLE</th>
                      <th>FAILED COMPONENT</th>
                      <th>SEVERITY</th>
                      <th>STATUS</th>
                      <th>DETECTED AT</th>
                      <th style={{ textAlign: 'right' }}>OPERATIONAL ACTION</th>
                    </tr>
                  </thead>
                  <tbody>
                    {activeIncidents.map((inc, idx) => (
                      <tr key={inc.id}>
                        <td className="ed-row-idx">{String(idx + 1).padStart(3, '0')}</td>
                        <td style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                          #{inc.id.substring(0, 8)}
                        </td>
                        <td style={{ fontWeight: 700, color: 'var(--ed-text-primary)' }}>
                          {inc.title}
                        </td>
                        <td style={{ fontFamily: 'var(--ed-font-mono)' }}>
                          {inc.failedSystemName}
                        </td>
                        <td>
                          <StatusBadge type="incidentSeverity" value={inc.severity} />
                        </td>
                        <td>
                          <StatusBadge type="incidentStatus" value={inc.status} />
                        </td>
                        <td style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-secondary)' }}>
                          {new Date(inc.detectedAt).toLocaleString()}
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            type="button"
                            className="ed-button ed-button-sm"
                            onClick={() => onNavigate('Incidents')}
                          >
                            [ MITIGATE → ]
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="ed-rule" />

          {/* SECTION 04 // ASYMMETRIC OPERATIONAL OVERVIEW (60 / 40 SPLIT) */}
          <div className="ed-asym-60-40">
            {/* Left Column: Registered Logistics Nodes Sample */}
            <div>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px' }}>
                <div className="ed-section-tag" style={{ margin: 0 }}>
                  <span>SECTION 04</span>
                  <span style={{ opacity: 0.3 }}>/</span>
                  <span>REGISTERED LOGISTICS NODES</span>
                </div>
                <button
                  type="button"
                  className="ed-button ed-button-sm"
                  onClick={() => onNavigate('Systems')}
                >
                  [ VIEW REGISTRY → ]
                </button>
              </div>

              {systems.length === 0 ? (
                <div style={{ padding: '24px', border: '1px dashed var(--ed-border)', color: 'var(--ed-text-muted)' }}>
                  No systems registered in this tenant workspace yet.
                </div>
              ) : (
                <div className="ed-table-container">
                  <table className="ed-table">
                    <thead>
                      <tr>
                        <th style={{ width: '40px' }}>#</th>
                        <th>SYSTEM NAME</th>
                        <th>CRITICALITY</th>
                        <th>STATUS</th>
                        <th style={{ textAlign: 'right' }}>ACTION</th>
                      </tr>
                    </thead>
                    <tbody>
                      {systems.slice(0, 5).map((sys, idx) => (
                        <tr key={sys.id}>
                          <td className="ed-row-idx">{String(idx + 1).padStart(3, '0')}</td>
                          <td>
                            <div style={{ fontWeight: 700 }}>{sys.name}</div>
                            {sys.businessPurpose && (
                              <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                                {sys.businessPurpose}
                              </div>
                            )}
                          </td>
                          <td>
                            <StatusBadge type="systemCriticality" value={sys.criticality} />
                          </td>
                          <td>
                            <StatusBadge type="systemStatus" value={sys.lifecycleStatus} />
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <button
                              type="button"
                              className="ed-button ed-button-sm"
                              onClick={() => onNavigate('ImpactAnalysis', sys.id)}
                            >
                              [ BLAST RADIUS → ]
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* Right Column: High Criticality Operations */}
            <div>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '14px' }}>
                <div className="ed-section-tag" style={{ margin: 0 }}>
                  <span>SECTION 05</span>
                  <span style={{ opacity: 0.3 }}>/</span>
                  <span>CONTINUITY WORKFLOWS</span>
                </div>
                <button
                  type="button"
                  className="ed-button ed-button-sm"
                  onClick={() => onNavigate('Operations')}
                >
                  [ VIEW OPERATIONS → ]
                </button>
              </div>

              {operations.length === 0 ? (
                <div style={{ padding: '24px', border: '1px dashed var(--ed-border)', color: 'var(--ed-text-muted)' }}>
                  No business operations configured for resilience scoring yet.
                </div>
              ) : (
                <div className="ed-table-container">
                  <table className="ed-table">
                    <thead>
                      <tr>
                        <th>OPERATION</th>
                        <th>CRITICALITY</th>
                        <th>AVAILABILITY</th>
                      </tr>
                    </thead>
                    <tbody>
                      {operations.slice(0, 5).map((op) => (
                        <tr key={op.id}>
                          <td>
                            <div style={{ fontWeight: 700 }}>{op.name}</div>
                            <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>
                              {op.supportingSystemsCount} SUPPORTING SYSTEMS
                            </div>
                          </td>
                          <td>
                            <StatusBadge type="operationCriticality" value={op.criticality} />
                          </td>
                          <td>
                            <StatusBadge type="availabilityRule" value={op.availabilityRule} />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
