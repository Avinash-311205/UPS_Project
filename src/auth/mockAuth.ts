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

export function saveMockSession(user: AuthUser): void {
  window.sessionStorage.setItem(SESSION_KEY, user.id)
}

export function restoreMockSession(): AuthUser | null {
  const userId = window.sessionStorage.getItem(SESSION_KEY)
  const account = demoAccounts.find((candidate) => candidate.id === userId)
  if (!account) return null
  const { password: _password, ...user } = account
  return user
}

export function clearMockSession(): void {
  window.sessionStorage.removeItem(SESSION_KEY)
}
