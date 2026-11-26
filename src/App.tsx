import { useCallback, useState } from 'react'
import { clearSession, restoreSession, saveSession } from './auth/session'
import type { AuthUser } from './auth/session'
import { googleLogin, loginUser, parseJwtPayload, registerUser } from './services/authApi'
import DashboardApp from './Dashboard'
import LoginPage from './pages/Login'
import './App.css'

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(() => restoreSession())
  const [isRegister, setIsRegister] = useState(false)
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const finishLogin = (
    token: string,
    loginEmail: string,
    loginUsername?: string,
    roleId?: number | null,
    roleCode?: string | null,
    roleName?: string | null
  ) => {
    const jwtPayload = parseJwtPayload(token)
    const resolvedName =
      loginUsername ||
      (jwtPayload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string) ||
      (jwtPayload?.['name'] as string) ||
      loginEmail.split('@')[0]
    const rawRole =
      (jwtPayload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as string) ||
      (jwtPayload?.['role'] as string) ||
      (jwtPayload?.['account_type'] as string)
    const accountType: 'Root' | 'IAM' = rawRole === 'Root' ? 'Root' : 'IAM'
    const tenantId = (jwtPayload?.['tenant_id'] as string) || null

    const resolvedRoleCode =
      accountType === 'Root'
        ? null
        : (roleCode || (jwtPayload?.['operational_role'] as string) || 'UNASSIGNED')
    const resolvedRoleId =
      accountType === 'Root'
        ? null
        : (roleId !== undefined && roleId !== null
            ? roleId
            : jwtPayload?.['role_id']
            ? Number(jwtPayload['role_id'])
            : null)
    const resolvedRoleName =
      accountType === 'Root'
        ? null
        : roleName ||
          (resolvedRoleCode === 'ADMIN'
            ? 'Admin'
            : resolvedRoleCode === 'HUB_OPERATOR'
            ? 'Hub Operator'
            : resolvedRoleCode === 'SECURITY_MANAGER'
            ? 'Security Manager'
            : 'Unassigned')

    const authenticatedUser: AuthUser = {
      id: String(jwtPayload?.['sub'] || loginEmail),
      employeeId: loginEmail,
      name: resolvedName,
      email: loginEmail,
      role: 'operations-manager',
      accountType,
      tenantId,
      roleId: resolvedRoleId,
      roleCode: resolvedRoleCode,
      roleName: resolvedRoleName,
    }

    saveSession(authenticatedUser, token)
    setUser(authenticatedUser)
  }

  const handleAuthSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setSuccessMessage('')
    setIsLoading(true)

    try {
      if (isRegister) {
        const regRes = await registerUser(username.trim(), email.trim(), password)
        if (!regRes.success || !regRes.data) {
          setError(regRes.message || 'Registration failed. Please check your details.')
          return
        }

        setIsRegister(false)
        setPassword('')
        setSuccessMessage('Account created. Sign in with your email and password.')
      } else {
        const loginRes = await loginUser(email.trim(), password)
        if (!loginRes.success || !loginRes.data) {
          setError(loginRes.message || 'Invalid email or password.')
          return
        }

        finishLogin(
          loginRes.data.token,
          loginRes.data.email,
          loginRes.data.username,
          loginRes.data.roleId,
          loginRes.data.roleCode,
          loginRes.data.roleName
        )
      }
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to reach the authentication service.')
    } finally {
      setIsLoading(false)
    }
  }

  const handleGoogleLogin = useCallback(async (credential: string) => {
    setError('')
    setSuccessMessage('')
    setIsLoading(true)
    try {
      const loginRes = await googleLogin(credential)
      if (!loginRes.success || !loginRes.data) {
        setError(loginRes.message || 'Google sign-in could not be verified.')
        return
      }

      finishLogin(
        loginRes.data.token,
        loginRes.data.email,
        loginRes.data.username,
        loginRes.data.roleId,
        loginRes.data.roleCode,
        loginRes.data.roleName
      )
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to reach the authentication service.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  const signOut = () => {
    clearSession()
    setUser(null)
    setPassword('')
    setError('')
    setSuccessMessage('')
  }

  if (user) return <DashboardApp user={user} onLogout={signOut} />

  return (
    <LoginPage
      isRegister={isRegister}
      setIsRegister={setIsRegister}
      username={username}
      setUsername={setUsername}
      email={email}
      password={password}
      showPassword={showPassword}
      isLoading={isLoading}
      error={error}
      successMessage={successMessage}
      setEmail={setEmail}
      setPassword={setPassword}
      setShowPassword={setShowPassword}
      setError={setError}
      setSuccessMessage={setSuccessMessage}
      onSubmit={handleAuthSubmit}
      onGoogleToken={handleGoogleLogin}
    />
  )
}
