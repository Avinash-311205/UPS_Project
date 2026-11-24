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
  systemLifecycleStatusLabels,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

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
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>02 // LOGISTICS SYSTEMS REGISTRY</h1>
          <p>Inventory, technical classification, criticality tiers and operational state of logistics systems.</p>
        </div>

        <button
          type="button"
          className="cc-button cc-button-primary"
          onClick={handleOpenRegister}
        >
          + REGISTER NEW SYSTEM
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

      {/* Toolbar & Filters */}
      <div className="cc-toolbar">
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'center' }}>
          <div className="cc-search-box">
            <span style={{ color: '#555555', fontFamily: 'var(--cc-font-mono)' }}>⌕</span>
            <input
              type="text"
              placeholder="SEARCH SYSTEMS..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
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
            className="cc-select"
            style={{ width: 'auto' }}
            value={critFilter}
            onChange={(e) => setCritFilter(e.target.value)}
          >
            <option value="all">ALL CRITICALITY</option>
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
              fontFamily: 'var(--cc-font-mono)',
              fontSize: '11px',
              color: '#888888',
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

        <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
          MATCHING: <strong>{filteredSystems.length}</strong> / {systems.length}
        </div>
      </div>

      {/* Detailed System Drawer if one is selected */}
      {selectedSystemForDetails && (
        <div className="cc-drawer">
          <div className="cc-drawer-header">
            <div>
              <span className="cc-brand-badge" style={{ marginRight: '8px' }}>
                SYSTEM DETAILS
              </span>
              <strong style={{ fontSize: '14px', letterSpacing: '0.05em' }}>
                {selectedSystemForDetails.name}
              </strong>
            </div>

            <div style={{ display: 'flex', gap: '8px' }}>
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => onNavigate('ImpactAnalysis', selectedSystemForDetails.id)}
              >
                RUN BLAST RADIUS →
              </button>
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => handleOpenEdit(selectedSystemForDetails)}
              >
                EDIT SYSTEM
              </button>
              <button
                type="button"
                className="cc-button cc-button-sm"
                onClick={() => setSelectedSystemForDetails(null)}
              >
                ✕ CLOSE
              </button>
            </div>
          </div>

          <div className="cc-grid-details">
            <div className="cc-detail-item">
              <span className="cc-detail-label">SYSTEM ID:</span>
              <span className="cc-detail-value" style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px' }}>
                {selectedSystemForDetails.id}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">CLASSIFICATION:</span>
              <span className="cc-detail-value">
                {systemTypeLabels[selectedSystemForDetails.systemType]}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">CRITICALITY TIER:</span>
              <span className="cc-detail-value">
                <StatusBadge type="systemCriticality" value={selectedSystemForDetails.criticality} />
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">ENVIRONMENT:</span>
              <span className="cc-detail-value">
                {systemEnvironmentLabels[selectedSystemForDetails.environment]}
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">LIFECYCLE STATUS:</span>
              <span className="cc-detail-value">
                <StatusBadge type="systemStatus" value={selectedSystemForDetails.lifecycleStatus} />
              </span>
            </div>
            <div className="cc-detail-item">
              <span className="cc-detail-label">REGISTERED AT:</span>
              <span className="cc-detail-value" style={{ fontFamily: 'var(--cc-font-mono)' }}>
                {new Date(selectedSystemForDetails.createdAt).toLocaleString()}
              </span>
            </div>
          </div>

          {selectedSystemForDetails.businessPurpose && (
            <div style={{ marginTop: '12px' }}>
              <span className="cc-detail-label">BUSINESS PURPOSE:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: '#CCCCCC' }}>
                {selectedSystemForDetails.businessPurpose}
              </p>
            </div>
          )}

          {selectedSystemForDetails.description && (
            <div style={{ marginTop: '8px' }}>
              <span className="cc-detail-label">TECHNICAL DESCRIPTION:</span>
              <p style={{ margin: '4px 0 0 0', fontSize: '12px', color: '#999999' }}>
                {selectedSystemForDetails.description}
              </p>
            </div>
          )}
        </div>
      )}

      {/* Main Systems Table */}
      {loading ? (
        <LoadingSpinner label="QUERYING SYSTEMS REGISTRY..." />
      ) : filteredSystems.length === 0 ? (
        <EmptyState
          title="NO MATCHING SYSTEMS FOUND"
          description="Adjust your search query or filter criteria, or register a new logistics system."
          action={
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={handleOpenRegister}
            >
              + REGISTER NEW SYSTEM
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
                <th>CREATED</th>
                <th style={{ textAlign: 'right' }}>OPERATIONAL ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredSystems.map((sys) => (
                <tr key={sys.id}>
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
                      onClick={() => setSelectedSystemForDetails(sys)}
                      title="Inspect technical details"
                    >
                      {sys.name} ↗
                    </button>
                    {sys.businessPurpose && (
                      <div style={{ fontSize: '11px', color: '#666666', marginTop: '2px' }}>
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
                    <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#AAAAAA' }}>
                      {systemEnvironmentLabels[sys.environment]}
                    </span>
                  </td>
                  <td>
                    <StatusBadge type="systemStatus" value={sys.lifecycleStatus} />
                  </td>
                  <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                    {new Date(sys.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'inline-flex', gap: '6px' }}>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => onNavigate('ImpactAnalysis', sys.id)}
                        title="Simulate blast radius"
                      >
                        IMPACT →
                      </button>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => handleOpenEdit(sys)}
                        title="Modify parameters"
                      >
                        EDIT
                      </button>
                      {sys.lifecycleStatus === SystemLifecycleStatus.Active && (
                        <button
                          type="button"
                          className="cc-button cc-button-sm cc-button-danger"
                          onClick={() => setDeactivatingId(sys.id)}
                          title="Soft deactivate system"
                        >
                          DEACTIVATE
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

      {/* Register System Modal */}
      <Modal
        isOpen={isRegisterOpen}
        title="REGISTER LOGISTICS SYSTEM"
        onClose={() => setIsRegisterOpen(false)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setIsRegisterOpen(false)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleRegisterSubmit}
            >
              {submitting ? 'COMMITTING...' : 'REGISTER SYSTEM'}
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
              className="cc-input"
              required
              maxLength={100}
              placeholder="e.g. Warehouse Execution Core (WMS-01)"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">SYSTEM TYPE *</label>
              <select
                className="cc-select"
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
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="cc-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as SystemCriticality })}
              >
                {Object.entries(systemCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">ENVIRONMENT *</label>
              <select
                className="cc-select"
                value={formData.environment}
                onChange={(e) => setFormData({ ...formData, environment: Number(e.target.value) as SystemEnvironment })}
              >
                {Object.entries(systemEnvironmentLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">INITIAL LIFECYCLE STATUS *</label>
              <select
                className="cc-select"
                value={formData.lifecycleStatus}
                onChange={(e) => setFormData({ ...formData, lifecycleStatus: Number(e.target.value) as SystemLifecycleStatus })}
              >
                {Object.entries(systemLifecycleStatusLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">BUSINESS PURPOSE</label>
            <input
              type="text"
              className="cc-input"
              maxLength={500}
              placeholder="e.g. Orchestrates package intake, sortation routing and conveyor handoff."
              value={formData.businessPurpose || ''}
              onChange={(e) => setFormData({ ...formData, businessPurpose: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">TECHNICAL DESCRIPTION</label>
            <textarea
              className="cc-textarea"
              rows={3}
              maxLength={500}
              placeholder="Technical specs, interface protocols, deployment host..."
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>
        </form>
      </Modal>

      {/* Edit System Modal */}
      <Modal
        isOpen={editingSystem !== null}
        title={`EDIT SYSTEM // ${editingSystem?.name || ''}`}
        onClose={() => setEditingSystem(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setEditingSystem(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleUpdateSubmit}
            >
              {submitting ? 'SAVING...' : 'COMMIT CHANGES'}
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
              className="cc-input"
              required
              maxLength={100}
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">SYSTEM TYPE *</label>
              <select
                className="cc-select"
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
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="cc-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as SystemCriticality })}
              >
                {Object.entries(systemCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">ENVIRONMENT *</label>
              <select
                className="cc-select"
                value={formData.environment}
                onChange={(e) => setFormData({ ...formData, environment: Number(e.target.value) as SystemEnvironment })}
              >
                {Object.entries(systemEnvironmentLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">LIFECYCLE STATUS *</label>
              <select
                className="cc-select"
                value={formData.lifecycleStatus}
                onChange={(e) => setFormData({ ...formData, lifecycleStatus: Number(e.target.value) as SystemLifecycleStatus })}
              >
                {Object.entries(systemLifecycleStatusLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">BUSINESS PURPOSE</label>
            <input
              type="text"
              className="cc-input"
              maxLength={500}
              value={formData.businessPurpose || ''}
              onChange={(e) => setFormData({ ...formData, businessPurpose: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">TECHNICAL DESCRIPTION</label>
            <textarea
              className="cc-textarea"
              rows={3}
              maxLength={500}
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>
        </form>
      </Modal>

      {/* Deactivate Confirmation Modal */}
      <Modal
        isOpen={deactivatingId !== null}
        title="CONFIRM DEACTIVATION"
        onClose={() => setDeactivatingId(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setDeactivatingId(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-danger"
              disabled={submitting}
              onClick={handleDeactivateConfirm}
            >
              {submitting ? 'DEACTIVATING...' : 'DEACTIVATE SYSTEM'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px', lineHeight: 1.5 }}>
          Are you sure you want to deactivate this system?
          <br /><br />
          <strong style={{ color: 'var(--cc-text-primary)' }}>Notice:</strong> The record will be soft-deactivated to preserve historical dependency chains, incident associations, and audit telemetry.
        </p>
      </Modal>
    </div>
  )
}
