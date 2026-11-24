import React, { useEffect, useState, useMemo } from 'react'
import { api } from '../services/api'
import type {
  Page,
  IncidentResponseDTO,
  CreateIncidentDTO,
  UpdateIncidentDTO,
  LogisticsSystemResponseDTO,
} from '../types'
import {
  IncidentSeverity,
  IncidentStatus,
  IncidentType,
  SystemLifecycleStatus,
  incidentSeverityLabels,
  incidentStatusLabels,
  incidentTypeLabels,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface IncidentsPageProps {
  onNavigate: (page: Page, systemId?: string) => void
}

export default function IncidentsPage({ onNavigate }: IncidentsPageProps) {
  const [incidents, setIncidents] = useState<IncidentResponseDTO[]>([])
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [severityFilter, setSeverityFilter] = useState<string>('all')
  const [typeFilter, setTypeFilter] = useState<string>('all')

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingIncident, setEditingIncident] = useState<IncidentResponseDTO | null>(null)
  const [selectedIncidentForDetails, setSelectedIncidentForDetails] = useState<IncidentResponseDTO | null>(null)

  // Status transition modal
  const [transitioningIncident, setTransitioningIncident] = useState<IncidentResponseDTO | null>(null)
  const [targetStatus, setTargetStatus] = useState<IncidentStatus>(IncidentStatus.Investigating)
  const [resolutionNotes, setResolutionNotes] = useState('')

  // Form State
  const [createData, setCreateData] = useState<CreateIncidentDTO>({
    failedSystemId: '',
    title: '',
    description: '',
    incidentType: IncidentType.CyberAttack,
    severity: IncidentSeverity.Critical,
    detectedAt: new Date().toISOString().slice(0, 16),
  })
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    loadData()
  }, [])

  async function loadData() {
    setLoading(true)
    setError(null)
    try {
      const [incData, sysData] = await Promise.all([
        api.getIncidents(),
        api.getSystems(false), // active systems for incident creation
      ])
      setIncidents(incData)
      setSystems(sysData)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve incidents.')
    } finally {
      setLoading(false)
    }
  }

  const activeSystems = useMemo(
    () => systems.filter((s) => s.lifecycleStatus === SystemLifecycleStatus.Active),
    [systems]
  )

  function handleOpenCreate() {
    setCreateData({
      failedSystemId: activeSystems[0]?.id || '',
      title: '',
      description: '',
      incidentType: IncidentType.CyberAttack,
      severity: IncidentSeverity.Critical,
      detectedAt: new Date().toISOString().slice(0, 16),
    })
    setFormError(null)
    setIsCreateOpen(true)
  }

  function handleOpenEdit(inc: IncidentResponseDTO) {
    setEditingIncident(inc)
    setFormError(null)
  }

  function handleOpenStatusTransition(inc: IncidentResponseDTO) {
    setTransitioningIncident(inc)
    // Suggest the next natural status step
    if (inc.status === IncidentStatus.Open) setTargetStatus(IncidentStatus.Investigating)
    else if (inc.status === IncidentStatus.Investigating) setTargetStatus(IncidentStatus.Mitigating)
    else if (inc.status === IncidentStatus.Mitigating) setTargetStatus(IncidentStatus.Resolved)
    else if (inc.status === IncidentStatus.Resolved) setTargetStatus(IncidentStatus.Closed)
    else setTargetStatus(IncidentStatus.Closed)
    setResolutionNotes('')
  }

  async function handleCreateSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!createData.failedSystemId) {
      setFormError('Failed system must be selected.')
      return
    }
    if (!createData.title.trim()) {
      setFormError('Incident title is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      const created = await api.createIncident({
        failedSystemId: createData.failedSystemId,
        title: createData.title.trim(),
        description: createData.description?.trim() || undefined,
        incidentType: Number(createData.incidentType) as IncidentType,
        severity: Number(createData.severity) as IncidentSeverity,
        detectedAt: createData.detectedAt ? new Date(createData.detectedAt).toISOString() : undefined,
      })
      setIsCreateOpen(false)
      setActionSuccess(`Disruption Incident "${created.title}" logged.`)
      await loadData()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to report incident.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleEditSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingIncident) return
    if (!editingIncident.title.trim()) {
      setFormError('Title is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      const updatePayload: UpdateIncidentDTO = {
        title: editingIncident.title.trim(),
        description: editingIncident.description?.trim() || undefined,
        incidentType: Number(editingIncident.incidentType) as IncidentType,
        severity: Number(editingIncident.severity) as IncidentSeverity,
      }
      const updated = await api.updateIncident(editingIncident.id, updatePayload)
      setEditingIncident(null)
      setActionSuccess(`Incident "${updated.title}" updated.`)
      if (selectedIncidentForDetails?.id === updated.id) {
        setSelectedIncidentForDetails(updated)
      }
      await loadData()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Update failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleStatusTransitionSubmit() {
    if (!transitioningIncident) return
    setSubmitting(true)
    try {
      const updated = await api.updateIncidentStatus(transitioningIncident.id, {
        status: Number(targetStatus) as IncidentStatus,
        resolutionNotes: resolutionNotes.trim() || undefined,
      })
      setTransitioningIncident(null)
      setActionSuccess(`Incident status transitioned to ${incidentStatusLabels[updated.status]}.`)
      if (selectedIncidentForDetails?.id === updated.id) {
        setSelectedIncidentForDetails(updated)
      }
      await loadData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Status transition failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const filteredIncidents = useMemo(() => {
    return incidents.filter((inc) => {
      const matchesSearch =
        inc.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        inc.failedSystemName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (inc.description && inc.description.toLowerCase().includes(searchTerm.toLowerCase()))
      const matchesStatus = statusFilter === 'all' || inc.status === Number(statusFilter)
      const matchesSeverity = severityFilter === 'all' || inc.severity === Number(severityFilter)
      const matchesType = typeFilter === 'all' || inc.incidentType === Number(typeFilter)
      return matchesSearch && matchesStatus && matchesSeverity && matchesType
    })
  }, [incidents, searchTerm, statusFilter, severityFilter, typeFilter])

  return (
    <div>
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>05 // INCIDENT RESPONSE COMMAND</h1>
          <p>
            Report, triage and track disruption events. Triggers automated blast radius and business operation impact simulation.
          </p>
        </div>

        <button
          type="button"
          className="cc-button cc-button-primary"
          onClick={handleOpenCreate}
          disabled={activeSystems.length === 0}
          title={activeSystems.length === 0 ? 'No active systems available' : undefined}
        >
          + REPORT NEW DISRUPTION
        </button>
      </div>

      {actionSuccess && (
        <div
          className="cc-badge cc-badge-green"
          style={{ width: '100%', padding: '10px 14px', marginBottom: '16px', boxSizing: 'border-box' }}
        >
          ✓ {actionSuccess}
        </div>
      )}

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Toolbar & Filter */}
      <div className="cc-toolbar">
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'center' }}>
          <div className="cc-search-box">
            <span style={{ color: '#555555', fontFamily: 'var(--cc-font-mono)' }}>⌕</span>
            <input
              type="text"
              placeholder="SEARCH INCIDENTS..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="all">ALL STATUSES</option>
            {Object.entries(incidentStatusLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
            value={severityFilter}
            onChange={(e) => setSeverityFilter(e.target.value)}
          >
            <option value="all">ALL SEVERITIES</option>
            {Object.entries(incidentSeverityLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
          >
            <option value="all">ALL INCIDENT TYPES</option>
            {Object.entries(incidentTypeLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>
        </div>

        <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
          RECORDED INCIDENTS: <strong>{filteredIncidents.length}</strong> / {incidents.length}
        </div>
      </div>

      {/* Incident Details Drawer if selected */}
      {selectedIncidentForDetails && (
        <div className="cc-drawer">
          <div className="cc-drawer-header">
            <div>
              <span className="cc-brand-badge" style={{ marginRight: '8px' }}>
                INCIDENT TELEMETRY
              </span>
              <strong style={{ fontSize: '14px' }}>{selectedIncidentForDetails.title}</strong>
            </div>

            <div style={{ display: 'flex', gap: '8px' }}>
              <button
                type="button"
                className="cc-button cc-button-sm cc-button-danger"
                onClick={() => onNavigate('ImpactAnalysis', selectedIncidentForDetails.failedSystemId)}
              >
                VIEW IMPACT ANALYSIS →
              </button>
              {selectedIncidentForDetails.status !== IncidentStatus.Closed && (
                <>
                  <button
                    type="button"
                    className="cc-button cc-button-sm"
                    onClick={() => handleOpenStatusTransition(selectedIncidentForDetails)}
                  >
                    TRANSITION STATUS
                  </button>
                  <button
                    type="button"
                    className="cc-button cc-button-sm"
                    onClick={() => handleOpenEdit(selectedIncidentForDetails)}
                  >
                    EDIT INCIDENT
                  </button>
                </>
              )}
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => setSelectedIncidentForDetails(null)}
              >
                ✕ CLOSE
              </button>
            </div>
          </div>

          <div className="cc-grid-details">
            <div className="cc-detail-item">
              <span className="cc-detail-label">INCIDENT ID:</span>
              <span className="cc-detail-value" style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px' }}>
                {selectedIncidentForDetails.id}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">FAILED COMPONENT:</span>
              <span className="cc-detail-value" style={{ fontWeight: 700 }}>
                {selectedIncidentForDetails.failedSystemName}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">SEVERITY LEVEL:</span>
              <span className="cc-detail-value">
                <StatusBadge type="incidentSeverity" value={selectedIncidentForDetails.severity} />
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">LIFECYCLE STATUS:</span>
              <span className="cc-detail-value">
                <StatusBadge type="incidentStatus" value={selectedIncidentForDetails.status} />
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">DISRUPTION TYPE:</span>
              <span className="cc-detail-value">
                {incidentTypeLabels[selectedIncidentForDetails.incidentType]}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">DETECTED TIMESTAMP:</span>
              <span className="cc-detail-value" style={{ fontFamily: 'var(--cc-font-mono)' }}>
                {new Date(selectedIncidentForDetails.detectedAt).toLocaleString()}
              </span>
            </div>
          </div>

          {selectedIncidentForDetails.description && (
            <div style={{ marginTop: '14px' }}>
              <span className="cc-detail-label">INCIDENT NARRATIVE / DIAGNOSTIC NOTES:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: '#CCCCCC', lineHeight: 1.5 }}>
                {selectedIncidentForDetails.description}
              </p>
            </div>
          )}

          {selectedIncidentForDetails.resolvedAt && (
            <div style={{ marginTop: '8px' }}>
              <span className="cc-detail-label">RESOLVED AT:</span>
              <span style={{ fontSize: '11px', color: 'var(--cc-green)', fontFamily: 'var(--cc-font-mono)', marginLeft: '8px' }}>
                {new Date(selectedIncidentForDetails.resolvedAt).toLocaleString()}
              </span>
            </div>
          )}
        </div>
      )}

      {/* Main Incidents Table */}
      {loading ? (
        <LoadingSpinner label="QUERYING INCIDENT LOGS..." />
      ) : filteredIncidents.length === 0 ? (
        <EmptyState
          title="NO INCIDENTS FOUND"
          description="No disruption incidents matching the filter criteria. All logistics systems are operating normally."
          action={
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={handleOpenCreate}
            >
              + REPORT NEW DISRUPTION
            </button>
          }
        />
      ) : (
        <div className="cc-table-container">
          <table className="cc-table">
            <thead>
              <tr>
                <th>INCIDENT ID</th>
                <th>TITLE</th>
                <th>FAILED SYSTEM</th>
                <th>TYPE</th>
                <th>SEVERITY</th>
                <th>STATUS</th>
                <th>DETECTED</th>
                <th style={{ textAlign: 'right' }}>COMMAND ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredIncidents.map((inc) => {
                const isCriticalOrHigh =
                  inc.severity === IncidentSeverity.Critical || inc.severity === IncidentSeverity.High

                return (
                  <tr
                    key={inc.id}
                    style={{
                      borderLeft: isCriticalOrHigh
                        ? `3px solid ${inc.severity === IncidentSeverity.Critical ? 'var(--cc-red)' : 'var(--cc-amber)'}`
                        : undefined,
                    }}
                  >
                    <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                      {inc.id.substring(0, 8)}...
                    </td>
                    <td>
                      <button
                        type="button"
                        style={{
                          background: 'transparent',
                          border: 'none',
                          color: '#FFFFFF',
                          fontWeight: 700,
                          cursor: 'pointer',
                          padding: 0,
                          textAlign: 'left',
                          fontFamily: 'inherit',
                        }}
                        onClick={() => setSelectedIncidentForDetails(inc)}
                        title="View full incident telemetry"
                      >
                        {inc.title} ↗
                      </button>
                    </td>
                    <td>
                      <span style={{ fontFamily: 'var(--cc-font-mono)', fontWeight: 600 }}>
                        {inc.failedSystemName}
                      </span>
                    </td>
                    <td>
                      <span className="cc-badge cc-badge-neutral">
                        {incidentTypeLabels[inc.incidentType] || 'Other'}
                      </span>
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
                      <div style={{ display: 'inline-flex', gap: '6px' }}>
                        <button
                          type="button"
                          className="cc-button cc-button-sm cc-button-danger"
                          onClick={() => onNavigate('ImpactAnalysis', inc.failedSystemId)}
                          title="Simulate affected systems and operations"
                        >
                          BLAST RADIUS →
                        </button>
                        {inc.status !== IncidentStatus.Closed && (
                          <button
                            type="button"
                            className="cc-button cc-button-sm"
                            onClick={() => handleOpenStatusTransition(inc)}
                            title="Transition lifecycle status"
                          >
                            STATUS
                          </button>
                        )}
                        {inc.status !== IncidentStatus.Closed && (
                          <button
                            type="button"
                            className="cc-button cc-button-sm"
                            onClick={() => handleOpenEdit(inc)}
                          >
                            EDIT
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Create Incident Modal */}
      <Modal
        isOpen={isCreateOpen}
        title="REPORT DISRUPTION INCIDENT"
        onClose={() => setIsCreateOpen(false)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setIsCreateOpen(false)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-danger"
              disabled={submitting}
              onClick={handleCreateSubmit}
            >
              {submitting ? 'RECORDING DISRUPTION...' : 'DECLARE DISRUPTION'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">PRIMARY FAILED LOGISTICS SYSTEM *</label>
            <select
              className="cc-select"
              required
              value={createData.failedSystemId}
              onChange={(e) => setCreateData({ ...createData, failedSystemId: e.target.value })}
            >
              <option value="">-- SELECT COMPONENT EXPERIENCING OUTAGE --</option>
              {activeSystems.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.environment === 3 ? 'PROD' : 'NON-PROD'})
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">INCIDENT TITLE *</label>
            <input
              type="text"
              className="cc-input"
              required
              maxLength={200}
              placeholder="e.g. EDI Gateway TLS Protocol Outage & Cipher Mismatch"
              value={createData.title}
              onChange={(e) => setCreateData({ ...createData, title: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">INCIDENT TYPE *</label>
              <select
                className="cc-select"
                value={createData.incidentType}
                onChange={(e) => setCreateData({ ...createData, incidentType: Number(e.target.value) as IncidentType })}
              >
                {Object.entries(incidentTypeLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">SEVERITY LEVEL *</label>
              <select
                className="cc-select"
                value={createData.severity}
                onChange={(e) => setCreateData({ ...createData, severity: Number(e.target.value) as IncidentSeverity })}
              >
                {Object.entries(incidentSeverityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DETECTED AT TIMESTAMP</label>
            <input
              type="datetime-local"
              className="cc-input cc-input-mono"
              value={createData.detectedAt}
              onChange={(e) => setCreateData({ ...createData, detectedAt: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DIAGNOSTIC NARRATIVE / ROOT CAUSE OBSERVATIONS</label>
            <textarea
              className="cc-textarea"
              rows={3}
              maxLength={2000}
              placeholder="Detailed symptoms, telemetry anomalies, affected endpoints..."
              value={createData.description || ''}
              onChange={(e) => setCreateData({ ...createData, description: e.target.value })}
            />
          </div>
        </form>
      </Modal>

      {/* Edit Incident Modal */}
      <Modal
        isOpen={editingIncident !== null}
        title={`EDIT INCIDENT // ${editingIncident?.id.substring(0, 8) || ''}`}
        onClose={() => setEditingIncident(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setEditingIncident(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleEditSubmit}
            >
              {submitting ? 'SAVING...' : 'COMMIT EDITS'}
            </button>
          </>
        }
      >
        {editingIncident && (
          <form onSubmit={handleEditSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
            {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

            <div className="cc-form-group">
              <label className="cc-form-label">INCIDENT TITLE *</label>
              <input
                type="text"
                className="cc-input"
                required
                maxLength={200}
                value={editingIncident.title}
                onChange={(e) => setEditingIncident({ ...editingIncident, title: e.target.value })}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="cc-form-group">
                <label className="cc-form-label">INCIDENT TYPE *</label>
                <select
                  className="cc-select"
                  value={editingIncident.incidentType}
                  onChange={(e) =>
                    setEditingIncident({ ...editingIncident, incidentType: Number(e.target.value) as IncidentType })
                  }
                >
                  {Object.entries(incidentTypeLabels).map(([key, label]) => (
                    <option key={key} value={key}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="cc-form-group">
                <label className="cc-form-label">SEVERITY LEVEL *</label>
                <select
                  className="cc-select"
                  value={editingIncident.severity}
                  onChange={(e) =>
                    setEditingIncident({ ...editingIncident, severity: Number(e.target.value) as IncidentSeverity })
                  }
                >
                  {Object.entries(incidentSeverityLabels).map(([key, label]) => (
                    <option key={key} value={key}>
                      {label.toUpperCase()}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">DESCRIPTION</label>
              <textarea
                className="cc-textarea"
                rows={3}
                maxLength={2000}
                value={editingIncident.description || ''}
                onChange={(e) => setEditingIncident({ ...editingIncident, description: e.target.value })}
              />
            </div>
          </form>
        )}
      </Modal>

      {/* Transition Status Modal */}
      <Modal
        isOpen={transitioningIncident !== null}
        title="TRANSITION INCIDENT STATUS"
        onClose={() => setTransitioningIncident(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setTransitioningIncident(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleStatusTransitionSubmit}
            >
              {submitting ? 'TRANSITIONING...' : 'COMMIT STATUS CHANGE'}
            </button>
          </>
        }
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          <div className="cc-form-group">
            <span className="cc-form-label">CURRENT STATUS:</span>
            <div style={{ marginTop: '4px' }}>
              {transitioningIncident && (
                <StatusBadge type="incidentStatus" value={transitioningIncident.status} />
              )}
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">TARGET STATUS *</label>
            <select
              className="cc-select"
              value={targetStatus}
              onChange={(e) => setTargetStatus(Number(e.target.value) as IncidentStatus)}
            >
              <option value={IncidentStatus.Open}>Open (Newly Detected)</option>
              <option value={IncidentStatus.Investigating}>Investigating (Triage in progress)</option>
              <option value={IncidentStatus.Mitigating}>Mitigating (Failover / Remediation active)</option>
              <option value={IncidentStatus.Resolved}>Resolved (Normal operations restored)</option>
              <option value={IncidentStatus.Closed}>Closed (Post-mortem signed off)</option>
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">RESOLUTION NOTES / ACTION LOG</label>
            <textarea
              className="cc-textarea"
              rows={3}
              maxLength={2000}
              placeholder="Actions taken, mitigation steps applied, root cause summary..."
              value={resolutionNotes}
              onChange={(e) => setResolutionNotes(e.target.value)}
            />
          </div>
        </div>
      </Modal>
    </div>
  )
}
