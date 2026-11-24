import React, { useEffect, useState, useMemo, useCallback } from 'react'
import { api } from '../services/api'
import type {
  Page,
  BusinessOperationResponseDTO,
  CreateBusinessOperationDTO,
  LogisticsSystemResponseDTO,
  SystemBusinessOperationResponseDTO,
} from '../types'
import {
  OperationCriticality,
  OperationLifecycleStatus,
  AvailabilityRule,
  operationCriticalityLabels,
  availabilityRuleLabels,
  SystemLifecycleStatus,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface BusinessOperationsPageProps {
  onNavigate: (page: Page, systemId?: string) => void
}

export default function BusinessOperationsPage({ onNavigate: _onNavigate }: BusinessOperationsPageProps) {
  const [operations, setOperations] = useState<BusinessOperationResponseDTO[]>([])
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [critFilter, setCritFilter] = useState<string>('all')
  const [ruleFilter, setRuleFilter] = useState<string>('all')
  const [includeInactive, setIncludeInactive] = useState(true)

  // Modals
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingOp, setEditingOp] = useState<BusinessOperationResponseDTO | null>(null)
  const [deactivatingId, setDeactivatingId] = useState<string | null>(null)

  // Manage Systems for Operation Modal
  const [managingOp, setManagingOp] = useState<BusinessOperationResponseDTO | null>(null)
  const [assignedSystems, setAssignedSystems] = useState<SystemBusinessOperationResponseDTO[]>([])
  const [loadingAssigned, setLoadingAssigned] = useState(false)
  const [selectedSystemToAssign, setSelectedSystemToAssign] = useState<string>('')
  const [assignError, setAssignError] = useState<string | null>(null)

  // Form State
  const [formData, setFormData] = useState<CreateBusinessOperationDTO>({
    name: '',
    description: '',
    criticality: OperationCriticality.Medium,
    availabilityRule: AvailabilityRule.AllRequired,
  })
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const loadData = useCallback(async () => {
    try {
      const [opsData, sysData] = await Promise.all([
        api.getOperations(includeInactive),
        api.getSystems(false), // active systems for assignment
      ])
      setOperations(opsData)
      setSystems(sysData)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve business operations.')
    } finally {
      setLoading(false)
    }
  }, [includeInactive])

  useEffect(() => {
    loadData()
  }, [loadData])

  function handleOpenCreate() {
    setFormData({
      name: '',
      description: '',
      criticality: OperationCriticality.Medium,
      availabilityRule: AvailabilityRule.AllRequired,
    })
    setFormError(null)
    setIsCreateOpen(true)
  }

  function handleOpenEdit(op: BusinessOperationResponseDTO) {
    setEditingOp(op)
    setFormData({
      name: op.name,
      description: op.description || '',
      criticality: op.criticality,
      availabilityRule: op.availabilityRule,
    })
    setFormError(null)
  }

  async function handleCreateSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!formData.name.trim()) {
      setFormError('Operation name is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      await api.createOperation({
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
        criticality: Number(formData.criticality) as OperationCriticality,
        availabilityRule: Number(formData.availabilityRule) as AvailabilityRule,
      })
      setIsCreateOpen(false)
      setActionSuccess(`Business operation "${formData.name.trim()}" created.`)
      await loadData()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Creation failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleEditSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingOp) return
    if (!formData.name.trim()) {
      setFormError('Operation name is required.')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      await api.updateOperation(editingOp.id, {
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
        lifecycleStatus: editingOp.lifecycleStatus,
        criticality: Number(formData.criticality) as OperationCriticality,
        availabilityRule: Number(formData.availabilityRule) as AvailabilityRule,
      })
      setEditingOp(null)
      setActionSuccess(`Operation "${formData.name.trim()}" updated.`)
      await loadData()
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
      await api.deactivateOperation(deactivatingId)
      setDeactivatingId(null)
      setActionSuccess('Business operation deactivated.')
      await loadData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Deactivation failed.')
    } finally {
      setSubmitting(false)
    }
  }

  // Manage Supporting Systems Handlers
  async function handleOpenManageSystems(op: BusinessOperationResponseDTO) {
    setManagingOp(op)
    setSelectedSystemToAssign('')
    setAssignError(null)
    setLoadingAssigned(true)
    try {
      const data = await api.getSystemsForOperation(op.id)
      setAssignedSystems(data)
    } catch (err) {
      setAssignError(err instanceof Error ? err.message : 'Failed to retrieve supporting systems.')
    } finally {
      setLoadingAssigned(false)
    }
  }

  async function handleAssignSystemSubmit() {
    if (!managingOp || !selectedSystemToAssign) return
    setSubmitting(true)
    setAssignError(null)
    try {
      await api.assignSystemToOperation(managingOp.id, selectedSystemToAssign)
      setSelectedSystemToAssign('')
      const updated = await api.getSystemsForOperation(managingOp.id)
      setAssignedSystems(updated)
      setActionSuccess('Supporting system assigned successfully.')
      await loadData()
    } catch (err) {
      setAssignError(err instanceof Error ? err.message : 'Assignment failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRemoveAssignedSystem(systemId: string) {
    if (!managingOp) return
    setSubmitting(true)
    setAssignError(null)
    try {
      await api.removeSystemFromOperation(managingOp.id, systemId)
      const updated = await api.getSystemsForOperation(managingOp.id)
      setAssignedSystems(updated)
      setActionSuccess('Supporting system unassigned.')
      await loadData()
    } catch (err) {
      setAssignError(err instanceof Error ? err.message : 'Removal failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const filteredOperations = useMemo(() => {
    return operations.filter((op) => {
      const matchesSearch =
        op.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (op.description && op.description.toLowerCase().includes(searchTerm.toLowerCase()))
      const matchesCrit = critFilter === 'all' || op.criticality === Number(critFilter)
      const matchesRule = ruleFilter === 'all' || op.availabilityRule === Number(ruleFilter)
      return matchesSearch && matchesCrit && matchesRule
    })
  }, [operations, searchTerm, critFilter, ruleFilter])

  // Systems available to be assigned to managingOp (not yet assigned)
  const unassignedSystems = useMemo(() => {
    const assignedIds = new Set(assignedSystems.map((a) => a.logisticsSystemId))
    return systems.filter(
      (s) => s.lifecycleStatus === SystemLifecycleStatus.Active && !assignedIds.has(s.id)
    )
  }, [systems, assignedSystems])

  return (
    <div>
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>04 // BUSINESS OPERATIONS CATALOG</h1>
          <p>
            Critical operational workflows, assigned supporting systems, and availability resilience rules (AllRequired vs AnyAvailable).
          </p>
        </div>

        <button
          type="button"
          className="cc-button cc-button-primary"
          onClick={handleOpenCreate}
        >
          + DEFINE BUSINESS OPERATION
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

      {/* Availability Rule Architecture Callout */}
      <div
        style={{
          backgroundColor: 'var(--cc-bg-surface)',
          border: '1px solid var(--cc-border)',
          padding: '14px 18px',
          marginBottom: '20px',
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: '16px',
        }}
      >
        <div style={{ borderRight: '1px solid var(--cc-border)', paddingRight: '16px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <span className="cc-badge cc-badge-amber">ALL REQUIRED (STRICT)</span>
            <span style={{ fontSize: '11px', color: '#888888', fontFamily: 'var(--cc-font-mono)' }}>
              DEFAULT EVALUATION
            </span>
          </div>
          <p style={{ margin: 0, fontSize: '11px', color: '#999999', lineHeight: 1.4 }}>
            Operation requires <strong>every supporting system</strong> to remain operational. If any supporting system fails, the operation is evaluated as <strong style={{ color: 'var(--cc-red)' }}>Potentially Affected</strong>.
          </p>
        </div>

        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <span className="cc-badge cc-badge-cyan">ANY AVAILABLE (FAILOVER)</span>
            <span style={{ fontSize: '11px', color: '#888888', fontFamily: 'var(--cc-font-mono)' }}>
              HIGH AVAILABILITY REDUNDANCY
            </span>
          </div>
          <p style={{ margin: 0, fontSize: '11px', color: '#999999', lineHeight: 1.4 }}>
            Operation can tolerate single system outages if alternative supporting systems remain online. Evaluated as <strong style={{ color: 'var(--cc-amber)' }}>Requires Verification</strong> instead of catastrophic disruption.
          </p>
        </div>
      </div>

      {/* Toolbar & Filter */}
      <div className="cc-toolbar">
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'center' }}>
          <div className="cc-search-box">
            <span style={{ color: '#555555', fontFamily: 'var(--cc-font-mono)' }}>⌕</span>
            <input
              type="text"
              placeholder="SEARCH OPERATIONS..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
            value={critFilter}
            onChange={(e) => setCritFilter(e.target.value)}
          >
            <option value="all">ALL CRITICALITY</option>
            {Object.entries(operationCriticalityLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>

          <select
            className="cc-select"
            style={{ width: 'auto' }}
            value={ruleFilter}
            onChange={(e) => setRuleFilter(e.target.value)}
          >
            <option value="all">ALL AVAILABILITY RULES</option>
            {Object.entries(availabilityRuleLabels).map(([key, label]) => (
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
          OPERATIONS: <strong>{filteredOperations.length}</strong> / {operations.length}
        </div>
      </div>

      {/* Operations Table */}
      {loading ? (
        <LoadingSpinner label="RETRIEVING BUSINESS OPERATIONS..." />
      ) : filteredOperations.length === 0 ? (
        <EmptyState
          title="NO BUSINESS OPERATIONS REGISTERED"
          description="Define business operations and map their supporting systems to enable Impact Analysis scoring."
          action={
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={handleOpenCreate}
            >
              + DEFINE FIRST OPERATION
            </button>
          }
        />
      ) : (
        <div className="cc-table-container">
          <table className="cc-table">
            <thead>
              <tr>
                <th>OPERATION NAME</th>
                <th>CRITICALITY</th>
                <th>AVAILABILITY RULE</th>
                <th>SUPPORTING SYSTEMS</th>
                <th>STATUS</th>
                <th style={{ textAlign: 'right' }}>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredOperations.map((op) => (
                <tr key={op.id}>
                  <td>
                    <div style={{ fontWeight: 700 }}>{op.name}</div>
                    {op.description && (
                      <div style={{ fontSize: '11px', color: '#666666' }}>{op.description}</div>
                    )}
                  </td>
                  <td>
                    <StatusBadge type="operationCriticality" value={op.criticality} />
                  </td>
                  <td>
                    <StatusBadge type="availabilityRule" value={op.availabilityRule} />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="cc-button cc-button-sm"
                      onClick={() => handleOpenManageSystems(op)}
                      title="Manage supporting infrastructure"
                    >
                      {op.supportingSystemsCount} SYSTEMS [⚙]
                    </button>
                  </td>
                  <td>
                    <StatusBadge type="operationStatus" value={op.lifecycleStatus} />
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'inline-flex', gap: '6px' }}>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => handleOpenManageSystems(op)}
                      >
                        ASSIGN SYSTEMS
                      </button>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => handleOpenEdit(op)}
                      >
                        EDIT
                      </button>
                      {op.lifecycleStatus === OperationLifecycleStatus.Active && (
                        <button
                          type="button"
                          className="cc-button cc-button-sm cc-button-danger"
                          onClick={() => setDeactivatingId(op.id)}
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

      {/* Create Operation Modal */}
      <Modal
        isOpen={isCreateOpen}
        title="DEFINE BUSINESS OPERATION"
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
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleCreateSubmit}
            >
              {submitting ? 'SAVING...' : 'CREATE OPERATION'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">OPERATION NAME *</label>
            <input
              type="text"
              className="cc-input"
              required
              maxLength={100}
              placeholder="e.g. Cross-Dock Dispatch Management"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
            <div className="cc-form-group">
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="cc-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as OperationCriticality })}
              >
                {Object.entries(operationCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">AVAILABILITY RULE *</label>
              <select
                className="cc-select"
                value={formData.availabilityRule}
                onChange={(e) => setFormData({ ...formData, availabilityRule: Number(e.target.value) as AvailabilityRule })}
              >
                <option value={AvailabilityRule.AllRequired}>All Required (Strict Dependency)</option>
                <option value={AvailabilityRule.AnyAvailable}>Any Available (Active Redundancy)</option>
              </select>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DESCRIPTION</label>
            <textarea
              className="cc-textarea"
              rows={3}
              maxLength={500}
              placeholder="Operational scope, service level requirements, business impact..."
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            />
          </div>
        </form>
      </Modal>

      {/* Edit Operation Modal */}
      <Modal
        isOpen={editingOp !== null}
        title={`EDIT OPERATION // ${editingOp?.name || ''}`}
        onClose={() => setEditingOp(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setEditingOp(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleEditSubmit}
            >
              {submitting ? 'SAVING...' : 'COMMIT CHANGES'}
            </button>
          </>
        }
      >
        <form onSubmit={handleEditSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">OPERATION NAME *</label>
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
              <label className="cc-form-label">CRITICALITY TIER *</label>
              <select
                className="cc-select"
                value={formData.criticality}
                onChange={(e) => setFormData({ ...formData, criticality: Number(e.target.value) as OperationCriticality })}
              >
                {Object.entries(operationCriticalityLabels).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div className="cc-form-group">
              <label className="cc-form-label">AVAILABILITY RULE *</label>
              <select
                className="cc-select"
                value={formData.availabilityRule}
                onChange={(e) => setFormData({ ...formData, availabilityRule: Number(e.target.value) as AvailabilityRule })}
              >
                <option value={AvailabilityRule.AllRequired}>All Required (Strict Dependency)</option>
                <option value={AvailabilityRule.AnyAvailable}>Any Available (Active Redundancy)</option>
              </select>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DESCRIPTION</label>
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

      {/* Manage Supporting Systems Modal */}
      <Modal
        isOpen={managingOp !== null}
        title={`SUPPORTING SYSTEMS // ${managingOp?.name || ''}`}
        onClose={() => setManagingOp(null)}
        footer={
          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={() => setManagingOp(null)}
          >
            DONE
          </button>
        }
      >
        <div>
          {assignError && <div className="cc-form-error" style={{ marginBottom: '12px' }}>[ERROR] {assignError}</div>}

          {/* Current Assigned Systems List */}
          <div style={{ marginBottom: '16px' }}>
            <span className="cc-form-label">CURRENTLY ASSIGNED SUPPORTING SYSTEMS ({assignedSystems.length}):</span>
            {loadingAssigned ? (
              <LoadingSpinner label="LOADING ASSIGNMENTS..." />
            ) : assignedSystems.length === 0 ? (
              <div style={{ padding: '12px', border: '1px dashed var(--cc-border)', color: '#777777', fontSize: '11px', marginTop: '6px' }}>
                No systems currently assigned to support this operation.
              </div>
            ) : (
              <div style={{ marginTop: '8px', display: 'flex', flexDirection: 'column', gap: '6px' }}>
                {assignedSystems.map((item) => (
                  <div
                    key={item.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      padding: '8px 12px',
                      backgroundColor: 'var(--cc-bg-secondary)',
                      border: '1px solid var(--cc-border)',
                    }}
                  >
                    <div>
                      <strong style={{ fontSize: '12px' }}>{item.logisticsSystemName}</strong>
                      <span style={{ fontSize: '10px', color: '#666666', marginLeft: '8px', fontFamily: 'var(--cc-font-mono)' }}>
                        {item.logisticsSystemId.substring(0, 8)}...
                      </span>
                    </div>

                    <button
                      type="button"
                      className="cc-button cc-button-sm cc-button-danger"
                      disabled={submitting}
                      onClick={() => handleRemoveAssignedSystem(item.logisticsSystemId)}
                    >
                      REMOVE
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Assign New System Section */}
          <div style={{ borderTop: '1px solid var(--cc-border)', paddingTop: '16px' }}>
            <span className="cc-form-label">ASSIGN ADDITIONAL SUPPORTING SYSTEM:</span>
            {unassignedSystems.length === 0 ? (
              <div style={{ fontSize: '11px', color: '#777777', marginTop: '6px' }}>
                All available active systems are already assigned to this operation.
              </div>
            ) : (
              <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
                <select
                  className="cc-select"
                  value={selectedSystemToAssign}
                  onChange={(e) => setSelectedSystemToAssign(e.target.value)}
                >
                  <option value="">-- SELECT SYSTEM TO ASSIGN --</option>
                  {unassignedSystems.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.environment === 3 ? 'PROD' : 'NON-PROD'})
                    </option>
                  ))}
                </select>

                <button
                  type="button"
                  className="cc-button cc-button-primary"
                  disabled={!selectedSystemToAssign || submitting}
                  onClick={handleAssignSystemSubmit}
                >
                  ASSIGN
                </button>
              </div>
            )}
          </div>
        </div>
      </Modal>

      {/* Deactivate Operation Modal */}
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
              {submitting ? 'DEACTIVATING...' : 'DEACTIVATE OPERATION'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px' }}>
          Are you sure you want to deactivate this business operation?
          <br /><br />
          The operation will be soft-deactivated to preserve historical impact telemetry.
        </p>
      </Modal>
    </div>
  )
}
