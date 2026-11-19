import { useCallback, useState } from 'react'
import { googleLogin, loginUser, registerUser } from './services/authApi'
import LoginPage from './pages/Login'
import './App.css'

export default function App() {
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

        setIsRegister(false)
        setPassword('')
        setSuccessMessage('Account created. Sign in with your email and password.')
      } else {
        const loginRes = await loginUser(email.trim(), password)
        if (!loginRes.success || !loginRes.data) {
          setError(loginRes.message || 'Invalid email or password.')
          return
        }

        setPassword('')
        setSuccessMessage('Sign-in successful.')
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
      setSuccessMessage('Google sign-in successful.')
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to reach the authentication service.')
    } finally {
      setIsLoading(false)
    }
  }, [])

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
