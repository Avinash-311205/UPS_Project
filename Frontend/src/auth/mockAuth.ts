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
const REGISTERED_USERS_KEY = 'resiliencex-registered-users'

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

function getRegisteredUsers(): MockAccount[] {
  const savedUsers = window.localStorage.getItem(REGISTERED_USERS_KEY)
  if (!savedUsers) return []

  let users: unknown
  try {
    users = JSON.parse(savedUsers)
  } catch {
    throw new Error('Saved registrations could not be read. Clear this browser’s saved app data and try again.')
  }
  if (!Array.isArray(users) || users.some((user) =>
    typeof user !== 'object'
    || user === null
    || typeof user.id !== 'string'
    || typeof user.employeeId !== 'string'
    || typeof user.name !== 'string'
    || typeof user.email !== 'string'
    || typeof user.password !== 'string'
    || !Object.hasOwn(roleLabels, user.role),
  )) {
    throw new Error('Saved registrations could not be read. Clear this browser’s saved app data and try again.')
  }
  return users as MockAccount[]
}

export function registerMockUser(name: string, email: string, password: string, role: UserRole): AuthUser {
  const registeredUsers = getRegisteredUsers()
  const normalizedEmail = email.trim().toLowerCase()
  if ([...demoAccounts, ...registeredUsers].some((account) => account.email.toLowerCase() === normalizedEmail)) {
    throw new Error('An account already uses this email. Sign in instead.')
  }

  const employeeId = `RX-${String(5001 + registeredUsers.length).padStart(4, '0')}`
  const user: MockAccount = {
    id: `registered-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    employeeId,
    name: name.trim(),
    email: normalizedEmail,
    password,
    role,
  }
  window.localStorage.setItem(REGISTERED_USERS_KEY, JSON.stringify([...registeredUsers, user]))
  const { password: _password, ...authenticatedUser } = user
  return authenticatedUser
}

export async function authenticateMockUser(identifier: string, password: string): Promise<AuthUser | null> {
  await new Promise((resolve) => window.setTimeout(resolve, 450))
  const normalizedIdentifier = identifier.trim().toLowerCase()
  const account = [...demoAccounts, ...getRegisteredUsers()].find((candidate) =>
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
  const account = [...demoAccounts, ...getRegisteredUsers()].find((candidate) => candidate.id === userId)
  if (!account) return null
  const { password: _password, ...user } = account
  return user
}

export function clearMockSession(): void {
  window.sessionStorage.removeItem(SESSION_KEY)
}
