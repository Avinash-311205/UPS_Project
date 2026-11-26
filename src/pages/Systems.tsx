import React, { useEffect, useState, useMemo, useCallback } from 'react'
import { api } from '../services/api'
import type {
  Page,
  LogisticsSystemResponseDTO,
  CreateLogisticsSystemDTO,
} from '../types'
import {
  SystemType,
  SystemCriticality,
  SystemEnvironment,
  SystemLifecycleStatus,
  systemTypeLabels,
  systemCriticalityLabels,
  systemEnvironmentLabels,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner } from '../components/CommonStates'

interface SystemsPageProps {
  onNavigate: (page: Page, systemId?: string) => void
}

export default function SystemsPage({ onNavigate }: SystemsPageProps) {
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [critFilter, setCritFilter] = useState<string>('all')
  const [includeInactive, setIncludeInactive] = useState(true)

  // Modal State
  const [isRegisterOpen, setIsRegisterOpen] = useState(false)
  const [editingSystem, setEditingSystem] = useState<LogisticsSystemResponseDTO | null>(null)
  const [selectedSystemForDetails, setSelectedSystemForDetails] = useState<LogisticsSystemResponseDTO | null>(null)
  const [deactivatingId, setDeactivatingId] = useState<string | null>(null)

  // Form State
  const [formData, setFormData] = useState<CreateLogisticsSystemDTO>({
    name: '',
    systemType: SystemType.TMS,
    description: '',
    businessPurpose: '',
    criticality: SystemCriticality.Medium,
    environment: SystemEnvironment.Production,
    lifecycleStatus: SystemLifecycleStatus.Active,
  })
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const loadSystems = useCallback(async () => {
    try {
      const data = await api.getSystems(includeInactive)
      setSystems(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve logistics systems.')
    } finally {
      setLoading(false)
    }
  }, [includeInactive])

  useEffect(() => {
    loadSystems()
  }, [loadSystems])

  function handleOpenRegister() {
    setFormData({
      name: '',
      systemType: SystemType.TMS,
      description: '',
      businessPurpose: '',
      criticality: SystemCriticality.Medium,
      environment: SystemEnvironment.Production,
      lifecycleStatus: SystemLifecycleStatus.Active,
    })
    setFormError(null)
    setIsRegisterOpen(true)
  }

  function handleOpenEdit(sys: LogisticsSystemResponseDTO) {
    setEditingSystem(sys)
    setFormData({
      name: sys.name,
      systemType: sys.systemType,
      description: sys.description || '',
      businessPurpose: sys.businessPurpose || '',
      criticality: sys.criticality,
      environment: sys.environment,
      lifecycleStatus: sys.lifecycleStatus,
    })
    setFormError(null)
  }

  async function handleRegisterSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!formData.name.trim()) {
      setFormError('System name is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      await api.createSystem({
        name: formData.name.trim(),
        systemType: Number(formData.systemType) as SystemType,
        description: formData.description?.trim() || undefined,
        businessPurpose: formData.businessPurpose?.trim() || undefined,
        criticality: Number(formData.criticality) as SystemCriticality,
        environment: Number(formData.environment) as SystemEnvironment,
        lifecycleStatus: Number(formData.lifecycleStatus) as SystemLifecycleStatus,
      })
      setIsRegisterOpen(false)
      setActionSuccess(`System "${formData.name.trim()}" registered successfully.`)
      await loadSystems()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Registration failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleUpdateSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingSystem) return
    if (!formData.name.trim()) {
      setFormError('System name is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      const updated = await api.updateSystem(editingSystem.id, {
        name: formData.name.trim(),
        systemType: Number(formData.systemType) as SystemType,
        description: formData.description?.trim() || undefined,
        businessPurpose: formData.businessPurpose?.trim() || undefined,
        criticality: Number(formData.criticality) as SystemCriticality,
        environment: Number(formData.environment) as SystemEnvironment,
        lifecycleStatus: Number(formData.lifecycleStatus) as SystemLifecycleStatus,
      })
      setEditingSystem(null)
      setActionSuccess(`System "${updated.name}" updated successfully.`)
      if (selectedSystemForDetails?.id === updated.id) {
        setSelectedSystemForDetails(updated)
      }
      await loadSystems()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Update failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeactivateConfirm() {
    if (!deactivatingId) return
    setSubmitting(true)
    try {
      await api.deactivateSystem(deactivatingId)
      setDeactivatingId(null)
      setActionSuccess('System deactivated successfully (preserved for audit tracking).')
      await loadSystems()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Deactivation failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const filteredSystems = useMemo(() => {
    return systems.filter((sys) => {
      const matchSearch =
        sys.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (sys.businessPurpose && sys.businessPurpose.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (sys.description && sys.description.toLowerCase().includes(searchTerm.toLowerCase()))

      const matchType = typeFilter === 'all' || sys.systemType === Number(typeFilter)
      const matchCrit = critFilter === 'all' || sys.criticality === Number(critFilter)

      return matchSearch && matchType && matchCrit
    })
  }, [systems, searchTerm, typeFilter, critFilter])

  return (
    <div>
      {/* Editorial Page Header */}
      <div className="ed-page-header">
        <div>
          <div className="ed-section-tag">
            <span>MODULE 02</span>
            <span style={{ opacity: 0.3 }}>/</span>
            <span>NODE DIRECTORY &amp; TECHNICAL REGISTRY</span>
          </div>
          <h1 className="ed-page-title">Logistics Systems Registry</h1>
          <p className="ed-page-subtitle">
            Technical asset inventory, classification tiers, and operational lifecycle state of catalogued logistics infrastructure.
          </p>
        </div>

        <button
          type="button"
          className="ed-button ed-button-primary"
          onClick={handleOpenRegister}
        >
          [ + REGISTER NEW SYSTEM ]
        </button>
      </div>

      {actionSuccess && (
        <div
          className="ed-badge ed-badge-teal"
          style={{ width: '100%', padding: '10px 16px', marginBottom: '20px', boxSizing: 'border-box' }}
        >
          [SUCCESS] {actionSuccess}
        </div>
      )}

      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

      {/* Main Registry Content */}
      {loading ? (
        <LoadingSpinner label="QUERYING SYSTEMS REGISTRY..." />
      ) : systems.length === 0 ? (
        <div className="cc-empty-state">
          <div className="cc-empty-title">00 // ZERO LOGISTICS NODES REGISTERED</div>
          <p className="cc-empty-desc">
            No logistics systems or infrastructure nodes have been provisioned in this tenant yet. Register your primary logistics node (TMS, WMS, ERP, EDI Gateway, Fleet Telematics) to initialize the dependency intelligence graph.
          </p>
          <button
            type="button"
            className="ed-button ed-button-primary"
            onClick={handleOpenRegister}
          >
            [ + REGISTER INITIAL SYSTEM ]
          </button>
        </div>
      ) : (
        <>
          {/* Unobtrusive Minimal Filter Strip */}
          <div className="ed-filter-strip">
            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
              <input
                type="text"
                className="ed-search-input"
                placeholder="SEARCH REGISTRY..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />

              <select
                className="ed-select"
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
              >
                <option value="all">ALL SYSTEM TYPES</option>
                {Object.entries(systemTypeLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>

              <select
                className="ed-select"
                value={critFilter}
                onChange={(e) => setCritFilter(e.target.value)}
              >
                <option value="all">ALL CRITICALITY TIERS</option>
                {Object.entries(systemCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>

              <label
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                  fontFamily: 'var(--ed-font-mono)',
                  fontSize: '11px',
                  color: 'var(--ed-text-secondary)',
                  cursor: 'pointer',
                  userSelect: 'none',
                }}
              >
                <input
                  type="checkbox"
                  checked={includeInactive}
                  onChange={(e) => setIncludeInactive(e.target.checked)}
                />
                SHOW INACTIVE
              </label>
            </div>

            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
              NODES: <strong style={{ color: 'var(--ed-text-primary)' }}>{String(filteredSystems.length).padStart(3, '0')}</strong> / {String(systems.length).padStart(3, '0')}
            </div>
          </div>

          {/* Contextual System Inspection Panel (If selected) */}
          {selectedSystemForDetails && (
            <div className="ed-context-panel" style={{ marginBottom: '32px' }}>
              <div className="ed-context-panel-title">
                <div>
                  <span style={{ color: 'var(--ed-amber)', marginRight: '8px' }}>NODE INSPECTION //</span>
                  <span>{selectedSystemForDetails.name}</span>
                </div>

                <div style={{ display: 'flex', gap: '8px' }}>
                  <button
                    type="button"
                    className="ed-button ed-button-sm ed-button-primary"
                    onClick={() => onNavigate('ImpactAnalysis', selectedSystemForDetails.id)}
                  >
                    [ BLAST RADIUS → ]
                  </button>
                  <button
                    type="button"
                    className="ed-button ed-button-sm"
                    onClick={() => handleOpenEdit(selectedSystemForDetails)}
                  >
                    [ EDIT ]
                  </button>
                  <button
                    type="button"
                    className="ed-button ed-button-sm"
                    onClick={() => setSelectedSystemForDetails(null)}
                  >
                    [ ✕ CLOSE ]
                  </button>
                </div>
              </div>

              <div className="cc-grid-details">
                <div className="cc-detail-item">
                  <span className="cc-detail-label">SYSTEM ID:</span>
                  <span className="cc-detail-value" style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px' }}>
                    {selectedSystemForDetails.id}
                  </span>
                </div>

                <div className="cc-detail-item">
                  <span className="cc-detail-label">TYPE:</span>
                  <span className="cc-detail-value">
                    {systemTypeLabels[selectedSystemForDetails.systemType]}
                  </span>
                </div>

                <div className="cc-detail-item">
                  <span className="cc-detail-label">CRITICALITY:</span>
                  <StatusBadge type="systemCriticality" value={selectedSystemForDetails.criticality} />
                </div>

                <div className="cc-detail-item">
                  <span className="cc-detail-label">ENVIRONMENT:</span>
                  <span className="cc-detail-value" style={{ fontFamily: 'var(--ed-font-mono)' }}>
                    {systemEnvironmentLabels[selectedSystemForDetails.environment]}
                  </span>
                </div>

                <div className="cc-detail-item">
                  <span className="cc-detail-label">STATUS:</span>
                  <StatusBadge type="systemStatus" value={selectedSystemForDetails.lifecycleStatus} />
                </div>

                <div className="cc-detail-item">
                  <span className="cc-detail-label">REGISTERED:</span>
                  <span className="cc-detail-value" style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px' }}>
                    {new Date(selectedSystemForDetails.createdAt).toLocaleString()}
                  </span>
                </div>
              </div>

              {selectedSystemForDetails.businessPurpose && (
                <div style={{ marginTop: '10px' }}>
                  <span className="cc-detail-label">BUSINESS PURPOSE:</span>
                  <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: 'var(--ed-text-secondary)', lineHeight: 1.5 }}>
                    {selectedSystemForDetails.businessPurpose}
                  </p>
                </div>
              )}

              {selectedSystemForDetails.description && (
                <div style={{ marginTop: '8px' }}>
                  <span className="cc-detail-label">TECHNICAL SPECIFICATION:</span>
                  <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: 'var(--ed-text-muted)', lineHeight: 1.5 }}>
                    {selectedSystemForDetails.description}
                  </p>
                </div>
              )}
            </div>
          )}

          {/* Full-Width Numbered Registry Table */}
          {filteredSystems.length === 0 ? (
            <div className="cc-empty-state">
              <div className="cc-empty-title">00 // NO MATCHING SYSTEMS FOUND</div>
              <p className="cc-empty-desc">
                Adjust search keywords or filter criteria to view registered logistics nodes.
              </p>
              <button
                type="button"
                className="ed-button ed-button-secondary"
                onClick={() => {
                  setSearchTerm('')
                  setTypeFilter('all')
                  setCritFilter('all')
                }}
              >
                [ RESET FILTERS ]
              </button>
            </div>
          ) : (
            <div className="ed-table-container">
              <table className="ed-table">
                <thead>
                  <tr>
                    <th style={{ width: '44px' }}>#</th>
                    <th>SYSTEM NAME &amp; PURPOSE</th>
                    <th>CLASSIFICATION</th>
                    <th>CRITICALITY</th>
                    <th>ENVIRONMENT</th>
                    <th>LIFECYCLE</th>
                    <th>REGISTERED</th>
                    <th style={{ textAlign: 'right' }}>COMMAND ACTIONS</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredSystems.map((sys, idx) => (
                    <tr key={sys.id}>
                      <td className="ed-row-idx">
                        {String(idx + 1).padStart(3, '0')}
                      </td>
                      <td>
                        <button
                          type="button"
                          style={{
                            background: 'transparent',
                            border: 'none',
                            color: 'var(--ed-text-primary)',
                            fontWeight: 700,
                            cursor: 'pointer',
                            padding: 0,
                            textAlign: 'left',
                            fontFamily: 'inherit',
                            fontSize: '13px',
                          }}
                          onClick={() => setSelectedSystemForDetails(sys)}
                          title="Inspect node telemetry"
                        >
                          {sys.name} <span style={{ opacity: 0.4 }}>↗</span>
                        </button>
                        {sys.businessPurpose && (
                          <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)', marginTop: '2px' }}>
                            {sys.businessPurpose}
                          </div>
                        )}
                      </td>
                      <td>
                        <span className="cc-badge cc-badge-neutral">
                          {systemTypeLabels[sys.systemType]?.split(' ')[0] || 'System'}
                        </span>
                      </td>
                      <td>
                        <StatusBadge type="systemCriticality" value={sys.criticality} />
                      </td>
                      <td>
                        <span style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-secondary)' }}>
                          {systemEnvironmentLabels[sys.environment]}
                        </span>
                      </td>
                      <td>
                        <StatusBadge type="systemStatus" value={sys.lifecycleStatus} />
                      </td>
                      <td style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        {new Date(sys.createdAt).toLocaleDateString()}
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '6px' }}>
                          <button
                            type="button"
                            className="ed-button ed-button-sm ed-button-secondary"
                            onClick={() => onNavigate('ImpactAnalysis', sys.id)}
                            title="Simulate blast radius"
                          >
                            [ BLAST RADIUS → ]
                          </button>
                          <button
                            type="button"
                            className="ed-button ed-button-sm"
                            onClick={() => handleOpenEdit(sys)}
                            title="Edit metadata"
                          >
                            [ EDIT ]
                          </button>
                          {sys.lifecycleStatus === SystemLifecycleStatus.Active && (
                            <button
                              type="button"
                              className="ed-button ed-button-sm ed-button-danger"
                              onClick={() => setDeactivatingId(sys.id)}
                              title="Soft deactivate node"
                            >
                              [ DEACTIVATE ]
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}

      {/* Register System Modal */}
      <Modal
        isOpen={isRegisterOpen}
        title="REGISTER LOGISTICS SYSTEM"
        onClose={() => setIsRegisterOpen(false)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setIsRegisterOpen(false)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-primary"
              disabled={submitting}
              onClick={handleRegisterSubmit}
            >
              {submitting ? 'COMMITTING...' : '[ REGISTER SYSTEM ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleRegisterSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">SYSTEM NAME *</label>
            <input
              type="text"
              className="ed-input"
              placeholder="e.g., SAP-TMS-CORE"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">SYSTEM TYPE *</label>
            <select
              className="ed-select"
              value={formData.systemType}
              onChange={(e) => setFormData({ ...formData, systemType: Number(e.target.value) as SystemType })}
            >
              {Object.entries(systemTypeLabels).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">BUSINESS PURPOSE</label>
            <input
              type="text"
              className="ed-input"
              placeholder="Operational role in supply chain"
              value={formData.businessPurpose || ''}
              onChange={(e) => setFormData({ ...formData, businessPurpose: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">TECHNICAL DESCRIPTION</label>
            <textarea
              className="ed-textarea"
              rows={3}
              placeholder="Architecture, protocols, or hosting details"
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="ed-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as SystemCriticality })}
              >
                {Object.entries(systemCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">ENVIRONMENT *</label>
              <select
                className="ed-select"
                value={formData.environment}
                onChange={(e) => setFormData({ ...formData, environment: Number(e.target.value) as SystemEnvironment })}
              >
                {Object.entries(systemEnvironmentLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </form>
      </Modal>

      {/* Edit System Modal */}
      <Modal
        isOpen={Boolean(editingSystem)}
        title={`EDIT SYSTEM: ${editingSystem?.name || ''}`}
        onClose={() => setEditingSystem(null)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setEditingSystem(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-primary"
              disabled={submitting}
              onClick={handleUpdateSubmit}
            >
              {submitting ? 'SAVING...' : '[ SAVE CHANGES ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleUpdateSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">SYSTEM NAME *</label>
            <input
              type="text"
              className="ed-input"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">SYSTEM TYPE *</label>
            <select
              className="ed-select"
              value={formData.systemType}
              onChange={(e) => setFormData({ ...formData, systemType: Number(e.target.value) as SystemType })}
            >
              {Object.entries(systemTypeLabels).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">BUSINESS PURPOSE</label>
            <input
              type="text"
              className="ed-input"
              value={formData.businessPurpose || ''}
              onChange={(e) => setFormData({ ...formData, businessPurpose: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">TECHNICAL DESCRIPTION</label>
            <textarea
              className="ed-textarea"
              rows={3}
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="ed-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as SystemCriticality })}
              >
                {Object.entries(systemCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">ENVIRONMENT *</label>
              <select
                className="ed-select"
                value={formData.environment}
                onChange={(e) => setFormData({ ...formData, environment: Number(e.target.value) as SystemEnvironment })}
              >
                {Object.entries(systemEnvironmentLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label.toUpperCase()}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </form>
      </Modal>

      {/* Deactivate Confirmation Modal */}
      <Modal
        isOpen={Boolean(deactivatingId)}
        title="CONFIRM SYSTEM DEACTIVATION"
        onClose={() => setDeactivatingId(null)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setDeactivatingId(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-danger"
              disabled={submitting}
              onClick={handleDeactivateConfirm}
            >
              {submitting ? 'DEACTIVATING...' : '[ CONFIRM DEACTIVATION ]'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px', color: 'var(--ed-text-secondary)', lineHeight: 1.6 }}>
          Are you sure you want to deactivate this logistics system? The record will be preserved for audit continuity, but will be excluded from default graph traversals and active impact evaluations.
        </p>
      </Modal>
    </div>
  )
}
