import { useState } from 'react'
import { authenticateMockUser, clearMockSession, demoAccounts, registerMockUser, restoreMockSession, saveMockSession } from './auth/mockAuth'
import type { AuthUser, UserRole } from './auth/mockAuth'
import DashboardApp from './Dashboard'
import LoginPage from './pages/Login'
import './App.css'

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(() => restoreMockSession())
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')
  const [registrationError, setRegistrationError] = useState('')

  const signIn = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsLoading(true)
    try {
      const authenticatedUser = await authenticateMockUser(email, password)
      if (!authenticatedUser) {
        setError('We could not verify those credentials. Check your employee ID or email and password.')
        return
      }
      saveMockSession(authenticatedUser)
      setUser(authenticatedUser)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Sign in failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  const signUp = (name: string, registrationEmail: string, registrationPassword: string, role: UserRole) => {
    setRegistrationError('')
    try {
      const registeredUser = registerMockUser(name, registrationEmail, registrationPassword, role)
      saveMockSession(registeredUser)
      setUser(registeredUser)
    } catch (cause) {
      setRegistrationError(cause instanceof Error ? cause.message : 'Registration failed. Please try again.')
    }
  }

  const signInDemo = async (account: (typeof demoAccounts)[number]) => {
    setError('')
    setIsLoading(true)
    try {
      const authenticatedUser = await authenticateMockUser(account.employeeId, account.password)
      if (!authenticatedUser) {
        setError('This demo account could not be authenticated. Please try again.')
        return
      }
      saveMockSession(authenticatedUser)
      setUser(authenticatedUser)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Demo sign in failed. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  const signOut = () => {
    clearMockSession()
    setUser(null)
    setPassword('')
  }

  if (user) return <DashboardApp user={user} onLogout={signOut} />

  return <LoginPage email={email} password={password} showPassword={showPassword} isLoading={isLoading} error={error} registrationError={registrationError} setEmail={setEmail} setPassword={setPassword} setShowPassword={setShowPassword} setError={setError} onSubmit={signIn} onSignUp={signUp} onSelectDemo={signInDemo} />
}
