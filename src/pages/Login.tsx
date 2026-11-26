import { useEffect, useRef } from 'react'

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: {
            client_id: string
            callback: (response: { credential: string }) => void
          }) => void
          renderButton: (
            parent: HTMLElement,
            options: { theme: 'outline'; size: 'large'; text: 'continue_with'; width: number },
          ) => void
        }
      }
    }
  }
}

const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID
const googleScriptUrl = 'https://accounts.google.com/gsi/client'

export default function LoginPage({
  isRegister,
  setIsRegister,
  username,
  setUsername,
  email,
  password,
  showPassword,
  isLoading,
  error,
  successMessage,
  setEmail,
  setPassword,
  setShowPassword,
  setError,
  setSuccessMessage,
  onSubmit,
  onGoogleToken,
}: {
  isRegister: boolean
  setIsRegister: (value: boolean) => void
  username: string
  setUsername: (value: string) => void
  email: string
  password: string
  showPassword: boolean
  isLoading: boolean
  error: string
  successMessage: string
  setEmail: (value: string) => void
  setPassword: (value: string) => void
  setShowPassword: (value: boolean) => void
  setError: (value: string) => void
  setSuccessMessage: (value: string) => void
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void
  onGoogleToken: (credential: string) => void
}) {
  const googleButtonRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!googleClientId) return

    let cancelled = false
    const renderGoogleButton = () => {
      const google = window.google
      const button = googleButtonRef.current
      if (cancelled || !google || !button) return

      google.accounts.id.initialize({
        client_id: googleClientId,
        callback: ({ credential }) => onGoogleToken(credential),
      })
      const buttonWidth = Math.max(240, Math.min(button.clientWidth || 360, 360))
      google.accounts.id.renderButton(button, {
        theme: 'outline',
        size: 'large',
        text: 'continue_with',
        width: buttonWidth,
      })
    }

    if (window.google) {
      renderGoogleButton()
    } else {
      let script = document.querySelector<HTMLScriptElement>(`script[src="${googleScriptUrl}"]`)
      if (!script) {
        script = document.createElement('script')
        script.src = googleScriptUrl
        script.async = true
        script.defer = true
        document.head.appendChild(script)
      }
      script.addEventListener('load', renderGoogleButton, { once: true })
      script.addEventListener('error', () => {
        if (!cancelled) setError('Google sign-in could not be loaded. Please use email and password.')
      }, { once: true })
    }

    return () => {
      cancelled = true
    }
  }, [onGoogleToken, setError])

  const changeMode = (register: boolean) => {
    setIsRegister(register)
    setPassword('')
    setError('')
    setSuccessMessage('')
  }

  return (
    <main className="auth-screen">
      <header className="auth-wordmark" aria-label="ResilienceX">
        <span className="auth-brand-title">
          ResilienceX<span className="auth-brand-dot">.</span>
        </span>
        <span className="auth-brand-sub">OPERATIONAL INTELLIGENCE</span>
      </header>

      <section className="login-card" aria-labelledby="auth-heading">
        <div className="auth-mode-switch" role="tablist" aria-label="Account access">
          <button
            type="button"
            role="tab"
            aria-selected={!isRegister}
            className={`auth-mode-tab ${!isRegister ? 'active' : ''}`}
            onClick={() => changeMode(false)}
          >
            Sign in
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={isRegister}
            className={`auth-mode-tab ${isRegister ? 'active' : ''}`}
            onClick={() => changeMode(true)}
          >
            Register
          </button>
        </div>

        <div className="login-heading">
          <h1 id="auth-heading">{isRegister ? 'Create your account' : 'Sign in'}</h1>
          <p>{isRegister ? 'Join your ResilienceX workspace.' : 'Welcome back — your workspace is ready.'}</p>
        </div>

        <div className="google-signin">
          {googleClientId ? (
            <div className="google-button-container" ref={googleButtonRef} />
          ) : (
            <>
              <button type="button" className="google-button" disabled aria-label="Google sign-in setup required">
                <span className="google-g" aria-hidden="true">G</span>
                Continue with Google
              </button>
              <span className="google-setup-hint">Google sign-in needs a configured OAuth client ID.</span>
            </>
          )}
        </div>

        <div className="auth-divider"><span>or continue with email</span></div>

        <form className="login-form" onSubmit={onSubmit}>
          {isRegister && (
            <>
              <label htmlFor="register-username">Username</label>
              <input
                id="register-username"
                autoComplete="username"
                maxLength={50}
                value={username}
                onChange={(event) => setUsername(event.target.value)}
                placeholder="Up to 50 characters"
                required
              />
            </>
          )}

          <label htmlFor="login-email">Email</label>
          <input
            id="login-email"
            type="email"
            autoComplete="email"
            maxLength={254}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@example.com"
            required
          />

          <div className="password-label-row">
            <label htmlFor="login-password">Password</label>
            <button
              type="button"
              className="text-button password-toggle-inline"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? 'Hide' : 'Show'}
            </button>
          </div>
          <input
            id="login-password"
            type={showPassword ? 'text' : 'password'}
            autoComplete={isRegister ? 'new-password' : 'current-password'}
            minLength={isRegister ? 8 : undefined}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder={isRegister ? 'At least 8 characters' : 'Your password'}
            required
          />

          {successMessage && <div className="auth-success" role="status">{successMessage}</div>}
          {error && <div className="auth-error" role="alert">{error}</div>}

          <button type="submit" className="button button-primary auth-submit" disabled={isLoading}>
            {isLoading ? 'Please wait…' : (isRegister ? 'Create account' : 'Enter dashboard')}
          </button>
        </form>

        <p className="auth-footer">
          {isRegister ? 'Already have an account?' : 'New to ResilienceX?'}{' '}
          <button type="button" className="auth-footer-link" onClick={() => changeMode(!isRegister)}>
            {isRegister ? 'Sign in' : 'Create an account'}
          </button>
        </p>
      </section>

      <p className="auth-caption">ResilienceX <span>·</span> Operational Resilience and Dependency Management.</p>
    </main>
  )
}
