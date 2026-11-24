import React, { useEffect, useState, useMemo, useCallback } from 'react'
import { api } from '../services/api'
import type {
  Page,
  IamUserResponseDTO,
  CreateIamUserDTO,
} from '../types'
import { UserStatus } from '../types'
import type { AuthUser } from '../auth/session'
import Modal from '../components/Modal'
import { LoadingSpinner, ErrorBanner, EmptyState } from '../components/CommonStates'

interface IamManagementPageProps {
  user: AuthUser
  onNavigate: (page: Page) => void
}

export default function IamManagementPage({ user, onNavigate }: IamManagementPageProps) {
  const [iamUsers, setIamUsers] = useState<IamUserResponseDTO[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  // Filters
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<'ALL' | 'ACTIVE' | 'INACTIVE'>('ALL')

  // Provision Modal State
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [createForm, setCreateForm] = useState<CreateIamUserDTO>({
    username: '',
    email: '',
    password: '',
  })
  const [createError, setCreateError] = useState<string | null>(null)
  const [createSubmitting, setCreateSubmitting] = useState(false)

  // Status Toggle Modal State
  const [statusTargetUser, setStatusTargetUser] = useState<IamUserResponseDTO | null>(null)
  const [statusSubmitting, setStatusSubmitting] = useState(false)

  // Reset Password Modal State
  const [resetTargetUser, setResetTargetUser] = useState<IamUserResponseDTO | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [resetError, setResetError] = useState<string | null>(null)
  const [resetSubmitting, setResetSubmitting] = useState(false)

  const isRoot = user.accountType === 'Root'

  const loadUsers = useCallback(async () => {
    if (!isRoot) return
    try {
      setLoading(true)
      const data = await api.getIamUsers()
      setIamUsers(data)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to retrieve IAM workforce directory.')
    } finally {
      setLoading(false)
    }
  }, [isRoot])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  // Filtered list
  const filteredUsers = useMemo(() => {
    return iamUsers.filter((u) => {
      const matchSearch =
        u.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.email.toLowerCase().includes(searchTerm.toLowerCase())

      const matchStatus =
        statusFilter === 'ALL' ||
        (statusFilter === 'ACTIVE' && u.status === UserStatus.Active) ||
        (statusFilter === 'INACTIVE' && u.status === UserStatus.Inactive)

      return matchSearch && matchStatus
    })
  }, [iamUsers, searchTerm, statusFilter])

  // Metrics
  const activeCount = useMemo(() => iamUsers.filter((u) => u.status === UserStatus.Active).length, [iamUsers])
  const inactiveCount = useMemo(() => iamUsers.filter((u) => u.status === UserStatus.Inactive).length, [iamUsers])

  // Handle Provisioning
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setCreateError(null)

    if (!createForm.username.trim() || createForm.username.trim().length < 2) {
      setCreateError('Username must be at least 2 characters.')
      return
    }

    if (!createForm.email.trim() || !createForm.email.includes('@')) {
      setCreateError('A valid corporate email address is required.')
      return
    }

    if (!createForm.password || createForm.password.length < 8) {
      setCreateError('Password must contain at least 8 characters.')
      return
    }

    try {
      setCreateSubmitting(true)
      const created = await api.createIamUser({
        username: createForm.username.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
      })

      setIamUsers((prev) => [...prev, created])
      setIsCreateOpen(false)
      setCreateForm({ username: '', email: '', password: '' })
      setSuccessMessage(`IAM User '${created.username}' provisioned successfully under organization workspace.`)
      setTimeout(() => setSuccessMessage(null), 6000)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to provision IAM user.')
    } finally {
      setCreateSubmitting(false)
    }
  }

  // Handle Status Toggle (Activate / Deactivate)
  const handleStatusToggle = async () => {
    if (!statusTargetUser) return
    const nextStatus = statusTargetUser.status === UserStatus.Active ? UserStatus.Inactive : UserStatus.Active

    try {
      setStatusSubmitting(true)
      const updated = await api.updateIamUserStatus(statusTargetUser.id, nextStatus)

      setIamUsers((prev) => prev.map((u) => (u.id === updated.id ? updated : u)))
      setStatusTargetUser(null)
      const actionName = nextStatus === UserStatus.Active ? 'activated' : 'deactivated'
      setSuccessMessage(
        `User '${updated.username}' successfully ${actionName}. ${
          nextStatus === UserStatus.Inactive ? 'Active login sessions have been invalidated immediately.' : ''
        }`
      )
      setTimeout(() => setSuccessMessage(null), 6000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update user status.')
    } finally {
      setStatusSubmitting(false)
    }
  }

  // Handle Reset Password
  const handleResetPasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!resetTargetUser) return
    setResetError(null)

    if (!newPassword || newPassword.length < 8) {
      setResetError('New password must contain at least 8 characters.')
      return
    }

    try {
      setResetSubmitting(true)
      await api.resetIamUserPassword(resetTargetUser.id, { newPassword })
      const targetName = resetTargetUser.username
      setResetTargetUser(null)
      setNewPassword('')
      setSuccessMessage(
        `Password for '${targetName}' has been reset. Existing session tokens have been invalidated immediately.`
      )
      setTimeout(() => setSuccessMessage(null), 6000)
    } catch (err) {
      setResetError(err instanceof Error ? err.message : 'Failed to reset password.')
    } finally {
      setResetSubmitting(false)
    }
  }

  if (!isRoot) {
    return (
      <div className="cc-empty-state" style={{ padding: '60px 20px', textAlign: 'center' }}>
        <div style={{ color: '#ef4444', fontFamily: 'var(--cc-font-mono)', fontSize: '16px', fontWeight: 700, marginBottom: '12px' }}>
          [ACCESS DENIED // 403 FORBIDDEN]
        </div>
        <div className="cc-empty-title">// ROOT PRIVILEGES REQUIRED</div>
        <p className="cc-empty-desc" style={{ maxWidth: '480px', margin: '0 auto 24px' }}>
          Identity & Access Management is restricted exclusively to Organization Root Accounts. Your authenticated
          credential is an IAM User account.
        </p>
        <button
          type="button"
          className="cc-button cc-button-primary"
          onClick={() => onNavigate('Overview')}
        >
          [ RETURN TO COMMAND OVERVIEW ]
        </button>
      </div>
    )
  }

  return (
    <div className="cc-page">
      {/* Page Header */}
      <div className="cc-page-header">
        <div>
          <div className="cc-section-tag">// MODULE 07 — ACCESS GOVERNANCE</div>
          <h1 className="cc-page-title">Identity & Access Management (IAM)</h1>
          <p className="cc-page-subtitle">
            Provision, monitor, and revoke workforce IAM accounts within your organization tenant workspace.
          </p>
        </div>

        <div className="cc-header-actions">
          <button
            type="button"
            className="cc-button cc-button-primary"
            onClick={() => {
              setCreateForm({ username: '', email: '', password: '' })
              setCreateError(null)
              setIsCreateOpen(true)
            }}
          >
            [ + PROVISION IAM USER ]
          </button>
        </div>
      </div>

      {/* Root Organization Context Banner */}
      <div
        className="cc-card"
        style={{
          marginBottom: '20px',
          background: 'linear-gradient(90deg, rgba(239,68,68,0.06) 0%, rgba(18,18,18,0.95) 100%)',
          borderLeft: '3px solid #ef4444',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '16px',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: '32px',
              height: '32px',
              borderRadius: '4px',
              background: 'rgba(239,68,68,0.15)',
              color: '#ef4444',
              fontFamily: 'var(--cc-font-mono)',
              fontWeight: 800,
              fontSize: '12px',
            }}
          >
            ROOT
          </span>
          <div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#aaaaaa', letterSpacing: '0.05em' }}>
              AUTHENTICATED ROOT AUTHORITY
            </div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '13px', color: '#ffffff', fontWeight: 600 }}>
              {user.name} ({user.email})
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '24px', alignItems: 'center' }}>
          <div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>TENANT SCOPE</div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#cccccc' }}>
              {user.tenantId ? user.tenantId : 'Organization Workspace'}
            </div>
          </div>
          <div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>SECURITY PROTOCOL</div>
            <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: 'var(--cc-green)' }}>
              STRICT SEC_STAMP VERIFIED
            </div>
          </div>
        </div>
      </div>

      {/* Metrics Summary Grid */}
      <div className="cc-metrics-grid" style={{ marginBottom: '24px' }}>
        <div className="cc-metric-card">
          <div className="cc-metric-header">
            <span className="cc-metric-label">TOTAL WORKFORCE</span>
            <span className="cc-metric-sublabel">IAM DIRECTORY</span>
          </div>
          <div className="cc-metric-value">{iamUsers.length}</div>
          <div className="cc-metric-detail">Assigned to this tenant</div>
        </div>

        <div className="cc-metric-card">
          <div className="cc-metric-header">
            <span className="cc-metric-label">ACTIVE OPERATORS</span>
            <span className="cc-metric-sublabel">AUTHENTICATED</span>
          </div>
          <div className="cc-metric-value" style={{ color: 'var(--cc-green)' }}>
            {activeCount}
          </div>
          <div className="cc-metric-detail">Permitted to sign in</div>
        </div>

        <div className="cc-metric-card">
          <div className="cc-metric-header">
            <span className="cc-metric-label">INACTIVE / DISABLED</span>
            <span className="cc-metric-sublabel">BLOCKED</span>
          </div>
          <div className="cc-metric-value" style={{ color: inactiveCount > 0 ? '#ef4444' : '#888888' }}>
            {inactiveCount}
          </div>
          <div className="cc-metric-detail">Tokens revoked immediately</div>
        </div>

        <div className="cc-metric-card">
          <div className="cc-metric-header">
            <span className="cc-metric-label">IAM AUTHORIZATION</span>
            <span className="cc-metric-sublabel">POLICY MATRIX</span>
          </div>
          <div className="cc-metric-value" style={{ fontSize: '18px', color: '#60a5fa', paddingTop: '4px' }}>
            RootOnly Policy
          </div>
          <div className="cc-metric-detail">Strict 403 enforcement</div>
        </div>
      </div>

      {/* Alerts & Notifications */}
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
      {successMessage && (
        <div
          className="cc-card"
          style={{
            marginBottom: '16px',
            background: 'rgba(34,197,94,0.08)',
            borderLeft: '3px solid var(--cc-green)',
            padding: '12px 16px',
            fontFamily: 'var(--cc-font-mono)',
            fontSize: '12px',
            color: '#4ade80',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <span>[SYSTEM // SUCCESS] {successMessage}</span>
          <button
            type="button"
            className="cc-button cc-button-sm"
            onClick={() => setSuccessMessage(null)}
            style={{ padding: '2px 6px', fontSize: '10px' }}
          >
            DISMISS
          </button>
        </div>
      )}

      {/* Filter and Search Bar */}
      <div className="cc-filter-bar" style={{ marginBottom: '16px' }}>
        <div className="cc-search-box">
          <span className="cc-search-icon">🔍</span>
          <input
            type="text"
            className="cc-input"
            placeholder="Search by username or email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>

        <div className="cc-filter-group">
          <label className="cc-filter-label" htmlFor="iam-status-filter">STATUS:</label>
          <select
            id="iam-status-filter"
            className="cc-select"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as 'ALL' | 'ACTIVE' | 'INACTIVE')}
          >
            <option value="ALL">All Statuses ({iamUsers.length})</option>
            <option value="ACTIVE">Active ({activeCount})</option>
            <option value="INACTIVE">Inactive ({inactiveCount})</option>
          </select>
        </div>

        <button
          type="button"
          className="cc-button cc-button-sm"
          onClick={loadUsers}
          title="Reload workforce directory"
        >
          [ REFRESH ]
        </button>
      </div>

      {/* Data Table */}
      {loading ? (
        <LoadingSpinner label="LOADING IAM WORKFORCE DIRECTORY..." />
      ) : filteredUsers.length === 0 ? (
        <EmptyState
          title="NO IAM USERS REGISTERED"
          description={
            searchTerm || statusFilter !== 'ALL'
              ? 'No IAM users match your active filter criteria.'
              : 'No IAM accounts have been provisioned under this organization tenant workspace yet.'
          }
          action={
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={() => {
                setCreateForm({ username: '', email: '', password: '' })
                setCreateError(null)
                setIsCreateOpen(true)
              }}
            >
              [ + PROVISION FIRST IAM USER ]
            </button>
          }
        />
      ) : (
        <div className="cc-table-container">
          <table className="cc-table">
            <thead>
              <tr>
                <th>USER IDENTIFIER</th>
                <th>CORPORATE EMAIL</th>
                <th>ACCOUNT TYPE</th>
                <th>STATUS</th>
                <th>PROVISIONED DATE</th>
                <th style={{ textAlign: 'right' }}>ACTIONS</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((item) => {
                const isActive = item.status === UserStatus.Active
                return (
                  <tr key={item.id}>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span
                          style={{
                            display: 'inline-block',
                            width: '24px',
                            height: '24px',
                            lineHeight: '24px',
                            textAlign: 'center',
                            borderRadius: '3px',
                            background: '#222222',
                            color: '#60a5fa',
                            fontFamily: 'var(--cc-font-mono)',
                            fontSize: '10px',
                            fontWeight: 700,
                          }}
                        >
                          {item.username.slice(0, 2).toUpperCase()}
                        </span>
                        <div>
                          <div style={{ fontWeight: 600, color: '#ffffff' }}>{item.username}</div>
                          <div style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>
                            ID: #{item.id}
                          </div>
                        </div>
                      </div>
                    </td>

                    <td>
                      <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '12px', color: '#cccccc' }}>
                        {item.email}
                      </span>
                    </td>

                    <td>
                      <span
                        style={{
                          display: 'inline-block',
                          padding: '2px 8px',
                          borderRadius: '3px',
                          fontSize: '10px',
                          fontWeight: 700,
                          fontFamily: 'var(--cc-font-mono)',
                          letterSpacing: '0.05em',
                          background: 'rgba(59, 130, 246, 0.15)',
                          color: '#60a5fa',
                          border: '1px solid rgba(59, 130, 246, 0.3)',
                        }}
                      >
                        IAM USER
                      </span>
                    </td>

                    <td>
                      <span className={`cc-badge ${isActive ? 'cc-badge-green' : 'cc-badge-neutral'}`}>
                        <span
                          className="cc-pulse-dot"
                          style={{ backgroundColor: isActive ? 'var(--cc-green)' : '#ef4444' }}
                        />
                        {isActive ? 'ACTIVE' : 'INACTIVE'}
                      </span>
                    </td>

                    <td>
                      <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '11px', color: '#888888' }}>
                        {new Date(item.createdAt).toLocaleDateString()}{' '}
                        {new Date(item.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </td>

                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '8px' }}>
                        <button
                          type="button"
                          className={`cc-button cc-button-sm ${isActive ? 'cc-button-danger' : 'cc-button-primary'}`}
                          onClick={() => setStatusTargetUser(item)}
                          title={isActive ? 'Deactivate user' : 'Reactivate user'}
                        >
                          {isActive ? 'DEACTIVATE' : 'ACTIVATE'}
                        </button>

                        <button
                          type="button"
                          className="cc-button cc-button-sm"
                          onClick={() => {
                            setResetTargetUser(item)
                            setNewPassword('')
                            setResetError(null)
                          }}
                          title="Reset user credentials"
                        >
                          RESET PWD
                        </button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* ========================================================= */}
      {/* MODAL 1: PROVISION IAM USER                              */}
      {/* ========================================================= */}
      <Modal
        isOpen={isCreateOpen}
        title="PROVISION NEW IAM WORKFORCE ACCOUNT"
        onClose={() => setIsCreateOpen(false)}
        footer={
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <button
              type="button"
              className="cc-button"
              onClick={() => setIsCreateOpen(false)}
              disabled={createSubmitting}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={handleCreateSubmit}
              disabled={createSubmitting}
            >
              {createSubmitting ? 'PROVISIONING...' : '[ PROVISION ACCOUNT ]'}
            </button>
          </div>
        }
      >
        <form onSubmit={handleCreateSubmit}>
          <p style={{ fontSize: '12px', color: '#888888', marginBottom: '16px', lineHeight: 1.5 }}>
            Provision an independently authenticated IAM workforce user. The account will inherit this organization's
            Tenant ID and receive operational access to modules without Root governance privileges.
          </p>

          {createError && <ErrorBanner message={createError} onDismiss={() => setCreateError(null)} />}

          <div className="cc-form-group" style={{ marginBottom: '14px' }}>
            <label className="cc-form-label" htmlFor="iam-username">OPERATOR USERNAME</label>
            <input
              id="iam-username"
              type="text"
              className="cc-input"
              placeholder="e.g., dispatch_john"
              value={createForm.username}
              onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
              required
              autoFocus
            />
          </div>

          <div className="cc-form-group" style={{ marginBottom: '14px' }}>
            <label className="cc-form-label" htmlFor="iam-email">CORPORATE EMAIL ADDRESS</label>
            <input
              id="iam-email"
              type="email"
              className="cc-input"
              placeholder="e.g., john.doe@carrier.com"
              value={createForm.email}
              onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
              required
            />
            <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>
              Must be unique globally across the entire platform.
            </span>
          </div>

          <div className="cc-form-group" style={{ marginBottom: '14px' }}>
            <label className="cc-form-label" htmlFor="iam-password">INITIAL TEMPORARY PASSWORD</label>
            <input
              id="iam-password"
              type="password"
              className="cc-input"
              placeholder="Minimum 8 characters"
              value={createForm.password}
              onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
              required
            />
            <span style={{ fontFamily: 'var(--cc-font-mono)', fontSize: '10px', color: '#666666' }}>
              Cryptographically hashed via PBKDF2 before storage. Plaintext credentials are never saved.
            </span>
          </div>
        </form>
      </Modal>

      {/* ========================================================= */}
      {/* MODAL 2: CONFIRM STATUS TOGGLE                           */}
      {/* ========================================================= */}
      <Modal
        isOpen={statusTargetUser !== null}
        title={
          statusTargetUser?.status === UserStatus.Active
            ? 'CONFIRM ACCOUNT DEACTIVATION'
            : 'CONFIRM ACCOUNT REACTIVATION'
        }
        onClose={() => setStatusTargetUser(null)}
        footer={
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <button
              type="button"
              className="cc-button"
              onClick={() => setStatusTargetUser(null)}
              disabled={statusSubmitting}
            >
              CANCEL
            </button>
            <button
              type="button"
              className={`cc-button ${
                statusTargetUser?.status === UserStatus.Active ? 'cc-button-danger' : 'cc-button-primary'
              }`}
              onClick={handleStatusToggle}
              disabled={statusSubmitting}
            >
              {statusSubmitting
                ? 'UPDATING...'
                : statusTargetUser?.status === UserStatus.Active
                ? '[ DEACTIVATE USER ]'
                : '[ REACTIVATE USER ]'}
            </button>
          </div>
        }
      >
        <div>
          <p style={{ fontSize: '13px', color: '#cccccc', marginBottom: '14px', lineHeight: 1.6 }}>
            {statusTargetUser?.status === UserStatus.Active ? (
              <>
                Are you sure you want to <strong>DEACTIVATE</strong> user{' '}
                <span style={{ color: '#ffffff', fontWeight: 700 }}>{statusTargetUser?.username}</span> (
                {statusTargetUser?.email})?
              </>
            ) : (
              <>
                Are you sure you want to <strong>REACTIVATE</strong> user{' '}
                <span style={{ color: '#ffffff', fontWeight: 700 }}>{statusTargetUser?.username}</span> (
                {statusTargetUser?.email})?
              </>
            )}
          </p>

          {statusTargetUser?.status === UserStatus.Active && (
            <div
              style={{
                background: 'rgba(239,68,68,0.1)',
                border: '1px solid rgba(239,68,68,0.3)',
                padding: '12px',
                borderRadius: '4px',
                fontFamily: 'var(--cc-font-mono)',
                fontSize: '11px',
                color: '#f87171',
                lineHeight: 1.5,
              }}
            >
              [SECURITY ADVISORY] Deactivating this user will regenerate their internal security stamp immediately.
              All active login sessions and JWT tokens will fail server-side token validation and be revoked across
              all active connections.
            </div>
          )}
        </div>
      </Modal>

      {/* ========================================================= */}
      {/* MODAL 3: RESET PASSWORD                                  */}
      {/* ========================================================= */}
      <Modal
        isOpen={resetTargetUser !== null}
        title={`RESET CREDENTIALS // ${resetTargetUser?.username.toUpperCase()}`}
        onClose={() => setResetTargetUser(null)}
        footer={
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <button
              type="button"
              className="cc-button"
              onClick={() => setResetTargetUser(null)}
              disabled={resetSubmitting}
            >
              CANCEL
            </button>
            <button
              type="button"
              className="cc-button cc-button-primary"
              onClick={handleResetPasswordSubmit}
              disabled={resetSubmitting}
            >
              {resetSubmitting ? 'RESETTING...' : '[ SET NEW PASSWORD ]'}
            </button>
          </div>
        }
      >
        <form onSubmit={handleResetPasswordSubmit}>
          <p style={{ fontSize: '13px', color: '#cccccc', marginBottom: '14px', lineHeight: 1.5 }}>
            Assign a new secure password for{' '}
            <strong style={{ color: '#ffffff' }}>{resetTargetUser?.username}</strong> ({resetTargetUser?.email}).
          </p>

          {resetError && <ErrorBanner message={resetError} onDismiss={() => setResetError(null)} />}

          <div className="cc-form-group" style={{ marginBottom: '14px' }}>
            <label className="cc-form-label" htmlFor="iam-reset-newpassword">NEW PASSWORD</label>
            <input
              id="iam-reset-newpassword"
              type="password"
              className="cc-input"
              placeholder="Minimum 8 characters"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              autoFocus
            />
          </div>

          <div
            style={{
              background: 'rgba(59,130,246,0.1)',
              border: '1px solid rgba(59,130,246,0.3)',
              padding: '10px',
              borderRadius: '4px',
              fontFamily: 'var(--cc-font-mono)',
              fontSize: '11px',
              color: '#93c5fd',
              lineHeight: 1.5,
            }}
          >
            [POLICY] Resetting credentials regenerates the security stamp. Any active session tokens will be
            invalidated immediately, forcing the operator to authenticate with the new password.
          </div>
        </form>
      </Modal>
    </div>
  )
}
