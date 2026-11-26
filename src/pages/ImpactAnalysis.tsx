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
      {/* Editorial Page Header */}
      <div className="ed-section-header">
        <div>
          <span className="ed-section-num">06 // IMPACT INTELLIGENCE</span>
          <h1 className="ed-section-title">Operational Blast Radius</h1>
          <p className="ed-section-desc">
            Autonomous Graph Engine traversal calculating downstream system blast radius, operation availability status, and composite severity scoring.
          </p>
        </div>

        <button
          type="button"
          className="ed-action-link"
          style={{ fontSize: '11px', letterSpacing: '0.1em' }}
          disabled={!selectedSystemId || loading}
          onClick={() => triggerAnalysis()}
        >
          {loading ? '[ TRAVERSING GRAPH... ]' : '[ EXECUTE IMPACT ANALYSIS ]'}
        </button>
      </div>

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Target System Selector Strip */}
      <div
        style={{
          border: '1px solid var(--ed-border)',
          background: 'var(--ed-surface)',
          padding: '20px 24px',
          marginBottom: '32px',
          display: 'flex',
          alignItems: 'center',
          gap: '24px',
          flexWrap: 'wrap',
        }}
      >
        <div style={{ flex: 1, minWidth: '280px' }}>
          <label className="ed-stat-label" style={{ display: 'block', marginBottom: '8px' }}>
            SELECT TARGET LOGISTICS NODE TO SIMULATE OUTAGE:
          </label>
          {loadingSystems ? (
            <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>
              LOADING SYSTEMS INVENTORY...
            </div>
          ) : (
            <select
              className="ed-filter-select"
              style={{ width: '100%', maxWidth: '500px' }}
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
              gap: '28px',
              borderLeft: '1px solid var(--ed-border)',
              paddingLeft: '24px',
            }}
          >
            <div>
              <div className="ed-stat-label">CLASSIFICATION</div>
              <div style={{ fontWeight: 600, fontSize: '13px', marginTop: '4px', color: 'var(--ed-text)' }}>
                {systemTypeLabels[selectedSystem.systemType]}
              </div>
            </div>

            <div>
              <div className="ed-stat-label">ENVIRONMENT</div>
              <div style={{ fontWeight: 600, fontSize: '13px', marginTop: '4px', color: 'var(--ed-text)' }}>
                {systemEnvironmentLabels[selectedSystem.environment]}
              </div>
            </div>

            <div>
              <div className="ed-stat-label" style={{ marginBottom: '4px' }}>STATUS</div>
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
              className="ed-action-link"
              disabled={!selectedSystemId}
              onClick={() => triggerAnalysis()}
            >
              [ RUN ANALYSIS ]
            </button>
          }
        />
      )}

      {impactData && !loading && (
        <>
          {/* Asymmetric Metric Grid */}
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
              gap: '24px',
              borderTop: '1px solid var(--ed-border)',
              borderBottom: '1px solid var(--ed-border)',
              padding: '24px 0',
              marginBottom: '32px',
            }}
          >
            <div>
              <div className="ed-stat-label">TOTAL AFFECTED SYSTEMS</div>
              <div
                className="ed-stat-massive"
                style={{
                  color:
                    impactData.totalAffectedSystemsCount > 0
                      ? 'var(--ed-amber)'
                      : 'var(--ed-teal-light)',
                  margin: '8px 0',
                }}
              >
                {impactData.totalAffectedSystemsCount}
              </div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                {impactData.directDependentsCount} DIRECT | {impactData.indirectDependentsCount} INDIRECT
              </div>
            </div>

            <div>
              <div className="ed-stat-label">AFFECTED OPERATIONS</div>
              <div
                className="ed-stat-massive"
                style={{
                  color:
                    impactData.totalAffectedOperationsCount > 0
                      ? 'var(--ed-amber)'
                      : 'var(--ed-text)',
                  margin: '8px 0',
                }}
              >
                {impactData.totalAffectedOperationsCount}
              </div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                <span style={{ color: 'var(--ed-amber)' }}>{impactData.criticalOperationsCount} CRITICAL</span> | {impactData.highOperationsCount} HIGH
              </div>
            </div>

            <div>
              <div className="ed-stat-label">VERIFICATION REQUIRED</div>
              <div
                className="ed-stat-massive"
                style={{
                  color:
                    impactData.requiresVerificationOperationsCount > 0
                      ? 'var(--ed-teal-light)'
                      : 'var(--ed-text)',
                  margin: '8px 0',
                }}
              >
                {impactData.requiresVerificationOperationsCount}
              </div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                ANY-AVAILABLE REDUNDANCY
              </div>
            </div>

            <div>
              <div className="ed-stat-label">SIMULATED NODE</div>
              <div style={{ fontFamily: 'var(--ed-font-heading)', fontSize: '18px', fontWeight: 600, color: 'var(--ed-text)', marginTop: '12px', marginBottom: '8px' }}>
                {impactData.failedSystem.name}
              </div>
              <div>
                <StatusBadge
                  type="systemCriticality"
                  value={impactData.failedSystem.criticality}
                />
              </div>
            </div>
          </div>

          {/* Architectural Directive Banner */}
          <div
            style={{
              border: '1px solid var(--ed-border)',
              borderLeft: '2px solid var(--ed-amber)',
              background: 'var(--ed-surface)',
              padding: '16px 20px',
              marginBottom: '32px',
              fontFamily: 'var(--ed-font-mono)',
              fontSize: '11px',
              color: 'var(--ed-text-secondary)',
              lineHeight: 1.6,
            }}
          >
            <div style={{ fontWeight: 700, letterSpacing: '0.08em', marginBottom: '4px', color: 'var(--ed-text)' }}>
              ARCHITECTURAL DIRECTIVE: SCOPE OF BLAST RADIUS
            </div>
            This simulation evaluates deterministic dependency topology and Availability Rule constraints.
            Downstream systems marked as affected represent potential service degradation pathways.
            Strict operations (<span style={{ color: 'var(--ed-amber)' }}>AllRequired</span>) experience full operational interruption.
            Redundant operations (<span style={{ color: 'var(--ed-teal-light)' }}>AnyAvailable</span>) maintain failover capability pending live verification.
          </div>

          {/* Affected Business Operations Section */}
          <div style={{ marginBottom: '40px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
              <span className="ed-section-num" style={{ margin: 0 }}>06.1 // IMPACTED BUSINESS OPERATIONS</span>
              <span className="cc-badge cc-badge-neutral">
                {impactData.affectedOperations.length} OPERATIONS
              </span>
            </div>

            {impactData.affectedOperations.length === 0 ? (
              <EmptyState
                title="NO BUSINESS OPERATIONS IMPACTED"
                description="The simulated failed system does not support any registered business operations, directly or through downstream dependencies."
              />
            ) : (
              <div style={{ borderTop: '1px solid var(--ed-border)' }}>
                {impactData.affectedOperations.map((op, idx) => {
                  const indexStr = String(idx + 1).padStart(3, '0')
                  return (
                    <div
                      key={op.operationId}
                      className="ed-system-row"
                      style={{
                        display: 'grid',
                        gridTemplateColumns: '50px 2fr 100px 100px 120px 100px 90px 1.5fr 120px',
                        alignItems: 'center',
                        gap: '12px',
                        padding: '14px 8px',
                        borderBottom: '1px solid var(--ed-border)',
                      }}
                    >
                      <div className="ed-system-index">{indexStr}</div>
                      <div>
                        <div style={{ fontFamily: 'var(--ed-font-heading)', fontWeight: 600, fontSize: '14px', color: 'var(--ed-text)' }}>
                          {op.operationName}
                        </div>
                        <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)', marginTop: '2px' }}>
                          {op.affectedSupportingSystemsCount} of {op.totalSupportingSystemsCount} supporting systems impacted
                        </div>
                      </div>
                      <div>
                        <StatusBadge type="operationCriticality" value={op.operationCriticality} />
                      </div>
                      <div>
                        <StatusBadge type="availabilityRule" value={op.availabilityRule} />
                      </div>
                      <div>
                        <StatusBadge type="availabilityStatus" value={op.availabilityStatus} />
                      </div>
                      <div>
                        <StatusBadge type="impactSeverity" value={op.potentialImpactSeverity} />
                      </div>
                      <div>
                        <span style={{ fontFamily: 'var(--ed-font-mono)', fontWeight: 700, fontSize: '13px', color: 'var(--ed-text)' }}>
                          {op.numericSeverityScore.toFixed(2)}
                        </span>
                      </div>
                      <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        {op.contributingDependencyPath.length > 0
                          ? op.contributingDependencyPath.join(' → ')
                          : op.contributingSystemName}
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <button
                          type="button"
                          className="ed-action-link"
                          style={{ fontSize: '11px' }}
                          onClick={() => setInspectingOp(op)}
                        >
                          [INSPECT]
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Directly Affected Systems */}
          <div style={{ marginBottom: '40px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
              <span className="ed-section-num" style={{ margin: 0 }}>06.2 // DIRECTLY RELIANT DOWNSTREAM NODES (DEPTH = 1)</span>
              <span className="cc-badge cc-badge-neutral">
                {impactData.directDependents.length} SYSTEMS
              </span>
            </div>

            {impactData.directDependents.length === 0 ? (
              <div
                style={{
                  padding: '16px',
                  border: '1px dashed var(--ed-border)',
                  color: 'var(--ed-text-muted)',
                  fontSize: '11px',
                  fontFamily: 'var(--ed-font-mono)',
                }}
              >
                No systems directly depend upon this component.
              </div>
            ) : (
              <div style={{ borderTop: '1px solid var(--ed-border)' }}>
                {impactData.directDependents.map((dep, idx) => {
                  const indexStr = String(idx + 1).padStart(3, '0')
                  return (
                    <div
                      key={dep.systemId}
                      className="ed-system-row"
                      style={{
                        display: 'grid',
                        gridTemplateColumns: '50px 2fr 120px 100px 120px 60px 2fr',
                        alignItems: 'center',
                        gap: '12px',
                        padding: '14px 8px',
                        borderBottom: '1px solid var(--ed-border)',
                      }}
                    >
                      <div className="ed-system-index">{indexStr}</div>
                      <div style={{ fontFamily: 'var(--ed-font-heading)', fontWeight: 600, fontSize: '14px', color: 'var(--ed-text)' }}>
                        {dep.systemName}
                      </div>
                      <div>
                        <span className="cc-badge cc-badge-neutral">
                          {systemTypeLabels[dep.systemType]?.split(' ')[0] || 'System'}
                        </span>
                      </div>
                      <div>
                        <StatusBadge type="systemCriticality" value={dep.criticality} />
                      </div>
                      <div>
                        <StatusBadge type="dependencyType" value={dep.dependencyType} />
                      </div>
                      <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text-muted)' }}>
                        D:{dep.depth}
                      </div>
                      <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        {dep.dependencyPath.join(' → ')}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {/* Indirect Cascade Systems */}
          <div style={{ marginBottom: '48px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
              <span className="ed-section-num" style={{ margin: 0 }}>06.3 // INDIRECT CASCADE SYSTEMS (DEPTH &gt; 1)</span>
              <span className="cc-badge cc-badge-neutral">
                {impactData.indirectDependents.length} SYSTEMS
              </span>
            </div>

            {impactData.indirectDependents.length === 0 ? (
              <div
                style={{
                  padding: '16px',
                  border: '1px dashed var(--ed-border)',
                  color: 'var(--ed-text-muted)',
                  fontSize: '11px',
                  fontFamily: 'var(--ed-font-mono)',
                }}
              >
                No second-degree or deeper indirect cascades detected.
              </div>
            ) : (
              <div style={{ borderTop: '1px solid var(--ed-border)' }}>
                {impactData.indirectDependents.map((dep, idx) => {
                  const indexStr = String(idx + 1).padStart(3, '0')
                  return (
                    <div
                      key={dep.systemId}
                      className="ed-system-row"
                      style={{
                        display: 'grid',
                        gridTemplateColumns: '50px 2fr 120px 100px 120px 60px 2fr',
                        alignItems: 'center',
                        gap: '12px',
                        padding: '14px 8px',
                        borderBottom: '1px solid var(--ed-border)',
                      }}
                    >
                      <div className="ed-system-index">{indexStr}</div>
                      <div style={{ fontFamily: 'var(--ed-font-heading)', fontWeight: 600, fontSize: '14px', color: 'var(--ed-text)' }}>
                        {dep.systemName}
                      </div>
                      <div>
                        <span className="cc-badge cc-badge-neutral">
                          {systemTypeLabels[dep.systemType]?.split(' ')[0] || 'System'}
                        </span>
                      </div>
                      <div>
                        <StatusBadge type="systemCriticality" value={dep.criticality} />
                      </div>
                      <div>
                        <StatusBadge type="dependencyType" value={dep.dependencyType} />
                      </div>
                      <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text-muted)' }}>
                        D:{dep.depth}
                      </div>
                      <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        {dep.dependencyPath.join(' → ')}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        </>
      )}

      {/* Inspect Scoring Breakdown Modal */}
      <Modal
        isOpen={inspectingOp !== null}
        title={`SCORING BREAKDOWN: ${inspectingOp?.operationName || ''}`}
        onClose={() => setInspectingOp(null)}
        footer={
          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={() => setInspectingOp(null)}
          >
            [ CLOSE INSPECTION ]
          </button>
        }
      >
        {inspectingOp && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ padding: '12px', background: 'var(--cc-bg-surface-elevated)', border: '1px solid var(--cc-border)' }}>
              <div style={{ fontSize: '11px', color: 'var(--cc-text-muted)', fontFamily: 'var(--cc-font-mono)' }}>COMPOSITE IMPACT SCORE</div>
              <div style={{ fontSize: '28px', fontWeight: 800, fontFamily: 'var(--cc-font-mono)', color: 'var(--cc-amber)', marginTop: '4px' }}>
                {inspectingOp.numericSeverityScore.toFixed(2)} / 5.00
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div>
                <span className="cc-form-label">OPERATION CRITICALITY:</span>
                <div style={{ marginTop: '4px' }}>
                  <StatusBadge type="operationCriticality" value={inspectingOp.operationCriticality} />
                </div>
              </div>
              <div>
                <span className="cc-form-label">AVAILABILITY RULE:</span>
                <div style={{ marginTop: '4px' }}>
                  <StatusBadge type="availabilityRule" value={inspectingOp.availabilityRule} />
                </div>
              </div>
              <div>
                <span className="cc-form-label">POTENTIAL IMPACT SEVERITY:</span>
                <div style={{ marginTop: '4px' }}>
                  <StatusBadge type="impactSeverity" value={inspectingOp.potentialImpactSeverity} />
                </div>
              </div>
              <div>
                <span className="cc-form-label">AVAILABILITY STATUS:</span>
                <div style={{ marginTop: '4px' }}>
                  <StatusBadge type="availabilityStatus" value={inspectingOp.availabilityStatus} />
                </div>
              </div>
            </div>

            <div style={{ borderTop: '1px solid var(--cc-border)', paddingTop: '12px' }}>
              <span className="cc-form-label">AFFECTED INFRASTRUCTURE RATIO:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '13px' }}>
                <strong>{inspectingOp.affectedSupportingSystemsCount}</strong> of <strong>{inspectingOp.totalSupportingSystemsCount}</strong> supporting systems degraded or offline.
              </p>
            </div>

            <div style={{ borderTop: '1px solid var(--cc-border)', paddingTop: '12px' }}>
              <span className="cc-form-label">CONTRIBUTING SYSTEM &amp; TOPOLOGY PATH:</span>
              <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '12px', color: 'var(--cc-text-secondary)', marginTop: '4px' }}>
                {inspectingOp.contributingDependencyPath.length > 0
                  ? inspectingOp.contributingDependencyPath.join(' → ')
                  : inspectingOp.contributingSystemName}
              </div>
            </div>

            <div style={{ borderTop: '1px solid var(--cc-border)', paddingTop: '12px' }}>
              <span className="cc-form-label">WEIGHTED CALCULATION FORMULA:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '11px', color: 'var(--cc-text-muted)', fontFamily: 'var(--cc-font-mono)', lineHeight: 1.5 }}>
                Composite Score = (30% System Criticality) + (20% Dependency Factor) + (50% Business Operation Criticality &amp; Availability Rule Evaluation).
              </p>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
