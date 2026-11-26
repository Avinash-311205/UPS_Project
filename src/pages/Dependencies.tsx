import React, { useEffect, useState, useMemo } from 'react'
import { api } from '../services/api'
import type {
  Page,
  SystemDependencyResponseDTO,
  LogisticsSystemResponseDTO,
  CreateSystemDependencyDTO,
} from '../types'
import {
  DependencyType,
  SystemLifecycleStatus,
  dependencyTypeLabels,
  systemEnvironmentLabels,
  systemTypeLabels,
} from '../types'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner } from '../components/CommonStates'

interface DependenciesPageProps {
  onNavigate: (page: Page, systemId?: string) => void
}

export default function DependenciesPage({ onNavigate }: DependenciesPageProps) {
  const [dependencies, setDependencies] = useState<SystemDependencyResponseDTO[]>([])
  const [systems, setSystems] = useState<LogisticsSystemResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionSuccess, setActionSuccess] = useState<string | null>(null)

  // Filters & Selected State
  const [searchTerm, setSearchTerm] = useState('')
  const [typeFilter, setTypeFilter] = useState<string>('all')
  const [viewMode, setViewMode] = useState<'graph' | 'table'>('graph')
  const [selectedDepId, setSelectedDepId] = useState<string | null>(null)
  const [selectedSystemId, setSelectedSystemId] = useState<string | null>(null)

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
        api.getSystems(false),
      ])
      setDependencies(depsData)
      setSystems(sysData)
      if (depsData.length > 0 && !selectedDepId) {
        setSelectedDepId(depsData[0].id)
      }
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
      if (selectedDepId === deletingId) {
        setSelectedDepId(null)
      }
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

  const selectedDep = useMemo(() => {
    return dependencies.find((d) => d.id === selectedDepId) || dependencies[0] || null
  }, [dependencies, selectedDepId])

  const selectedSystem = useMemo(() => {
    return systems.find((s) => s.id === selectedSystemId) || null
  }, [systems, selectedSystemId])

  // Compute node coordinates for topological graph visualization
  const graphNodes = useMemo(() => {
    const list = activeSystems
    const count = list.length
    if (count === 0) return []
    const width = 680
    const height = 400
    const padding = 60

    return list.map((sys, idx) => {
      // 2 columns or circular / tiered distribution
      const isLeft = idx % 2 === 0
      const row = Math.floor(idx / 2)
      const totalRows = Math.ceil(count / 2) || 1
      const x = isLeft ? padding + 60 : width - padding - 60
      const y = padding + (row / Math.max(1, totalRows - 1)) * (height - 2 * padding)
      return {
        id: sys.id,
        name: sys.name,
        system: sys,
        x: count === 1 ? width / 2 : x,
        y: count === 1 ? height / 2 : y,
      }
    })
  }, [activeSystems])

  const nodeMap = useMemo(() => {
    const map = new Map<string, { id: string; name: string; x: number; y: number }>()
    graphNodes.forEach((n) => map.set(n.id, n))
    return map
  }, [graphNodes])

  return (
    <div>
      {/* Editorial Page Header */}
      <div className="ed-page-header">
        <div>
          <div className="ed-section-tag">
            <span>MODULE 03</span>
            <span style={{ opacity: 0.3 }}>/</span>
            <span>RELATIONSHIP TOPOLOGY &amp; GRAPH ENGINE</span>
          </div>
          <h1 className="ed-page-title">Dependency Relationship Graph</h1>
          <p className="ed-page-subtitle">
            Directed graph topology linking upstream provider components to reliant logistics services. Primary data source for blast radius traversal.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <div style={{ display: 'inline-flex', border: '1px solid var(--ed-border)' }}>
            <button
              type="button"
              className={`ed-button ed-button-sm ${viewMode === 'graph' ? 'ed-button-primary' : ''}`}
              style={{ border: 'none' }}
              onClick={() => setViewMode('graph')}
            >
              [ HERO GRAPH ]
            </button>
            <button
              type="button"
              className={`ed-button ed-button-sm ${viewMode === 'table' ? 'ed-button-primary' : ''}`}
              style={{ border: 'none' }}
              onClick={() => setViewMode('table')}
            >
              [ TABULAR VIEW ]
            </button>
          </div>

          <button
            type="button"
            className="ed-button ed-button-primary"
            onClick={handleOpenAdd}
            disabled={activeSystems.length < 2}
            title={activeSystems.length < 2 ? 'At least 2 active systems required' : undefined}
          >
            [ + MAP DEPENDENCY ]
          </button>
        </div>
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

      {/* Unobtrusive Minimal Filter Strip */}
      <div className="ed-filter-strip">
        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
          <input
            type="text"
            className="ed-search-input"
            placeholder="SEARCH DEPENDENCIES..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />

          <select
            className="ed-select"
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
          >
            <option value="all">ALL CLASSIFICATIONS</option>
            {Object.entries(dependencyTypeLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label.toUpperCase()}
              </option>
            ))}
          </select>
        </div>

        <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
          EDGES: <strong style={{ color: 'var(--ed-text-primary)' }}>{String(filteredDependencies.length).padStart(3, '0')}</strong> / {String(dependencies.length).padStart(3, '0')}
        </div>
      </div>

      {loading ? (
        <LoadingSpinner label="COMPUTING DEPENDENCY EDGES..." />
      ) : filteredDependencies.length === 0 && dependencies.length === 0 ? (
        <div className="cc-empty-state">
          <div className="cc-empty-title">00 // ZERO DEPENDENCY EDGES MAPPED</div>
          <p className="cc-empty-desc">
            {activeSystems.length < 2
              ? 'At least two active logistics systems must be registered before dependency linkages can be established.'
              : 'Define architectural dependency linkages between systems to enable Graph Engine traversal and blast radius cascade scoring.'}
          </p>
          {activeSystems.length >= 2 && (
            <button
              type="button"
              className="ed-button ed-button-primary"
              onClick={handleOpenAdd}
            >
              [ + MAP FIRST DEPENDENCY ]
            </button>
          )}
        </div>
      ) : viewMode === 'graph' ? (
        /* HERO DEPENDENCY GRAPH + CONTEXTUAL SIDE PANEL (ASYMMETRIC 68 / 32 SPLIT) */
        <div className="ed-asym-65-35">
          {/* Left Column: Primary Visual Hero Graph */}
          <div>
            <div
              style={{
                backgroundColor: 'var(--ed-bg-surface)',
                border: '1px solid var(--ed-border)',
                position: 'relative',
                overflow: 'hidden',
                minHeight: '440px',
                display: 'flex',
                flexDirection: 'column',
              }}
            >
              {/* Graph Header Monospace Strip */}
              <div
                style={{
                  padding: '12px 18px',
                  borderBottom: '1px solid var(--ed-border)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  fontFamily: 'var(--ed-font-mono)',
                  fontSize: '10px',
                  color: 'var(--ed-text-muted)',
                  letterSpacing: '0.1em',
                }}
              >
                <span>TOPOLOGICAL GRAPH VIEW // DIRECTED EDGES</span>
                <div style={{ display: 'flex', gap: '14px' }}>
                  <span style={{ color: 'var(--ed-amber-text)' }}>● MANDATORY (STRICT)</span>
                  <span style={{ color: 'var(--ed-teal-text)' }}>● OPTIONAL (FAILOVER)</span>
                </div>
              </div>

              {/* Interactive SVG Canvas */}
              <div style={{ flex: 1, position: 'relative', minHeight: '380px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <svg
                  width="100%"
                  height="400"
                  viewBox="0 0 680 400"
                  style={{ background: 'radial-gradient(circle, rgba(237,231,220,0.02) 1px, transparent 1px)', backgroundSize: '24px 24px' }}
                >
                  <defs>
                    <marker
                      id="arrow-mandatory"
                      viewBox="0 0 10 10"
                      refX="18"
                      refY="5"
                      markerWidth="6"
                      markerHeight="6"
                      orient="auto-start-reverse"
                    >
                      <path d="M 0 0 L 10 5 L 0 10 z" fill="#E8913C" />
                    </marker>
                    <marker
                      id="arrow-optional"
                      viewBox="0 0 10 10"
                      refX="18"
                      refY="5"
                      markerWidth="6"
                      markerHeight="6"
                      orient="auto-start-reverse"
                    >
                      <path d="M 0 0 L 10 5 L 0 10 z" fill="#2E6B72" />
                    </marker>
                  </defs>

                  {/* Draw Directed Edges */}
                  {filteredDependencies.map((dep) => {
                    const src = nodeMap.get(dep.dependencySystemId)
                    const dst = nodeMap.get(dep.dependentSystemId)
                    if (!src || !dst) return null
                    const isSelected = selectedDepId === dep.id
                    const isMandatory = dep.dependencyType === DependencyType.Mandatory
                    const strokeColor = isSelected ? '#FFFFFF' : isMandatory ? '#E8913C' : '#2E6B72'
                    const strokeWidth = isSelected ? 2.5 : 1.5

                    // Calculate curve control point
                    const midX = (src.x + dst.x) / 2
                    const midY = (src.y + dst.y) / 2 - 20

                    return (
                      <g key={dep.id} style={{ cursor: 'pointer' }} onClick={() => { setSelectedDepId(dep.id); setSelectedSystemId(null) }}>
                        <path
                          d={`M ${src.x} ${src.y} Q ${midX} ${midY} ${dst.x} ${dst.y}`}
                          fill="none"
                          stroke={strokeColor}
                          strokeWidth={strokeWidth}
                          strokeDasharray={isMandatory ? 'none' : '4 3'}
                          markerEnd={isMandatory ? 'url(#arrow-mandatory)' : 'url(#arrow-optional)'}
                          opacity={isSelected ? 1 : 0.75}
                        />
                      </g>
                    )
                  })}

                  {/* Draw Nodes */}
                  {graphNodes.map((n) => {
                    const isSelected = selectedSystemId === n.id
                    return (
                      <g
                        key={n.id}
                        style={{ cursor: 'pointer' }}
                        onClick={() => {
                          setSelectedSystemId(n.id)
                          setSelectedDepId(null)
                        }}
                      >
                        <circle
                          cx={n.x}
                          cy={n.y}
                          r={isSelected ? 10 : 7}
                          fill={isSelected ? 'var(--ed-amber)' : 'var(--ed-bg-elevated)'}
                          stroke={isSelected ? '#FFFFFF' : 'var(--ed-border-strong)'}
                          strokeWidth={isSelected ? 2 : 1}
                        />
                        <text
                          x={n.x}
                          y={n.y - 14}
                          textAnchor="middle"
                          fill={isSelected ? 'var(--ed-amber)' : 'var(--ed-text-primary)'}
                          fontSize="10"
                          fontFamily="var(--ed-font-mono)"
                          fontWeight="700"
                        >
                          {n.name}
                        </text>
                      </g>
                    )
                  })}
                </svg>
              </div>

              {/* Graph Footer Caption */}
              <div
                style={{
                  padding: '10px 18px',
                  borderTop: '1px solid var(--ed-border)',
                  fontFamily: 'var(--ed-font-mono)',
                  fontSize: '10px',
                  color: 'var(--ed-text-muted)',
                  display: 'flex',
                  justifyContent: 'space-between',
                }}
              >
                <span>TIP: CLICK ANY EDGE OR NODE TO INSPECT DETAILS</span>
                <span>NODES: {graphNodes.length} / EDGES: {filteredDependencies.length}</span>
              </div>
            </div>
          </div>

          {/* Right Column: Contextual Side Panel */}
          <div className="ed-context-panel">
            {selectedDep ? (
              <>
                <div className="ed-context-panel-title">
                  <span>EDGE INSPECTION //</span>
                  <StatusBadge type="dependencyType" value={selectedDep.dependencyType} />
                </div>

                <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', lineHeight: 1.8 }}>
                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>EDGE ID: </span>
                    <strong style={{ color: 'var(--ed-text-primary)' }}>#{selectedDep.id.substring(0, 8)}</strong>
                  </div>

                  <div style={{ margin: '12px 0', padding: '12px', background: 'var(--ed-bg-elevated)', border: '1px solid var(--ed-border)' }}>
                    <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', marginBottom: '4px' }}>UPSTREAM PROVIDER (SOURCE)</div>
                    <div style={{ fontSize: '14px', fontWeight: 800, color: 'var(--ed-amber)' }}>
                      {selectedDep.dependencySystem?.name || selectedDep.dependencySystemId}
                    </div>
                    <div style={{ margin: '8px 0', textAlign: 'center', color: 'var(--ed-text-muted)', fontSize: '12px' }}>
                      ───[ DIRECTED CASUAL VECTOR ]───▶
                    </div>
                    <div style={{ fontSize: '10px', color: 'var(--ed-text-muted)', marginBottom: '4px' }}>DOWNSTREAM RELIANT (DESTINATION)</div>
                    <div style={{ fontSize: '14px', fontWeight: 800, color: 'var(--ed-text-primary)' }}>
                      {selectedDep.dependentSystem?.name || selectedDep.dependentSystemId}
                    </div>
                  </div>

                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>ARCHITECTURAL RULE: </span>
                    <strong>
                      {selectedDep.dependencyType === DependencyType.Mandatory
                        ? 'Strict Prerequisite (Outage cascades immediately)'
                        : 'Optional Redundancy (Failover tolerated)'}
                    </strong>
                  </div>

                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>ESTABLISHED: </span>
                    <span>{new Date(selectedDep.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>

                <div className="ed-rule-subtle" style={{ margin: '14px 0' }} />

                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <button
                    type="button"
                    className="ed-button ed-button-primary"
                    onClick={() => onNavigate('ImpactAnalysis', selectedDep.dependencySystemId)}
                    style={{ width: '100%' }}
                  >
                    [ SIMULATE PROVIDER OUTAGE → ]
                  </button>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                    <button
                      type="button"
                      className="ed-button"
                      onClick={() => handleOpenEdit(selectedDep)}
                    >
                      [ EDIT EDGE ]
                    </button>
                    <button
                      type="button"
                      className="ed-button ed-button-danger"
                      onClick={() => setDeletingId(selectedDep.id)}
                    >
                      [ DELETE ]
                    </button>
                  </div>
                </div>
              </>
            ) : selectedSystem ? (
              <>
                <div className="ed-context-panel-title">
                  <span>NODE INSPECTION //</span>
                  <StatusBadge type="systemCriticality" value={selectedSystem.criticality} />
                </div>

                <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', lineHeight: 1.8 }}>
                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>SYSTEM: </span>
                    <strong style={{ fontSize: '15px', color: 'var(--ed-text-primary)' }}>{selectedSystem.name}</strong>
                  </div>
                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>ENVIRONMENT: </span>
                    <span>{systemEnvironmentLabels[selectedSystem.environment]}</span>
                  </div>
                  <div>
                    <span style={{ color: 'var(--ed-text-muted)' }}>STATUS: </span>
                    <StatusBadge type="systemStatus" value={selectedSystem.lifecycleStatus} />
                  </div>
                </div>

                <div className="ed-rule-subtle" style={{ margin: '14px 0' }} />

                <button
                  type="button"
                  className="ed-button ed-button-primary"
                  onClick={() => onNavigate('ImpactAnalysis', selectedSystem.id)}
                  style={{ width: '100%' }}
                >
                  [ SIMULATE BLAST RADIUS → ]
                </button>
              </>
            ) : (
              <div style={{ color: 'var(--ed-text-muted)', textAlign: 'center', padding: '36px 0' }}>
                Select a dependency edge or node in the graph to inspect architectural relationships.
              </div>
            )}
          </div>
        </div>
      ) : (
        /* TABULAR BULK DIRECTORY VIEW */
        <div className="ed-table-container">
          <table className="ed-table">
            <thead>
              <tr>
                <th style={{ width: '44px' }}>#</th>
                <th>DOWNSTREAM RELIANT (DESTINATION)</th>
                <th style={{ textAlign: 'center' }}>VECTOR</th>
                <th>UPSTREAM PROVIDER (SOURCE)</th>
                <th>CLASSIFICATION</th>
                <th>ESTABLISHED</th>
                <th style={{ textAlign: 'right' }}>COMMAND ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredDependencies.map((dep, idx) => (
                <tr key={dep.id}>
                  <td className="ed-row-idx">{String(idx + 1).padStart(3, '0')}</td>
                  <td>
                    <div style={{ fontWeight: 700 }}>
                      {dep.dependentSystem?.name || dep.dependentSystemId}
                    </div>
                    {dep.dependentSystem && (
                      <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        ID: #{dep.dependentSystem.id.substring(0, 8)}
                      </div>
                    )}
                  </td>
                  <td style={{ textAlign: 'center', color: dep.dependencyType === DependencyType.Mandatory ? 'var(--ed-amber)' : 'var(--ed-teal-text)', fontFamily: 'var(--ed-font-mono)' }}>
                    ──▶
                  </td>
                  <td>
                    <div style={{ fontWeight: 700 }}>
                      {dep.dependencySystem?.name || dep.dependencySystemId}
                    </div>
                    {dep.dependencySystem && (
                      <div style={{ fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                        ID: #{dep.dependencySystem.id.substring(0, 8)}
                      </div>
                    )}
                  </td>
                  <td>
                    <StatusBadge type="dependencyType" value={dep.dependencyType} />
                  </td>
                  <td style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                    {new Date(dep.createdAt).toLocaleDateString()}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <div style={{ display: 'inline-flex', gap: '6px' }}>
                      <button
                        type="button"
                        className="ed-button ed-button-sm ed-button-secondary"
                        onClick={() => onNavigate('ImpactAnalysis', dep.dependencySystemId)}
                        title="Simulate blast radius"
                      >
                        [ BLAST RADIUS → ]
                      </button>
                      <button
                        type="button"
                        className="ed-button ed-button-sm"
                        onClick={() => handleOpenEdit(dep)}
                      >
                        [ EDIT ]
                      </button>
                      <button
                        type="button"
                        className="ed-button ed-button-sm ed-button-danger"
                        onClick={() => setDeletingId(dep.id)}
                      >
                        [ DELETE ]
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Map Dependency Modal */}
      <Modal
        isOpen={isAddOpen}
        title="MAP DEPENDENCY RELATIONSHIP"
        onClose={() => setIsAddOpen(false)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setIsAddOpen(false)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-primary"
              disabled={submitting}
              onClick={handleAddSubmit}
            >
              {submitting ? 'COMMITTING...' : '[ MAP DEPENDENCY EDGE ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleAddSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">DOWNSTREAM RELIANT SYSTEM *</label>
            <select
              className="ed-select"
              value={formData.dependentSystemId}
              onChange={(e) => setFormData({ ...formData, dependentSystemId: e.target.value })}
              required
            >
              <option value="">-- SELECT RELIANT SYSTEM --</option>
              {activeSystems.map((sys) => (
                <option key={sys.id} value={sys.id}>
                  {sys.name} ({systemTypeLabels[sys.systemType]?.split(' ')[0]})
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">UPSTREAM PROVIDER SYSTEM (DEPENDENCY) *</label>
            <select
              className="ed-select"
              value={formData.dependencySystemId}
              onChange={(e) => setFormData({ ...formData, dependencySystemId: e.target.value })}
              required
            >
              <option value="">-- SELECT PROVIDER SYSTEM --</option>
              {activeSystems.map((sys) => (
                <option key={sys.id} value={sys.id}>
                  {sys.name} ({systemTypeLabels[sys.systemType]?.split(' ')[0]})
                </option>
              ))}
            </select>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">CLASSIFICATION TYPE *</label>
            <select
              className="ed-select"
              value={formData.dependencyType}
              onChange={(e) => setFormData({ ...formData, dependencyType: Number(e.target.value) as DependencyType })}
            >
              <option value={DependencyType.Mandatory}>MANDATORY (STRICT PREREQUISITE)</option>
              <option value={DependencyType.Optional}>OPTIONAL (FAILOVER PERMITTED)</option>
            </select>
            <span className="cc-form-hint">
              Mandatory dependencies cause cascading failures if the provider goes offline.
            </span>
          </div>
        </form>
      </Modal>

      {/* Edit Dependency Modal */}
      <Modal
        isOpen={Boolean(editingDep)}
        title="MODIFY DEPENDENCY CLASSIFICATION"
        onClose={() => setEditingDep(null)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setEditingDep(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-primary"
              disabled={submitting}
              onClick={handleEditSubmit}
            >
              {submitting ? 'SAVING...' : '[ SAVE CLASSIFICATION ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleEditSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {formError && <div className="cc-form-error">[VALIDATION ERROR] {formError}</div>}

          <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-secondary)', lineHeight: 1.6 }}>
            <div>RELIANT: <strong>{editingDep?.dependentSystem?.name}</strong></div>
            <div>PROVIDER: <strong>{editingDep?.dependencySystem?.name}</strong></div>
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">CLASSIFICATION TYPE *</label>
            <select
              className="ed-select"
              value={formData.dependencyType}
              onChange={(e) => setFormData({ ...formData, dependencyType: Number(e.target.value) as DependencyType })}
            >
              <option value={DependencyType.Mandatory}>MANDATORY (STRICT PREREQUISITE)</option>
              <option value={DependencyType.Optional}>OPTIONAL (FAILOVER PERMITTED)</option>
            </select>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={Boolean(deletingId)}
        title="REMOVE DEPENDENCY RELATIONSHIP"
        onClose={() => setDeletingId(null)}
        footer={
          <>
            <button
              type="button"
              className="ed-button"
              onClick={() => setDeletingId(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="ed-button ed-button-danger"
              disabled={submitting}
              onClick={handleDeleteConfirm}
            >
              {submitting ? 'REMOVING...' : '[ CONFIRM REMOVAL ]'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px', color: 'var(--ed-text-secondary)', lineHeight: 1.6 }}>
          Are you sure you want to remove this dependency linkage? Both underlying logistics systems will remain preserved in the registry; only the relationship edge will be removed from graph traversal.
        </p>
      </Modal>
    </div>
  )
}
