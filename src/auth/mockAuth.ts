export type UserRole = 'operations-manager' | 'security-admin' | 'hub-operator'

export type AuthUser = {
  id: string
  employeeId: string
  name: string
  email: string
  role: UserRole
}

type MockAccount = AuthUser & {
  password: string
}

const SESSION_KEY = 'resiliencex-demo-user'

export const demoAccounts: MockAccount[] = [
  {
    id: 'user-manager-001',
    employeeId: 'RX-1042',
    name: 'Alex Morgan',
    email: 'manager@resiliencex.com',
    password: 'Password123!',
    role: 'operations-manager',
  },
  {
    id: 'user-security-001',
    employeeId: 'RX-2086',
    name: 'Priya Shah',
    email: 'security@resiliencex.com',
    password: 'SecurePass456!',
    role: 'security-admin',
  },
  {
    id: 'user-operator-001',
    employeeId: 'RX-3195',
    name: 'Luis Ortega',
    email: 'operator@resiliencex.com',
    password: 'HubOps789!',
    role: 'hub-operator',
  },
]

export const roleLabels: Record<UserRole, string> = {
  'operations-manager': 'Operations Manager',
  'security-admin': 'IT / Security Administrator',
  'hub-operator': 'Hub Operator',
}

export async function authenticateMockUser(identifier: string, password: string): Promise<AuthUser | null> {
  await new Promise((resolve) => window.setTimeout(resolve, 450))
  const normalizedIdentifier = identifier.trim().toLowerCase()
  const account = demoAccounts.find((candidate) =>
    (candidate.email.toLowerCase() === normalizedIdentifier || candidate.employeeId.toLowerCase() === normalizedIdentifier)
    && candidate.password === password,
  )

  if (!account) return null

  const { password: _password, ...user } = account
  return user
}

export function saveMockSession(user: AuthUser, token?: string): void {
  window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(user))
  if (token) {
    window.sessionStorage.setItem('resiliencex-auth-token', token)
  }
}

export function restoreMockSession(): AuthUser | null {
  const stored = window.sessionStorage.getItem(SESSION_KEY)
  if (!stored) return null

  try {
    const parsed = JSON.parse(stored)
    if (parsed && typeof parsed === 'object' && parsed.email && parsed.role) {
      return parsed as AuthUser
    }
  } catch {
    // If it was stored as a raw ID string in a previous session
    const account = demoAccounts.find((candidate) => candidate.id === stored)
    if (account) {
      const { password: _password, ...user } = account
      return user
    }
  }

  return null
}

export function clearMockSession(): void {
  window.sessionStorage.removeItem(SESSION_KEY)
  window.sessionStorage.removeItem('resiliencex-auth-token')
}
