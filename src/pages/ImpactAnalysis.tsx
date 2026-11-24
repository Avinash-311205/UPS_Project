import { useEffect, useState, useMemo, useCallback } from 'react'
import { api } from '../services/api'
import type {
  Page,
  ImpactAnalysisResponseDTO,
  LogisticsSystemResponseDTO,
  AffectedOperationDTO,
} from '../types'
import {
  systemTypeLabels,
  systemCriticalityLabels,
  systemEnvironmentLabels,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface ImpactAnalysisPageProps {
  initialSystemId?: string | null
  onNavigate: (page: Page, systemId?: string) => void
}

export default function ImpactAnalysisPage({
  initialSystemId,
  onNavigate: _onNavigate,
}: ImpactAnalysisPageProps) {
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [selectedSystemId, setSelectedSystemId] = useState<string>(initialSystemId || '')
  const [impactData, setImpactData] = useState<ImpactAnalysisResponseDTO | null>(null)
  const [loading, setLoading] = useState(false)
  const [loadingSystems, setLoadingSystems] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Modal to inspect full scoring breakdown of an affected operation
  const [inspectingOp, setInspectingOp] = useState<AffectedOperationDTO | null>(null)

  const triggerAnalysis = useCallback(async (systemIdToAnalyze?: string) => {
    const sysId = systemIdToAnalyze || selectedSystemId
    if (!sysId) return

    setLoading(true)
    setError(null)
    setImpactData(null)
    try {
      const result = await api.getSystemImpactAnalysis(sysId)
      setImpactData(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to compute impact analysis.')
    } finally {
      setLoading(false)
    }
  }, [selectedSystemId])

  const loadSystemsList = useCallback(async () => {
    try {
      const data = await api.getSystems(true)
      setSystems(data)
      if (!selectedSystemId && data.length > 0) {
        setSelectedSystemId(data[0].id)
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve systems list.')
    } finally {
      setLoadingSystems(false)
    }
  }, [selectedSystemId])

  useEffect(() => {
    loadSystemsList()
  }, [loadSystemsList])

  useEffect(() => {
    if (!initialSystemId) return
    let active = true
    async function runInitial(sysId: string) {
      try {
        const result = await api.getSystemImpactAnalysis(sysId)
        if (active) setImpactData(result)
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Failed to compute impact analysis.')
      } finally {
        if (active) setLoading(false)
      }
    }
    runInitial(initialSystemId)
    return () => {
      active = false
    }
  }, [initialSystemId])

  const selectedSystem = useMemo(
    () => systems.find((s) => s.id === selectedSystemId),
    [systems, selectedSystemId]
  )

  return (
    <div>
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>06 // OPERATIONAL BLAST RADIUS & IMPACT ANALYSIS</h1>
          <p>
            Autonomous Graph Engine traversal calculating downstream system blast radius, operation availability status, and 3-factor composite severity scoring.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            type="button"
            className="cc-button cc-button-primary"
            disabled={!selectedSystemId || loading}
            onClick={() => triggerAnalysis()}
          >
            {loading ? 'TRAVERSING GRAPH...' : 'EXECUTE IMPACT ANALYSIS'}
          </button>
        </div>
      </div>

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* System Selector Header */}
      <div
        style={{
          backgroundColor: 'var(--cc-bg-surface)',
          border: '1px solid var(--cc-border)',
          padding: '16px',
          marginBottom: '24px',
          display: 'flex',
          alignItems: 'center',
          gap: '16px',
          flexWrap: 'wrap',
        }}
      >
        <div style={{ flex: 1, minWidth: '280px' }}>
          <label className="cc-form-label" style={{ display: 'block', marginBottom: '6px' }}>
            SELECT TARGET LOGISTICS SYSTEM TO SIMULATE OUTAGE:
          </label>
          {loadingSystems ? (
            <div style={{ fontSize: '11px', color: '#888888' }}>LOADING SYSTEMS INVENTORY...</div>
          ) : (
            <select
              className="cc-select"
              value={selectedSystemId}
              onChange={(e) => {
                setSelectedSystemId(e.target.value)
                triggerAnalysis(e.target.value)
              }}
            >
              <option value="">-- SELECT TARGET COMPONENT --</option>
              {systems.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} [{systemTypeLabels[s.systemType]?.split(' ')[0]}] ({systemCriticalityLabels[s.criticality]} Crit)
                </option>
              ))}
            </select>
          )}
        </div>

        {selectedSystem && (
          <div
            style={{
              display: 'flex',
              gap: '16px',
              borderLeft: '1px solid var(--cc-border)',
              paddingLeft: '16px',
            }}
          >
            <div>
              <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                CLASSIFICATION
              </div>
              <div style={{ fontWeight: 600, fontSize: '12px' }}>
                {systemTypeLabels[selectedSystem.systemType]}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                ENVIRONMENT
              </div>
              <div style={{ fontWeight: 600, fontSize: '12px' }}>
                {systemEnvironmentLabels[selectedSystem.environment]}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                STATUS
              </div>
              <div>
                <StatusBadge type="systemStatus" value={selectedSystem.lifecycleStatus} />
              </div>
            </div>
          </div>
        )}
      </div>

      {loading && <LoadingSpinner label="CALCULATING DOWNSTREAM BLAST RADIUS..." />}

      {!loading && !impactData && !error && (
        <EmptyState
          title="READY TO SIMULATE OUTAGE"
          description="Select a logistics system above and execute impact analysis to discover cascading dependent systems and evaluate business operation continuity."
          action={
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={!selectedSystemId}
              onClick={() => triggerAnalysis()}
            >
              RUN ANALYSIS
            </button>
          }
        />
      )}

      {impactData && !loading && (
        <>
          {/* Summary Stat Grid */}
          <div className="cc-grid-stats">
            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>TOTAL AFFECTED SYSTEMS</span>
                <span>[01]</span>
              </div>
              <div
                className="cc-stat-value"
                style={{
                  color:
                    impactData.totalAffectedSystemsCount > 0
                      ? 'var(--cc-amber)'
                      : 'var(--cc-green)',
                }}
              >
                {impactData.totalAffectedSystemsCount}
              </div>
              <div className="cc-stat-sub">
                <span>{impactData.directDependentsCount} DIRECT</span>
                <span style={{ color: '#555555', margin: '0 4px' }}>|</span>
                <span>{impactData.indirectDependentsCount} INDIRECT</span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>AFFECTED OPERATIONS</span>
                <span>[02]</span>
              </div>
              <div
                className="cc-stat-value"
                style={{
                  color:
                    impactData.totalAffectedOperationsCount > 0
                      ? 'var(--cc-red)'
                      : 'var(--cc-text-primary)',
                }}
              >
                {impactData.totalAffectedOperationsCount}
              </div>
              <div className="cc-stat-sub">
                <span style={{ color: 'var(--cc-red)' }}>
                  {impactData.criticalOperationsCount} CRITICAL
                </span>
                <span style={{ color: '#555555', margin: '0 4px' }}>|</span>
                <span style={{ color: 'var(--cc-amber)' }}>
                  {impactData.highOperationsCount} HIGH
                </span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>VERIFICATION REQUIRED</span>
                <span>[03]</span>
              </div>
              <div
                className="cc-stat-value"
                style={{
                  color:
                    impactData.requiresVerificationOperationsCount > 0
                      ? 'var(--cc-amber)'
                      : 'var(--cc-text-primary)',
                }}
              >
                {impactData.requiresVerificationOperationsCount}
              </div>
              <div className="cc-stat-sub">
                <span>ANY-AVAILABLE REDUNDANCY</span>
              </div>
            </div>

            <div className="cc-stat-tile">
              <div className="cc-stat-header">
                <span>SIMULATED COMPONENT</span>
                <span>[TARGET]</span>
              </div>
              <div className="cc-stat-value" style={{ fontSize: '18px', lineHeight: 1.2 }}>
                {impactData.failedSystem.name}
              </div>
              <div className="cc-stat-sub">
                <StatusBadge
                  type="systemCriticality"
                  value={impactData.failedSystem.criticality}
                />
              </div>
            </div>
          </div>

          {/* Operational Blast Radius Technical Callout */}
          <div
            style={{
              backgroundColor: '#12171E',
              border: '1px solid #1E2E3E',
              padding: '12px 16px',
              marginBottom: '24px',
              fontFamily: 'var(--cc-font-mono)',
              fontSize: '11px',
              color: '#8CB8D8',
              lineHeight: 1.5,
            }}
          >
            <strong>// DEFENSE DIRECTIVE // SCOPE OF BLAST RADIUS:</strong>
            <br />
            This analysis evaluates static dependency topology and Availability Rule constraints.
            Downstream systems marked as affected represent potential service degradation pathways.
            Strict operations (<span style={{ color: '#FDE68A' }}>AllRequired</span>) will experience operational interruption.
            Redundant operations (<span style={{ color: '#A7F3D0' }}>AnyAvailable</span>) maintain failover capability pending verification.
          </div>

          {/* Affected Business Operations Section */}
          <div style={{ marginBottom: '32px' }}>
            <div className="cc-toolbar">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', fontWeight: 800 }}>
                  // IMPACTED BUSINESS OPERATIONS
                </span>
                <span className="cc-badge cc-badge-neutral">
                  {impactData.affectedOperations.length} OPERATIONS
                </span>
              </div>
            </div>

            {impactData.affectedOperations.length === 0 ? (
              <EmptyState
                title="NO BUSINESS OPERATIONS IMPACTED"
                description="The simulated failed system does not support any registered business operations, directly or through downstream dependencies."
              />
            ) : (
              <div className="cc-table-container">
                <table className="cc-table">
                  <thead>
                    <tr>
                      <th>OPERATION</th>
                      <th>CRITICALITY</th>
                      <th>RULE</th>
                      <th>AVAILABILITY STATUS</th>
                      <th>SEVERITY</th>
                      <th>SCORE</th>
                      <th>CONTRIBUTING PATH</th>
                      <th style={{ textAlign: 'right' }}>DETAILS</th>
                    </tr>
                  </thead>
                  <tbody>
                    {impactData.affectedOperations.map((op) => (
                      <tr key={op.operationId}>
                        <td>
                          <div style={{ fontWeight: 700 }}>{op.operationName}</div>
                          <div style={{ fontSize: '11px', color: '#777777', marginTop: '2px' }}>
                            {op.affectedSupportingSystemsCount} of {op.totalSupportingSystemsCount} supporting systems impacted
                          </div>
                        </td>
                        <td>
                          <StatusBadge type="operationCriticality" value={op.operationCriticality} />
                        </td>
                        <td>
                          <StatusBadge type="availabilityRule" value={op.availabilityRule} />
                        </td>
                        <td>
                          <StatusBadge type="availabilityStatus" value={op.availabilityStatus} />
                        </td>
                        <td>
                          <StatusBadge type="impactSeverity" value={op.potentialImpactSeverity} />
                        </td>
                        <td>
                          <span style={{ fontFamily: 'var(--cc-font-mono)', fontWeight: 700 }}>
                            {op.numericSeverityScore.toFixed(2)}
                          </span>
                        </td>
                        <td>
                          <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#AAAAAA' }}>
                            {op.contributingDependencyPath.length > 0
                              ? op.contributingDependencyPath.join(' → ')
                              : op.contributingSystemName}
                          </div>
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            type="button"
                            className="cc-button cc-button-sm"
                            onClick={() => setInspectingOp(op)}
                          >
                            INSPECT SCORE [⚙]
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Directly Affected Systems */}
          <div style={{ marginBottom: '32px' }}>
            <div className="cc-toolbar">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', fontWeight: 800 }}>
                  // DIRECTLY RELIANT DOWNSTREAM SYSTEMS (DEPTH = 1)
                </span>
                <span className="cc-badge cc-badge-neutral">
                  {impactData.directDependents.length} SYSTEMS
                </span>
              </div>
            </div>

            {impactData.directDependents.length === 0 ? (
              <div
                style={{
                  padding: '16px',
                  border: '1px dashed var(--cc-border)',
                  color: '#777777',
                  fontSize: '11px',
                  fontFamily: 'var(--cc-font-mono)',
                }}
              >
                No systems directly depend upon this component.
              </div>
            ) : (
              <div className="cc-table-container">
                <table className="cc-table">
                  <thead>
                    <tr>
                      <th>SYSTEM NAME</th>
                      <th>TYPE</th>
                      <th>CRITICALITY</th>
                      <th>DEPENDENCY TYPE</th>
                      <th>DEPTH</th>
                      <th>PATH</th>
                    </tr>
                  </thead>
                  <tbody>
                    {impactData.directDependents.map((dep) => (
                      <tr key={dep.systemId}>
                        <td style={{ fontWeight: 700 }}>{dep.systemName}</td>
                        <td>
                          <span className="cc-badge cc-badge-neutral">
                            {systemTypeLabels[dep.systemType]?.split(' ')[0] || 'System'}
                          </span>
                        </td>
                        <td>
                          <StatusBadge type="systemCriticality" value={dep.criticality} />
                        </td>
                        <td>
                          <StatusBadge type="dependencyType" value={dep.dependencyType} />
                        </td>
                        <td>
                          <span style={{ fontFamily: 'var(--cc-font-mono)' }}>{dep.depth}</span>
                        </td>
                        <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                          {dep.dependencyPath.join(' → ')}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Indirect Downstream Systems */}
          <div>
            <div className="cc-toolbar">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', fontWeight: 800 }}>
                  // INDIRECT CASCADE SYSTEMS (DEPTH &gt; 1)
                </span>
                <span className="cc-badge cc-badge-neutral">
                  {impactData.indirectDependents.length} SYSTEMS
                </span>
              </div>
            </div>

            {impactData.indirectDependents.length === 0 ? (
              <div
                style={{
                  padding: '16px',
                  border: '1px dashed var(--cc-border)',
                  color: '#777777',
                  fontSize: '11px',
                  fontFamily: 'var(--cc-font-mono)',
                }}
              >
                No secondary or tertiary cascading systems identified in the dependency graph.
              </div>
            ) : (
              <div className="cc-table-container">
                <table className="cc-table">
                  <thead>
                    <tr>
                      <th>SYSTEM NAME</th>
                      <th>TYPE</th>
                      <th>CRITICALITY</th>
                      <th>DEPENDENCY TYPE</th>
                      <th>DEPTH</th>
                      <th>PATH (TRAVERSAL CHAIN)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {impactData.indirectDependents.map((dep) => (
                      <tr key={dep.systemId}>
                        <td style={{ fontWeight: 700 }}>{dep.systemName}</td>
                        <td>
                          <span className="cc-badge cc-badge-neutral">
                            {systemTypeLabels[dep.systemType]?.split(' ')[0] || 'System'}
                          </span>
                        </td>
                        <td>
                          <StatusBadge type="systemCriticality" value={dep.criticality} />
                        </td>
                        <td>
                          <StatusBadge type="dependencyType" value={dep.dependencyType} />
                        </td>
                        <td>
                          <span style={{ fontFamily: 'var(--cc-font-mono)' }}>{dep.depth}</span>
                        </td>
                        <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                          {dep.dependencyPath.join(' → ')}
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

      {/* Inspect Scoring Breakdown Modal */}
      <Modal
        isOpen={inspectingOp !== null}
        title={`SCORING BREAKDOWN // ${inspectingOp?.operationName || ''}`}
        onClose={() => setInspectingOp(null)}
        footer={
          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={() => setInspectingOp(null)}
          >
            CLOSE
          </button>
        }
      >
        {inspectingOp && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(3, 1fr)',
                gap: '12px',
                textAlign: 'center',
              }}
            >
              <div style={{ background: '#111111', padding: '12px', border: '1px solid var(--cc-border)' }}>
                <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                  SYSTEM FACTOR (30%)
                </div>
                <div style={{ fontSize: '18px', fontWeight: 800, marginTop: '4px' }}>
                  {inspectingOp.scoringBreakdown.systemCriticalityScore.toFixed(2)}
                </div>
                <div style={{ fontSize: '10px', color: '#888888', marginTop: '2px' }}>
                  WEIGHT: {inspectingOp.scoringBreakdown.systemCriticalityWeight}%
                </div>
              </div>

              <div style={{ background: '#111111', padding: '12px', border: '1px solid var(--cc-border)' }}>
                <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                  DEPENDENCY FACTOR (20%)
                </div>
                <div style={{ fontSize: '18px', fontWeight: 800, marginTop: '4px' }}>
                  {inspectingOp.scoringBreakdown.dependencyTypeScore.toFixed(2)}
                </div>
                <div style={{ fontSize: '10px', color: '#888888', marginTop: '2px' }}>
                  WEIGHT: {inspectingOp.scoringBreakdown.dependencyTypeWeight}%
                </div>
              </div>

              <div style={{ background: '#111111', padding: '12px', border: '1px solid var(--cc-border)' }}>
                <div style={{ fontSize: '10px', color: '#777777', fontFamily: 'var(--cc-font-mono)' }}>
                  OPERATION FACTOR (50%)
                </div>
                <div style={{ fontSize: '18px', fontWeight: 800, marginTop: '4px' }}>
                  {inspectingOp.scoringBreakdown.operationCriticalityScore.toFixed(2)}
                </div>
                <div style={{ fontSize: '10px', color: '#888888', marginTop: '2px' }}>
                  WEIGHT: {inspectingOp.scoringBreakdown.operationCriticalityWeight}%
                </div>
              </div>
            </div>

            <div
              style={{
                backgroundColor: 'var(--cc-bg-secondary)',
                border: '1px solid var(--cc-border)',
                padding: '12px',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <span className="cc-form-label">COMPOSITE IMPACT SCORE:</span>
              <span style={{ fontSize: '20px', fontWeight: 800, color: 'var(--cc-amber)', fontFamily: 'var(--cc-font-mono)' }}>
                {inspectingOp.scoringBreakdown.totalScore.toFixed(2)} / 4.00
              </span>
            </div>

            <div>
              <span className="cc-form-label">AVAILABILITY EVALUATION REASON:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: '#CCCCCC', lineHeight: 1.5 }}>
                {inspectingOp.availabilityReason}
              </p>
            </div>

            <div>
              <span className="cc-form-label">SEVERITY RATING JUSTIFICATION:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: '#999999', lineHeight: 1.5 }}>
                {inspectingOp.severityReason}
              </p>
            </div>

            <div>
              <span className="cc-form-label">IMPACTED SUPPORTING INFRASTRUCTURE:</span>
              <div style={{ marginTop: '4px', display: 'flex', flexWrap: 'wrap', gap: '6px' }}>
                {inspectingOp.affectedSupportingSystemNames.map((name, i) => (
                  <span key={i} className="cc-badge cc-badge-red">
                    {name}
                  </span>
                ))}
              </div>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
