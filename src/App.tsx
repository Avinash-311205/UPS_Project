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

  const finishLogin = (token: string, loginEmail: string, loginUsername?: string) => {
    const jwtPayload = parseJwtPayload(token)
    const resolvedName =
      loginUsername ||
      (jwtPayload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string) ||
      (jwtPayload?.['name'] as string) ||
      loginEmail.split('@')[0]
    const authenticatedUser: AuthUser = {
      id: String(jwtPayload?.['sub'] || loginEmail),
      employeeId: loginEmail,
      name: resolvedName,
      email: loginEmail,
      role: 'operations-manager',
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

        finishLogin(loginRes.data.token, loginRes.data.email, loginRes.data.username)
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

      finishLogin(loginRes.data.token, loginRes.data.email, loginRes.data.username)
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
