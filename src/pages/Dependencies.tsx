import React, { useEffect, useState, useMemo } from 'react'
import { api } from '../services/api'
import type {
  Page,
  SystemDependencyResponseDTO,
  LogisticsSystemResponseDTO,
  CreateSystemDependencyDTO,
} from '../types'
import { DependencyType, SystemLifecycleStatus, dependencyTypeLabels } from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface DependenciesPageProps {
  onNavigate: (page: Page, systemId?: string) => void
}

export default function DependenciesPage({ onNavigate }: DependenciesPageProps) {
  const [dependencies, setDependencies] = useState<SystemDependencyResponseDTO[]>([])
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  // Filters & View Mode
  const [searchTerm, setSearchTerm] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [viewMode, setViewMode] = useState<'table' | 'visual'>('table')

  // Modals
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [editingDep, setEditingDep] = useState<SystemDependencyResponseDTO | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  // Form State
  const [formData, setFormData] = useState<CreateSystemDependencyDTO>({
    dependentSystemId: '',
    dependencySystemId: '',
    dependencyType: DependencyType.Mandatory,
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
      const [depsData, sysData] = await Promise.all([
        api.getDependencies(),
        api.getSystems(false), // only active systems for dependency creation
      ])
      setDependencies(depsData)
      setSystems(sysData)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve dependency relationships.')
    } finally {
      setLoading(false)
    }
  }

  const activeSystems = useMemo(
    () => systems.filter((s) => s.lifecycleStatus === SystemLifecycleStatus.Active),
    [systems]
  )

  function handleOpenAdd() {
    const firstSys = activeSystems[0]?.id || ''
    const secondSys = activeSystems[1]?.id || ''
    setFormData({
      dependentSystemId: firstSys,
      dependencySystemId: secondSys,
      dependencyType: DependencyType.Mandatory,
    })
    setFormError(null)
    setIsAddOpen(true)
  }

  function handleOpenEdit(dep: SystemDependencyResponseDTO) {
    setEditingDep(dep)
    setFormData({
      dependentSystemId: dep.dependentSystemId,
      dependencySystemId: dep.dependencySystemId,
      dependencyType: dep.dependencyType,
    })
    setFormError(null)
  }

  async function handleAddSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!formData.dependentSystemId || !formData.dependencySystemId) {
      setFormError('Both dependent system and provider system are required.')
      return
    }
    if (formData.dependentSystemId === formData.dependencySystemId) {
      setFormError('A system cannot depend on itself (self-dependency is prohibited).')
      return
    }

    setSubmitting(true)
    setFormError(null)
    try {
      await api.createDependency({
        dependentSystemId: formData.dependentSystemId,
        dependencySystemId: formData.dependencySystemId,
        dependencyType: Number(formData.dependencyType) as DependencyType,
      })
      setIsAddOpen(false)
      setActionSuccess('Dependency edge mapped successfully.')
      await loadData()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to map dependency.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleEditSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!editingDep) return

    setSubmitting(true)
    setFormError(null)
    try {
      await api.updateDependency(editingDep.id, {
        dependencyType: Number(formData.dependencyType) as DependencyType,
      })
      setEditingDep(null)
      setActionSuccess('Dependency classification updated.')
      await loadData()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Update failed.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeleteConfirm() {
    if (!deletingId) return
    setSubmitting(true)
    try {
      await api.deleteDependency(deletingId)
      setDeletingId(null)
      setActionSuccess('Dependency relationship removed.')
      await loadData()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Deletion failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const filteredDependencies = useMemo(() => {
    return dependencies.filter((dep) => {
      const depName = dep.dependentSystem?.name || dep.dependentSystemId
      const provName = dep.dependencySystem?.name || dep.dependencySystemId
      const matchesSearch =
        depName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        provName.toLowerCase().includes(searchTerm.toLowerCase())
      const matchesType = typeFilter === 'all' || dep.dependencyType === Number(typeFilter)
      return matchesSearch && matchesType
    })
  }, [dependencies, searchTerm, typeFilter])

  return (
    <div>
      <div className="cc-page-header">
        <div className="cc-page-title-group">
          <h1>03 // DEPENDENCY RELATIONSHIP GRAPH</h1>
          <p>
            Upstream provider to downstream reliant system dependencies. Ingested into the Graph Engine for blast radius calculation.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <div style={{ display: 'inline-flex', border: '1px solid var(--cc-border)' }}>
            <button
              type="button"
              className={`cc-button cc-button-sm ${viewMode === 'table' ? 'cc-button-primary' : ''}`}
              style={{ border: 'none' }}
              onClick={() => setViewMode('table')}
            >
              TABLE VIEW
            </button>
            <button
              type="button"
              className={`cc-button cc-button-sm ${viewMode === 'visual' ? 'cc-button-primary' : ''}`}
              style={{ border: 'none' }}
              onClick={() => setViewMode('visual')}
            >
              RELATIONSHIP FLOW
            </button>
          </div>

          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={handleOpenAdd}
            disabled={activeSystems.length < 2}
            title={activeSystems.length < 2 ? 'At least 2 active systems required' : undefined}
          >
            + MAP DEPENDENCY
          </button>
        </div>
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

      {/* Filter toolbar */}
      <div className="cc-toolbar">
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', alignItems: 'center' }}>
          <div className="cc-search-box">
            <span style={{ color: '#555555', fontFamily: 'var(--cc-font-mono)' }}>⌕</span>
            <input
              type="text"
              placeholder="SEARCH DEPENDENCIES..."
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
            <option value="all">ALL DEPENDENCY TYPES</option>
            {Object.entries(dependencyTypeLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>
        </div>

        <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
          REGISTERED EDGES: <strong>{filteredDependencies.length}</strong> / {dependencies.length}
        </div>
      </div>

      {loading ? (
        <LoadingSpinner label="COMPUTING DEPENDENCY EDGES..." />
      ) : filteredDependencies.length === 0 ? (
        <EmptyState
          title="NO DEPENDENCY EDGES RECORDED"
          description={
            activeSystems.length < 2
              ? 'At least two active logistics systems must be registered before dependency edges can be defined.'
              : 'Define dependency linkages to enable Graph Engine traversal and downstream impact simulation.'
          }
          action={
            activeSystems.length >= 2 ? (
              <button
                type="button"
                className="cc-button cc-button-primary"
                onClick={handleOpenAdd}
              >
                + MAP FIRST DEPENDENCY
              </button>
            ) : undefined
          }
        />
      ) : viewMode === 'table' ? (
        /* TABLE VIEW */
        <div className="cc-table-container">
          <table className="cc-table">
            <thead>
              <tr>
                <th>DEPENDENT SYSTEM (RELIANT)</th>
                <th style={{ textAlign: 'center' }}>RELATIONSHIP</th>
                <th>PROVIDER SYSTEM (DEPENDENCY)</th>
                <th>CLASSIFICATION</th>
                <th>ESTABLISHED</th>
                <th style={{ textAlign: 'right' }}>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredDependencies.map((dep) => (
                <tr key={dep.id}>
                  <td>
                    <div style={{ fontWeight: 700 }}>
                      {dep.dependentSystem?.name || dep.dependentSystemId}
                    </div>
                    {dep.dependentSystem && (
                      <div style={{ fontSize: '11px', color: '#666666' }}>
                        ID: {dep.dependentSystem.id.substring(0, 8)}...
                      </div>
                    )}
                  </td>
                  <td style={{ textAlign: 'center' }}>
                    <span style={{ fontFamily: 'var(--cc-font-mono)', color: '#888888', fontSize: '11px' }}>
                      ── relies upon ──▶
                    </span>
                  </td>
                  <td>
                    <div style={{ fontWeight: 700 }}>
                      {dep.dependencySystem?.name || dep.dependencySystemId}
                    </div>
                    {dep.dependencySystem && (
                      <div style={{ fontSize: '11px', color: '#666666' }}>
                        ID: {dep.dependencySystem.id.substring(0, 8)}...
                      </div>
                    )}
                  </td>
                  <td>
                    <StatusBadge type="dependencyType" value={dep.dependencyType} />
                  </td>
                  <td style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                    {new Date(dep.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'inline-flex', gap: '6px' }}>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => onNavigate('ImpactAnalysis', dep.dependencySystemId)}
                        title="Analyze blast radius if provider fails"
                      >
                        SIMULATE →
                      </button>
                      <button
                        type="button"
                        className="cc-button cc-button-sm"
                        onClick={() => handleOpenEdit(dep)}
                      >
                        EDIT
                      </button>
                      <button
                        type="button"
                        className="cc-button cc-button-sm cc-button-danger"
                        onClick={() => setDeletingId(dep.id)}
                      >
                        DELETE
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        /* VISUAL CARD FLOW VIEW */
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '16px' }}>
          {filteredDependencies.map((dep) => (
            <div
              key={dep.id}
              style={{
                backgroundColor: 'var(--cc-bg-surface)',
                border: '1px solid var(--cc-border)',
                padding: '16px',
                display: 'flex',
                flexDirection: 'column',
                gap: '12px',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span className="cc-brand-badge" style={{ fontSize: '10px' }}>
                  EDGE // {dep.id.substring(0, 8)}
                </span>
                <StatusBadge type="dependencyType" value={dep.dependencyType} />
              </div>

              {/* Edge Visual Connector */}
              <div
                style={{
                  backgroundColor: 'var(--cc-bg-secondary)',
                  border: '1px solid var(--cc-border)',
                  padding: '12px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <div>
                  <div style={{ fontSize: '10px', color: '#888888', fontFamily: 'var(--cc-font-mono)' }}>
                    PROVIDER (SRC)
                  </div>
                  <div style={{ fontWeight: 700, fontSize: '13px' }}>
                    {dep.dependencySystem?.name || dep.dependencySystemId}
                  </div>
                </div>

                <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '14px', color: 'var(--cc-cyan)' }}>
                  ──▶
                </span>

                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: '10px', color: '#888888', fontFamily: 'var(--cc-font-mono)' }}>
                    DEPENDENT (DST)
                  </div>
                  <div style={{ fontWeight: 700, fontSize: '13px' }}>
                    {dep.dependentSystem?.name || dep.dependentSystemId}
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 'auto' }}>
                <span style={{ fontSize: '11px', color: '#666666', fontFamily: 'var(--cc-font-mono)' }}>
                  TYPE: {dep.dependencyType === DependencyType.Mandatory ? 'STRICT PREREQUISITE' : 'FAILOVER PERMITTED'}
                </span>

                <div style={{ display: 'flex', gap: '6px' }}>
                  <button
                    type="button"
                    className="cc-button cc-button-sm"
                    onClick={() => handleOpenEdit(dep)}
                  >
                    EDIT
                  </button>
                  <button
                    type="button"
                    className="cc-button cc-button-sm cc-button-danger"
                    onClick={() => setDeletingId(dep.id)}
                  >
                    DELETE
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Add Dependency Modal */}
      <Modal
        isOpen={isAddOpen}
        title="MAP SYSTEM DEPENDENCY"
        onClose={() => setIsAddOpen(false)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setIsAddOpen(false)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleAddSubmit}
            >
              {submitting ? 'CONNECTING...' : 'ESTABLISH DEPENDENCY'}
            </button>
          </>
        }
      >
        <form onSubmit={handleAddSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">DEPENDENT SYSTEM (The system that requires support) *</label>
            <select
              className="cc-select"
              required
              value={formData.dependentSystemId}
              onChange={(e) => setFormData({ ...formData, dependentSystemId: e.target.value })}
            >
              <option value="">-- SELECT DEPENDENT SYSTEM --</option>
              {activeSystems.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.environment === 3 ? 'PROD' : 'NON-PROD'})
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DEPENDENCY / PROVIDER SYSTEM (The prerequisite system) *</label>
            <select
              className="cc-select"
              required
              value={formData.dependencySystemId}
              onChange={(e) => setFormData({ ...formData, dependencySystemId: e.target.value })}
            >
              <option value="">-- SELECT PROVIDER SYSTEM --</option>
              {activeSystems.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.environment === 3 ? 'PROD' : 'NON-PROD'})
                </option>
              ))}
            </select>
            <span className="cc-form-hint">
              Traversal direction: If Provider fails, Dependent will be marked downstream affected.
            </span>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DEPENDENCY TYPE CLASSIFICATION *</label>
            <select
              className="cc-select"
              value={formData.dependencyType}
              onChange={(e) => setFormData({ ...formData, dependencyType: Number(e.target.value) as DependencyType })}
            >
              <option value={DependencyType.Mandatory}>Mandatory (Strict Outage Cascade)</option>
              <option value={DependencyType.Optional}>Optional (Graceful Degradation / Fallback Available)</option>
            </select>
          </div>
        </form>
      </Modal>

      {/* Edit Dependency Modal */}
      <Modal
        isOpen={editingDep !== null}
        title="MODIFY DEPENDENCY CLASSIFICATION"
        onClose={() => setEditingDep(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setEditingDep(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              disabled={submitting}
              onClick={handleEditSubmit}
            >
              {submitting ? 'UPDATING...' : 'SAVE CHANGES'}
            </button>
          </>
        }
      >
        <form onSubmit={handleEditSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <span className="cc-form-label">RELATIONSHIP PAIR:</span>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '12px', padding: '8px', background: '#111111' }}>
              <strong>{editingDep?.dependencySystem?.name || editingDep?.dependencySystemId}</strong>
              <span style={{ color: 'var(--cc-cyan)', margin: '0 8px' }}>──▶</span>
              <strong>{editingDep?.dependentSystem?.name || editingDep?.dependentSystemId}</strong>
            </div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">DEPENDENCY TYPE *</label>
            <select
              className="cc-select"
              value={formData.dependencyType}
              onChange={(e) => setFormData({ ...formData, dependencyType: Number(e.target.value) as DependencyType })}
            >
              <option value={DependencyType.Mandatory}>Mandatory (Cascading)</option>
              <option value={DependencyType.Optional}>Optional (Non-Cascading)</option>
            </select>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={deletingId !== null}
        title="CONFIRM REMOVAL OF DEPENDENCY EDGE"
        onClose={() => setDeletingId(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setDeletingId(null)}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-danger"
              disabled={submitting}
              onClick={handleDeleteConfirm}
            >
              {submitting ? 'REMOVING...' : 'DELETE EDGE'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px' }}>
          Are you sure you want to delete this dependency edge?
          <br /><br />
          The Graph Engine will no longer traverse from provider to dependent during blast radius impact analysis.
        </p>
      </Modal>
    </div>
  )
}
