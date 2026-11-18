import { useState } from 'react'
import { authenticateMockUser, clearMockSession, demoAccounts, restoreMockSession, saveMockSession } from './auth/mockAuth'
import type { AuthUser } from './auth/mockAuth'
import { loginUser, parseJwtPayload, registerUser } from './services/authApi'
import DashboardApp from './Dashboard'
import LoginPage from './pages/Login'
import './App.css'

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(() => restoreMockSession())
  const [isRegister, setIsRegister] = useState(false)
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const handleAuthSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setSuccessMessage('')
    setIsLoading(true)

    try {
      if (isRegister) {
        // --- 1. Backend Registration (/api/authentication/register) ---
        const regRes = await registerUser(username.trim(), email.trim(), password)
        if (!regRes.success || !regRes.data) {
          setError(regRes.message || 'Registration failed. Please check your details.')
          return
        }

        setSuccessMessage('Account registered successfully! Signing you in…')

        // Auto sign-in after registration
        try {
          const loginRes = await loginUser(email.trim(), password)
          if (loginRes.success && loginRes.data) {
            const token = loginRes.data.token
            const jwtPayload = parseJwtPayload(token)
            const resolvedName =
              (jwtPayload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string) ||
              regRes.data.username ||
              username.trim()

            const authenticatedUser: AuthUser = {
              id: String(regRes.data.id || email.trim()),
              employeeId: email.trim(),
              name: resolvedName,
              email: email.trim(),
              role: 'operations-manager',
            }

            saveMockSession(authenticatedUser, token)
            setUser(authenticatedUser)
            return
          }
        } catch {
          // If auto sign-in has any issue, switch back to sign-in tab with success notice
          setIsRegister(false)
          setSuccessMessage('Account created! Please sign in with your credentials.')
          return
        }

        setIsRegister(false)
        setSuccessMessage('Account created! Please sign in with your credentials.')
      } else {
        // --- 2. Backend Login (/api/authentication/login) ---
        // If employee ID format, check demo account directly
        if (email.trim().toUpperCase().startsWith('RX-')) {
          const demoUser = await authenticateMockUser(email.trim(), password)
          if (demoUser) {
            saveMockSession(demoUser)
            setUser(demoUser)
            return
          }
        }

        try {
          const loginRes = await loginUser(email.trim(), password)
          if (loginRes.success && loginRes.data) {
            const token = loginRes.data.token
            const jwtPayload = parseJwtPayload(token)
            const resolvedName =
              (jwtPayload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string) ||
              (jwtPayload?.['name'] as string) ||
              email.trim().split('@')[0]

            const authenticatedUser: AuthUser = {
              id: String(jwtPayload?.['sub'] || loginRes.data.email),
              employeeId: loginRes.data.email,
              name: resolvedName,
              email: loginRes.data.email,
              role: 'operations-manager',
            }

            saveMockSession(authenticatedUser, token)
            setUser(authenticatedUser)
            return
          }

          // If backend returns failure, check if it matches a demo account
          const demoUser = await authenticateMockUser(email.trim(), password)
          if (demoUser) {
            saveMockSession(demoUser)
            setUser(demoUser)
            return
          }

          setError(loginRes.message || 'Invalid email or password.')
        } catch {
          // If backend is not running or unreachable, fallback to demo accounts
          const demoUser = await authenticateMockUser(email.trim(), password)
          if (demoUser) {
            saveMockSession(demoUser)
            setUser(demoUser)
            return
          }
          setError('Could not connect to the backend server. Make sure your ASP.NET Core API is running on http://localhost:5123.')
        }
      }
    } finally {
      setIsLoading(false)
    }
  }

  const signInDemo = async (account: (typeof demoAccounts)[number]) => {
    setError('')
    setSuccessMessage('')
    setIsLoading(true)
    try {
      const authenticatedUser = await authenticateMockUser(account.employeeId, account.password)
      if (!authenticatedUser) {
        setError('This demo account could not be authenticated. Please try again.')
        return
      }
      saveMockSession(authenticatedUser)
      setUser(authenticatedUser)
    } finally {
      setIsLoading(false)
    }
  }

  const signOut = () => {
    clearMockSession()
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
      onSelectDemo={signInDemo}
    />
  )
}
