export type UserRole = 'operations-manager' | 'security-admin' | 'hub-operator'

export type AuthUser = {
  id: string
  employeeId: string
  name: string
  email: string
  role: UserRole
}

export const roleLabels: Record<UserRole, string> = {
  'operations-manager': 'Operations Manager',
  'security-admin': 'IT / Security Administrator',
  'hub-operator': 'Hub Operator',
}

const SESSION_KEY = 'resiliencex-user'
const TOKEN_KEY = 'resiliencex-auth-token'

export function saveSession(user: AuthUser, token: string): void {
  window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(user))
  window.sessionStorage.setItem(TOKEN_KEY, token)
}

export function restoreSession(): AuthUser | null {
  const storedUser = window.sessionStorage.getItem(SESSION_KEY)
  const token = window.sessionStorage.getItem(TOKEN_KEY)
  if (!storedUser || !token) {
    clearSession()
    return null
  }

  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))) as { exp?: number }
    if (typeof payload.exp !== 'number' || payload.exp * 1000 <= Date.now()) {
      clearSession()
      return null
    }

    const user = JSON.parse(storedUser) as AuthUser
    if (!user || typeof user.email !== 'string' || typeof user.name !== 'string') {
      clearSession()
      return null
    }
    return user
  } catch {
    clearSession()
    return null
  }
}

export function clearSession(): void {
  window.sessionStorage.removeItem(SESSION_KEY)
  window.sessionStorage.removeItem(TOKEN_KEY)
}
