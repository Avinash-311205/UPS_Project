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
        <div style={{ color: 'var(--ed-amber)', fontFamily: 'var(--ed-font-mono)', fontSize: '14px', fontWeight: 700, marginBottom: '12px' }}>
          [ACCESS DENIED: 403 FORBIDDEN]
        </div>
        <div style={{ fontFamily: 'var(--ed-font-heading)', fontSize: '24px', fontWeight: 700, color: 'var(--ed-text)' }}>
          ROOT PRIVILEGES REQUIRED
        </div>
        <p style={{ maxWidth: '480px', margin: '12px auto 24px', color: 'var(--ed-text-secondary)', fontSize: '13px', lineHeight: 1.6 }}>
          Identity &amp; Access Management is restricted exclusively to Organization Root Accounts. Your authenticated
          credential is an IAM User account.
        </p>
        <button
          type="button"
          className="ed-action-link"
          onClick={() => onNavigate('Overview')}
        >
          [ RETURN TO COMMAND OVERVIEW ]
        </button>
      </div>
    )
  }

  return (
    <div>
      {/* Editorial Page Header */}
      <div className="ed-section-header">
        <div>
          <span className="ed-section-num">07 // ACCESS GOVERNANCE</span>
          <h1 className="ed-section-title">Identity &amp; Access Management</h1>
          <p className="ed-section-desc">
            Workforce credential lifecycle, security stamp revocation, and role-based access governance within this tenant partition.
          </p>
        </div>

        <button
          type="button"
          className="ed-action-link"
          style={{ fontSize: '11px', letterSpacing: '0.1em' }}
          onClick={() => {
            setCreateForm({ username: '', email: '', password: '' })
            setCreateError(null)
            setIsCreateOpen(true)
          }}
        >
          [ + PROVISION IAM USER ]
        </button>
      </div>

      {/* Root Authority Context Banner */}
      <div
        style={{
          border: '1px solid var(--ed-border)',
          borderLeft: '2px solid var(--ed-amber)',
          background: 'var(--ed-surface)',
          padding: '20px 24px',
          marginBottom: '32px',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: '20px',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: '4px 10px',
              border: '1px solid var(--ed-amber)',
              background: 'rgba(232, 145, 60, 0.12)',
              color: 'var(--ed-amber)',
              fontFamily: 'var(--ed-font-mono)',
              fontWeight: 800,
              fontSize: '11px',
              letterSpacing: '0.1em',
            }}
          >
            [ROOT]
          </span>
          <div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '10px', color: 'var(--ed-text-muted)', letterSpacing: '0.1em', textTransform: 'uppercase' }}>
              AUTHENTICATED ROOT AUTHORITY
            </div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '14px', color: 'var(--ed-text)', fontWeight: 600, marginTop: '2px' }}>
              {user.name} ({user.email})
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '32px', alignItems: 'center' }}>
          <div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '10px', color: 'var(--ed-text-muted)', letterSpacing: '0.08em' }}>TENANT SCOPE</div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text)' }}>
              {user.tenantId ? user.tenantId : 'Organization Workspace'}
            </div>
          </div>
          <div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '10px', color: 'var(--ed-text-muted)', letterSpacing: '0.08em' }}>SECURITY PROTOCOL</div>
            <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-teal-light)', fontWeight: 700 }}>
              STRICT SEC_STAMP VERIFIED
            </div>
          </div>
        </div>
      </div>

      {/* Editorial Metrics Strip */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '24px',
          borderTop: '1px solid var(--ed-border)',
          borderBottom: '1px solid var(--ed-border)',
          padding: '24px 0',
          marginBottom: '32px',
        }}
      >
        <div>
          <div className="ed-stat-label">TOTAL WORKFORCE</div>
          <div className="ed-stat-massive" style={{ margin: '8px 0' }}>{iamUsers.length}</div>
          <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
            Assigned to this tenant
          </div>
        </div>

        <div>
          <div className="ed-stat-label">ACTIVE OPERATORS</div>
          <div className="ed-stat-massive" style={{ color: 'var(--ed-teal-light)', margin: '8px 0' }}>
            {activeCount}
          </div>
          <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
            Permitted to sign in
          </div>
        </div>

        <div>
          <div className="ed-stat-label">INACTIVE / DISABLED</div>
          <div className="ed-stat-massive" style={{ color: inactiveCount > 0 ? 'var(--ed-amber)' : 'var(--ed-text-muted)', margin: '8px 0' }}>
            {inactiveCount}
          </div>
          <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
            Tokens revoked immediately
          </div>
        </div>

        <div>
          <div className="ed-stat-label">AUTHORIZATION POLICY</div>
          <div style={{ fontFamily: 'var(--ed-font-heading)', fontSize: '24px', fontWeight: 700, color: 'var(--ed-text)', marginTop: '12px', marginBottom: '8px' }}>
            RootOnly
          </div>
          <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-teal-light)' }}>
            Strict 403 enforcement
          </div>
        </div>
      </div>

      {/* Alerts */}
      {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
      {successMessage && (
        <div
          className="cc-badge cc-badge-green"
          style={{ width: '100%', padding: '12px 16px', marginBottom: '20px', boxSizing: 'border-box' }}
        >
          [SUCCESS] {successMessage}
        </div>
      )}

      {/* Editorial Filter Strip */}
      <div className="ed-filter-strip">
        <input
          type="text"
          className="ed-filter-input"
          placeholder="SEARCH USERNAME OR EMAIL..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />

        <select
          className="ed-filter-select"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as 'ALL' | 'ACTIVE' | 'INACTIVE')}
        >
          <option value="ALL">ALL STATUSES ({iamUsers.length})</option>
          <option value="ACTIVE">ACTIVE ({activeCount})</option>
          <option value="INACTIVE">INACTIVE ({inactiveCount})</option>
        </select>

        <button
          type="button"
          className="ed-action-link"
          style={{ fontSize: '11px' }}
          onClick={loadUsers}
        >
          [REFRESH DIRECTORY]
        </button>

        <div style={{ marginLeft: 'auto', fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
          WORKFORCE: <span style={{ color: 'var(--ed-text)', fontWeight: 700 }}>{filteredUsers.length}</span> / {iamUsers.length}
        </div>
      </div>

      {/* Workforce Directory */}
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
              className="ed-action-link"
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
        <div style={{ borderTop: '1px solid var(--ed-border)', marginBottom: '48px' }}>
          {filteredUsers.map((item, idx) => {
            const indexStr = String(idx + 1).padStart(3, '0')
            const isActive = item.status === UserStatus.Active

            return (
              <div
                key={item.id}
                className="ed-system-row"
                style={{
                  display: 'grid',
                  gridTemplateColumns: '50px 2fr 2fr 100px 100px 140px 200px',
                  alignItems: 'center',
                  gap: '16px',
                  padding: '16px 8px',
                  borderBottom: '1px solid var(--ed-border)',
                }}
              >
                {/* Index */}
                <div className="ed-system-index">{indexStr}</div>

                {/* Username & ID */}
                <div>
                  <div style={{ fontFamily: 'var(--ed-font-heading)', fontWeight: 600, fontSize: '15px', color: 'var(--ed-text)' }}>
                    {item.username}
                  </div>
                  <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)', marginTop: '2px' }}>
                    ID: #{item.id}
                  </div>
                </div>

                {/* Corporate Email */}
                <div>
                  <span style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '12px', color: 'var(--ed-text-secondary)' }}>
                    {item.email}
                  </span>
                </div>

                {/* Account Type */}
                <div>
                  <span
                    style={{
                      display: 'inline-block',
                      padding: '2px 8px',
                      fontSize: '10px',
                      fontWeight: 700,
                      fontFamily: 'var(--ed-font-mono)',
                      letterSpacing: '0.05em',
                      background: 'rgba(46, 107, 114, 0.15)',
                      color: 'var(--ed-teal-light)',
                      border: '1px solid rgba(46, 107, 114, 0.4)',
                    }}
                  >
                    IAM USER
                  </span>
                </div>

                {/* Status */}
                <div>
                  <span
                    style={{
                      display: 'inline-block',
                      padding: '2px 8px',
                      fontSize: '10px',
                      fontWeight: 700,
                      fontFamily: 'var(--ed-font-mono)',
                      letterSpacing: '0.05em',
                      background: isActive ? 'rgba(46, 107, 114, 0.2)' : 'rgba(232, 145, 60, 0.2)',
                      color: isActive ? 'var(--ed-teal-light)' : 'var(--ed-amber)',
                      border: isActive ? '1px solid rgba(46, 107, 114, 0.5)' : '1px solid rgba(232, 145, 60, 0.5)',
                    }}
                  >
                    {isActive ? 'ACTIVE' : 'INACTIVE'}
                  </span>
                </div>

                {/* Provisioned Date */}
                <div style={{ fontFamily: 'var(--ed-font-mono)', fontSize: '11px', color: 'var(--ed-text-muted)' }}>
                  {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : 'N/A'}
                </div>

                {/* Actions */}
                <div style={{ textAlign: 'right', display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                  <button
                    type="button"
                    className="ed-action-link"
                    style={{ fontSize: '11px' }}
                    onClick={() => {
                      setResetTargetUser(item)
                      setNewPassword('')
                      setResetError(null)
                    }}
                  >
                    [RESET SEC]
                  </button>
                  <button
                    type="button"
                    className="ed-action-link"
                    style={{ fontSize: '11px', color: isActive ? 'var(--ed-amber)' : 'var(--ed-teal-light)' }}
                    onClick={() => setStatusTargetUser(item)}
                  >
                    {isActive ? '[REVOKE]' : '[ENABLE]'}
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Provision IAM User Modal */}
      <Modal
        isOpen={isCreateOpen}
        title="PROVISION WORKFORCE IAM USER"
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
              className="cc-button cc-button-primary"
              disabled={createSubmitting}
              onClick={handleCreateSubmit}
            >
              {createSubmitting ? 'PROVISIONING...' : '[ PROVISION CREDENTIAL ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleCreateSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {createError && <div className="cc-form-error">[VALIDATION ERROR] {createError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">OPERATOR USERNAME *</label>
            <input
              type="text"
              className="cc-input"
              required
              minLength={2}
              maxLength={50}
              placeholder="e.g. ops_marcus"
              value={createForm.username}
              onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">CORPORATE EMAIL ADDRESS *</label>
            <input
              type="email"
              className="cc-input"
              required
              placeholder="e.g. marcus@enterprise-logistics.com"
              value={createForm.email}
              onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
            />
          </div>

          <div className="cc-form-group">
            <label className="cc-form-label">INITIAL ACCESS PASSWORD *</label>
            <input
              type="password"
              className="cc-input"
              required
              minLength={8}
              placeholder="Min 8 characters..."
              value={createForm.password}
              onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
            />
            <div style={{ fontSize: '10px', color: 'var(--cc-text-muted)', fontFamily: 'var(--cc-font-mono)', marginTop: '4px' }}>
              Minimum 8 characters. Password will be hashed using HMAC-SHA256 with tenant security stamp salt.
            </div>
          </div>
        </form>
      </Modal>

      {/* Confirm Status Toggle Modal */}
      <Modal
        isOpen={statusTargetUser !== null}
        title={statusTargetUser?.status === UserStatus.Active ? 'DEACTIVATE IAM USER' : 'ACTIVATE IAM USER'}
        onClose={() => setStatusTargetUser(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setStatusTargetUser(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className={`cc-button ${statusTargetUser?.status === UserStatus.Active ? 'cc-button-danger' : 'cc-button-primary'}`}
              disabled={statusSubmitting}
              onClick={handleStatusToggle}
            >
              {statusSubmitting ? 'UPDATING...' : statusTargetUser?.status === UserStatus.Active ? '[ CONFIRM DEACTIVATION ]' : '[ CONFIRM ACTIVATION ]'}
            </button>
          </>
        }
      >
        <p style={{ margin: 0, fontSize: '13px', lineHeight: 1.6 }}>
          Are you sure you want to {statusTargetUser?.status === UserStatus.Active ? 'deactivate' : 'activate'} workforce credential{' '}
          <strong>'{statusTargetUser?.username}'</strong>?
          <br /><br />
          {statusTargetUser?.status === UserStatus.Active
            ? 'Deactivation immediately invalidates active session tokens via the token validation query filter. Subsequent requests will return 401 Unauthorized.'
            : 'Activation allows the user to sign in with their active password and tenant context.'}
        </p>
      </Modal>

      {/* Reset Password Modal */}
      <Modal
        isOpen={resetTargetUser !== null}
        title={`RESET IAM CREDENTIAL: ${resetTargetUser?.username || ''}`}
        onClose={() => setResetTargetUser(null)}
        footer={
          <>
            <button
              type="button"
              className="cc-button"
              onClick={() => setResetTargetUser(null)}
            >
              [ CANCEL ]
            </button>
            <button
              type="button"
              className="cc-button cc-button-danger"
              disabled={resetSubmitting}
              onClick={handleResetPasswordSubmit}
            >
              {resetSubmitting ? 'RESETTING...' : '[ COMMIT PASSWORD RESET ]'}
            </button>
          </>
        }
      >
        <form onSubmit={handleResetPasswordSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
          {resetError && <div className="cc-form-error">[RESET ERROR] {resetError}</div>}

          <div className="cc-form-group">
            <label className="cc-form-label">NEW ACCESS PASSWORD *</label>
            <input
              type="password"
              className="cc-input"
              required
              minLength={8}
              placeholder="Min 8 characters..."
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <div style={{ fontSize: '10px', color: 'var(--cc-text-muted)', fontFamily: 'var(--cc-font-mono)', marginTop: '4px' }}>
              Minimum 8 characters. Resetting will immediately regenerate SecurityStamp and invalidate all current JWT tokens for this user.
            </div>
          </div>
        </form>
      </Modal>
    </div>
  )
}
