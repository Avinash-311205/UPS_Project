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

  // Modals & Panels
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
      {/* Editorial Page Header */}
      <div className="ed-section-header">
        <div>
          <span className="ed-section-num">05 // INCIDENT DISPATCH</span>
          <h1 className="ed-section-title">Incident Response Command</h1>
          <p className="ed-section-desc">
            Chronological operational disruption timeline, telemetry triage, and automated failure cascade linkage.
          </p>
        </div>

        <button
          type="button"
          className="ed-action-link"
          style={{ fontSize: '11px', letterSpacing: '0.1em' }}
          onClick={handleOpenCreate}
          disabled={activeSystems.length === 0}
        >
          [ + REPORT DISRUPTION ]
        </button>
      </div>

      {actionSuccess && (
        <div
          className="cc-badge cc-badge-green"
          style={{ width: '100%', padding: '12px 16px', marginBottom: '20px', boxSizing: 'border-box' }}
        >
          [SUCCESS] {actionSuccess}
        </div>
      )}

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Selected Incident Diagnostic Station */}
      {selectedIncidentForDetails && (
        <div
          style={{
            border: '1px solid var(--ed-border)',
            borderLeft: '2px solid var(--ed-amber)',
            background: 'var(--ed-surface)',
            padding: '24px 28px',
            marginBottom: '32px',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '16px', marginBottom: '20px' }}>
            <div>
              <span style={{ fontSize: '10px', color: 'var(--ed-amber)', fontFamily: 'var(--ed-font-mono)', letterSpacing: '0.1em', fontWeight: 700 }}>
                [ACTIVE DIAGNOSTIC TELEMETRY]
              </span>
              <h2 style={{ fontFamily: 'var(--ed-font-heading)', fontSize: '20px', fontWeight: 600, color: 'var(--ed-text)', margin: '4px 0 0 0' }}>
                {selectedIncidentForDetails.title}
              </h2>
            </div>

            <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
              <button
                type="button"
                className="ed-action-link"
                style={{ fontSize: '11px', color: 'var(--ed-amber)' }}
                onClick={() => onNavigate('ImpactAnalysis', selectedIncidentForDetails.failedSystemId)}
              >
                [ BLAST RADIUS → ]
              </button>
              {selectedIncidentForDetails.status !== IncidentStatus.Closed && (
                <>
                  <button
                    type="button"
                    className="ed-action-link"
                    style={{ fontSize: '11px' }}
                    onClick={() => handleOpenStatusTransition(selectedIncidentForDetails)}
                  >
                    [ TRANSITION STATUS ]
                  </button>
                  <button
                    type="button"
                    className="ed-action-link"
                    style={{ fontSize: '11px' }}
                    onClick={() => handleOpenEdit(selectedIncidentForDetails)}
                  >
                    [ EDIT ]
                  </button>
                </>
              )}
              <button
                type="button"
                className="ed-action-link"
                style={{ fontSize: '11px' }}
                onClick={() => setSelectedIncidentForDetails(null)}
              >
                [ ✕ CLOSE ]
              </button>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '20px', paddingBottom: '20px', borderBottom: '1px solid var(--ed-border)' }}>
            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>INCIDENT ID</div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text)', marginTop: '4px' }}>
                {selectedIncidentForDetails.id}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>FAILED COMPONENT</div>
              <div style={{ fontFamily: 'var(--ed-font-heading)', fontSize: '13px', color: 'var(--ed-text)', fontWeight: 600, marginTop: '4px' }}>
                {selectedIncidentForDetails.failedSystemName}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>SEVERITY LEVEL</div>
              <div style={{ marginTop: '4px' }}>
                <StatusBadge type="incidentSeverity" value={selectedIncidentForDetails.severity} />
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>LIFECYCLE STATUS</div>
              <div style={{ marginTop: '4px' }}>
                <StatusBadge type="incidentStatus" value={selectedIncidentForDetails.status} />
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>DISRUPTION TYPE</div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text)', marginTop: '4px' }}>
                {incidentTypeLabels[selectedIncidentForDetails.incidentType]}
              </div>
            </div>

            <div>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)' }}>DETECTED AT</div>
              <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text)', marginTop: '4px' }}>
                {new Date(selectedIncidentForDetails.detectedAt).toLocaleString()}
              </div>
            </div>
          </div>

          {selectedIncidentForDetails.description && (
            <div style={{ marginTop: '16px' }}>
              <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', fontFamily: 'var(--ed-font-mono)', letterSpacing: '0.08em' }}>
                INCIDENT NARRATIVE / DIAGNOSTIC NOTES
              </div>
              <p style={{ margin: '6px 0 0 0', fontSize: '13px', color: 'var(--ed-text-secondary)', lineHeight: 1.6 }}>
                {selectedIncidentForDetails.description}
              </p>
            </div>
          )}

          {selectedIncidentForDetails.resolvedAt && (
            <div style={{ marginTop: '12px', fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-teal-light)' }}>
              RESOLVED AT: {new Date(selectedIncidentForDetails.resolvedAt).toLocaleString()}
            </div>
          )}
        </div>
      )}

      {/* Editorial Filter Strip */}
      <div className="ed-filter-strip">
        <input
          type="text"
          className="ed-filter-input"
          placeholder="SEARCH INCIDENTS..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />

        <select
          className="ed-filter-select"
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
          className="ed-filter-select"
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
          className="ed-filter-select"
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
        >
          <option value="all">ALL DISRUPTION TYPES</option>
          {Object.entries(incidentTypeLabels).map(([key, label]) => (
            <option key={key} value={key}>
              {label.toUpperCase()}
            </option>
          ))}
        </select>

        <div style={{ marginLeft: 'auto', fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
          TOTAL: <span style={{ color: 'var(--ed-text)', fontWeight: 700 }}>{filteredIncidents.length}</span> / {incidents.length}
        </div>
      </div>

      {/* Main Incidents Chronological Timeline */}
      {loading ? (
        <LoadingSpinner label="QUERYING INCIDENT LOGS..." />
      ) : filteredIncidents.length === 0 ? (
        <EmptyState
          title="NO INCIDENTS FOUND"
          description="No disruption incidents matching the filter criteria. All logistics systems are operating normally."
          action={
            <button
              type="button"
              className="ed-action-link"
              onClick={handleOpenCreate}
            >
              [ + REPORT NEW DISRUPTION ]
            </button>
          }
        />
      ) : (
        <div style={{ borderTop: '1px solid var(--ed-border)', marginBottom: '48px' }}>
          {filteredIncidents.map((inc, idx) => {
            const indexStr = String(idx + 1).padStart(3, '0')
            const isCritical = inc.severity === IncidentSeverity.Critical
            const isHigh = inc.severity === IncidentSeverity.High

            return (
              <div
                key={inc.id}
                className="ed-system-row"
                style={{
                  display: 'grid',
                  gridTemplateColumns: '60px 140px 2fr 1.2fr 100px 100px 200px',
                  alignItems: 'center',
                  gap: '16px',
                  padding: '16px 8px',
                  borderBottom: '1px solid var(--ed-border)',
                  borderLeft: isCritical
                    ? '2px solid var(--ed-amber)'
                    : isHigh
                    ? '2px solid rgba(232, 145, 60, 0.5)'
                    : undefined,
                }}
              >
                {/* Index */}
                <div className="ed-system-index">{indexStr}</div>

                {/* Detected Timestamp */}
                <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                  {new Date(inc.detectedAt).toLocaleDateString()}
                  <div style={{ fontSize: '10px' }}>{new Date(inc.detectedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                </div>

                {/* Title & Diagnostic Trigger */}
                <div>
                  <button
                    type="button"
                    style={{
                      background: 'transparent',
                      border: 'none',
                      color: 'var(--ed-text)',
                      fontFamily: 'var(--ed-font-heading)',
                      fontWeight: 600,
                      fontSize: '14px',
                      cursor: 'pointer',
                      padding: 0,
                      textAlign: 'left',
                      letterSpacing: '0.01em',
                    }}
                    onClick={() => setSelectedIncidentForDetails(inc)}
                    title="Inspect incident telemetry"
                  >
                    {inc.title} ↗
                  </button>
                  <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)', marginTop: '2px', fontFamily: 'var(--ed-font-mono)' }}>
                    ID: {inc.id.substring(0, 8)}... | {incidentTypeLabels[inc.incidentType] || 'Other'}
                  </div>
                </div>

                {/* Failed System */}
                <div>
                  <span style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text-secondary)', fontWeight: 600 }}>
                    {inc.failedSystemName}
                  </span>
                </div>

                {/* Severity */}
                <div>
                  <StatusBadge type="incidentSeverity" value={inc.severity} />
                </div>

                {/* Status */}
                <div>
                  <StatusBadge type="incidentStatus" value={inc.status} />
                </div>

                {/* Command Actions */}
                <div style={{ textAlign: 'right', display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                  <button
                    type="button"
                    className="ed-action-link"
                    style={{ fontSize: '11px', color: 'var(--ed-amber)' }}
                    onClick={() => onNavigate('ImpactAnalysis', inc.failedSystemId)}
                    title="Simulate affected systems and operations"
                  >
                    [BLAST RADIUS]
                  </button>
                  {inc.status !== IncidentStatus.Closed && (
                    <>
                      <button
                        type="button"
                        className="ed-action-link"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleOpenStatusTransition(inc)}
                        title="Transition lifecycle status"
                      >
                        [STATUS]
                      </button>
                      <button
                        type="button"
                        className="ed-action-link"
                        style={{ fontSize: '11px' }}
                        onClick={() => handleOpenEdit(inc)}
                      >
                        [EDIT]
                      </button>
                    </>
                  )}
                </div>
              </div>
            )
          })}
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
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="cc-button cc-button-danger"
              disabled={submitting}
              onClick={handleCreateSubmit}
            >
              {submitting ? 'RECORDING DISRUPTION...' : '[ DECLARE DISRUPTION ]'}
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
        title={`EDIT INCIDENT: ${editingIncident?.id.substring(0, 8) || ''}`}
        onClose={() => setEditingIncident(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setEditingIncident(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleEditSubmit}
            >
              {submitting ? 'SAVING...' : '[ COMMIT EDITS ]'}
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
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleStatusTransitionSubmit}
            >
              {submitting ? 'TRANSITIONING...' : '[ COMMIT STATUS CHANGE ]'}
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
