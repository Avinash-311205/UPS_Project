import { useCallback, useState } from 'react'
import { googleLogin, loginUser, registerUser } from './services/authApi'
import LoginPage from './pages/Login'
import StaticHome from './pages/StaticHome'
import './App.css'

export default function App() {
  const [isSignedIn, setIsSignedIn] = useState(false)
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
        const regRes = await registerUser(username.trim(), email.trim(), password)
        if (!regRes.success || !regRes.data) {
          setError(regRes.message || 'Registration failed. Please check your details.')
          return
        }

        setIsSignedIn(true)
      } else {
        const loginRes = await loginUser(email.trim(), password)
        if (!loginRes.success || !loginRes.data) {
          setError(loginRes.message || 'Invalid email or password.')
          return
        }

        setPassword('')
        setIsSignedIn(true)
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

      setPassword('')
      setIsSignedIn(true)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to reach the authentication service.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  if (isSignedIn) {
    return <StaticHome displayName={username.trim() || email.split('@')[0] || 'Operations'} />
  }

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
