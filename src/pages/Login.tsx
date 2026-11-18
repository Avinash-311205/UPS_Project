import { useEffect, useRef, useState } from 'react'
import { demoAccounts, roleLabels } from '../auth/mockAuth'
import Icon from '../components/Icon'

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
  onSelectDemo,
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
  successMessage?: string
  setEmail: (value: string) => void
  setPassword: (value: string) => void
  setShowPassword: (value: boolean) => void
  setError: (value: string) => void
  setSuccessMessage?: (value: string) => void
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void
  onSelectDemo: (account: (typeof demoAccounts)[number]) => void
}) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const [showIntro, setShowIntro] = useState(true)
  const [playbackBlocked, setPlaybackBlocked] = useState(false)

  useEffect(() => {
    if (!showIntro) return
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.body.style.overflow = previousOverflow
    }
  }, [showIntro])

  useEffect(() => {
    const video = videoRef.current
    if (!video || !video.paused) return

    void video.play()
      .then(() => setPlaybackBlocked(false))
      .catch(() => setPlaybackBlocked(true))
  }, [])

  const playVideo = async () => {
    const video = videoRef.current
    if (!video) return

    try {
      await video.play()
      setPlaybackBlocked(false)
    } catch {
      setPlaybackBlocked(true)
    }
  }

  const finishIntro = () => {
    videoRef.current?.pause()
    setShowIntro(false)
  }

  return (
    <div className="auth-screen">
      <section className="auth-brand-panel">
        <div className="auth-brand">
          <span className="brand-mark auth-brand-mark">R<span>X</span></span>
          <span className="brand-name">resilience<span>x</span><small>LOGISTICS RESILIENCE</small></span>
        </div>
        <div className="auth-intro">
          <span className="auth-kicker"><span className="live-dot" /> CONTINUITY, CONTROLLED</span>
          <h1>Keep your logistics network moving.</h1>
          <p>One trusted workspace to monitor disruptions, coordinate response, and maintain essential operations.</p>
        </div>
        <div className="auth-highlights">
          <div><strong>05</strong><span>critical systems monitored</span></div>
          <div><strong>2–3h</strong><span>target recovery window</span></div>
        </div>
        <p className="auth-brand-note">ResilienceX · Intelligent logistics cyber-resilience</p>
      </section>

      <main className="auth-main">
        <section className="login-card">
          <div className="auth-mode-switch">
            <button
              type="button"
              className={`auth-mode-tab ${!isRegister ? 'active' : ''}`}
              onClick={() => {
                setIsRegister(false)
                setError('')
                setSuccessMessage?.('')
              }}
            >
              Sign In
            </button>
            <button
              type="button"
              className={`auth-mode-tab ${isRegister ? 'active' : ''}`}
              onClick={() => {
                setIsRegister(true)
                setError('')
                setSuccessMessage?.('')
              }}
            >
              Create Account
            </button>
          </div>

          <div className="login-heading">
            <span className="card-kicker">SECURE WORKSPACE</span>
            <h2>{isRegister ? 'Create account' : 'Welcome back'}</h2>
            <p>{isRegister ? 'Register your account to access the logistics workspace.' : 'Sign in with your company account to continue.'}</p>
          </div>

          <form className="login-form" onSubmit={onSubmit}>
            {isRegister && (
              <>
                <label htmlFor="register-username">Username</label>
                <input
                  id="register-username"
                  autoComplete="username"
                  maxLength={10}
                  value={username}
                  onChange={(event) => setUsername(event.target.value)}
                  placeholder="e.g. alexm (max 10 chars)"
                  required
                />
                <span className="field-hint">Maximum 10 characters</span>
              </>
            )}

            <label htmlFor="login-identifier">{isRegister ? 'Company email' : 'Employee ID or company email'}</label>
            <input
              id="login-identifier"
              type={isRegister ? 'email' : 'text'}
              autoComplete={isRegister ? 'email' : 'username'}
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder={isRegister ? 'name@company.com' : 'RX-1042 or name@company.com'}
              required
            />

            <div className="password-label-row">
              <label htmlFor="login-password">Password</label>
              {!isRegister && (
                <button type="button" className="text-button" onClick={() => setError('For this demo, use one of the sample accounts below. In production, password recovery will be handled by the identity provider.')}>
                  Forgot password?
                </button>
              )}
            </div>
            <div className="password-field">
              <input
                id="login-password"
                type={showPassword ? 'text' : 'password'}
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                minLength={isRegister ? 8 : undefined}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder={isRegister ? 'Create password (min 8 chars)' : 'Enter your password'}
                required
              />
              <button type="button" className="password-toggle" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? 'Hide password' : 'Show password'}>
                {showPassword ? 'Hide' : 'Show'}
              </button>
            </div>
            {isRegister && <span className="field-hint">Minimum 8 characters</span>}

            {successMessage && <div className="auth-success" role="status">{successMessage}</div>}
            {error && <div className="auth-error" role="alert">{error}</div>}

            <button type="submit" className="button button-primary auth-submit" disabled={isLoading}>
              {isLoading ? (isRegister ? 'Creating account…' : 'Signing in…') : (isRegister ? 'Create account' : 'Sign in')}
              <Icon name="chevron" size={15} />
            </button>
          </form>

          <div className="demo-access">
            <div className="demo-access-heading"><span>DEMO ACCOUNTS</span><span>Click a role to enter</span></div>
            <div className="demo-account-list">
              {demoAccounts.map((account) => (
                <button type="button" className="demo-account" key={account.id} onClick={() => onSelectDemo(account)} disabled={isLoading}>
                  <span className="demo-avatar">{account.name.split(' ').map((part) => part[0]).join('')}</span>
                  <span className="demo-account-copy"><strong>{account.name}</strong><small>{roleLabels[account.role]}</small></span>
                  <Icon name="chevron" size={14} />
                </button>
              ))}
            </div>
            <p className="demo-disclaimer">Demo only — authentication and permissions are simulated in this frontend. Do not use real credentials.</p>
          </div>
        </section>
      </main>
      {showIntro && (
        <div className="login-intro-overlay" role="dialog" aria-label="ResilienceX introduction video">
          <video
            ref={videoRef}
            src="/promo_video.mp4"
            autoPlay
            playsInline
            preload="auto"
            aria-label="ResilienceX logistics resilience introduction"
            onPlay={() => setPlaybackBlocked(false)}
            onEnded={finishIntro}
            onError={() => {
              setError('The introduction video could not be loaded. You can still sign in.')
              finishIntro()
            }}
          />
          {playbackBlocked && (
            <button type="button" className="intro-play-prompt" onClick={() => void playVideo()}>
              <span className="video-play-icon">▶</span>
              <span><strong>Play introduction with sound</strong><small>Your browser requires a click to allow audio playback</small></span>
            </button>
          )}
          <button type="button" className="intro-skip" onClick={finishIntro}>Skip intro</button>
        </div>
      )}
    </div>
  )
}
