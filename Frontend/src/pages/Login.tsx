import { useState } from 'react'
import { demoAccounts, roleLabels } from '../auth/mockAuth'
import type { UserRole } from '../auth/mockAuth'
import Icon from '../components/Icon'
import { currentYear } from '../data/mockData'

export default function LoginPage({
  email,
  password,
  showPassword,
  isLoading,
  error,
  registrationError,
  setEmail,
  setPassword,
  setShowPassword,
  setError,
  onSubmit,
  onSignUp,
  onSelectDemo,
}: {
  email: string
  password: string
  showPassword: boolean
  isLoading: boolean
  error: string
  registrationError: string
  setEmail: (value: string) => void
  setPassword: (value: string) => void
  setShowPassword: (value: boolean) => void
  setError: (value: string) => void
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void
  onSignUp: (name: string, email: string, password: string, role: UserRole) => void
  onSelectDemo: (account: (typeof demoAccounts)[number]) => void
}) {
  const [authMode, setAuthMode] = useState<'register' | 'login'>('register')
  const [name, setName] = useState('')
  const [registrationEmail, setRegistrationEmail] = useState('')
  const [registrationPassword, setRegistrationPassword] = useState('')
  const [role, setRole] = useState<UserRole>('operations-manager')

  const submitRegistration = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    onSignUp(name, registrationEmail, registrationPassword, role)
  }

  return (
    <div className="auth-shell">
      <header className="auth-topbar">
        <div className="auth-brand">
          <span className="brand-mark auth-brand-mark">R<span>X</span></span>
          <span className="brand-name">resilience<span>x</span><small>LOGISTICS RESILIENCE</small></span>
        </div>
        <div className="auth-topbar-note"><span className="live-dot" /> SECURE OPERATIONS WORKSPACE</div>
      </header>

      <main className="auth-content">
        <section className="auth-welcome">
          <div className="auth-kicker"><span className="live-dot" /> YOUR NETWORK. IN MOTION.</div>
          <h1>Keep every<br />delivery moving.</h1>
          <p>Coordinate your team, track shipments, and stay ready when operations are disrupted.</p>
          <div className="auth-feature-row">
            <span><Icon name="pin" size={16} /> Live fleet visibility</span>
            <span><Icon name="shield" size={16} /> Continuity ready</span>
          </div>
        </section>

        <section className="auth-forms" aria-label="Account access">
          <div className="auth-tabs" role="tablist" aria-label="Choose account action">
            <button type="button" role="tab" id="register-tab" aria-selected={authMode === 'register'} aria-controls="register-panel" className={authMode === 'register' ? 'auth-tab active' : 'auth-tab'} onClick={() => { setAuthMode('register'); setError('') }}>Register</button>
            <button type="button" role="tab" id="login-tab" aria-selected={authMode === 'login'} aria-controls="login-panel" className={authMode === 'login' ? 'auth-tab active' : 'auth-tab'} onClick={() => { setAuthMode('login'); setError('') }}>Login</button>
          </div>
          {authMode === 'register' ? (
            <article className="auth-card signup-card auth-panel" id="register-panel" role="tabpanel" aria-labelledby="register-tab">
              <div className="auth-card-heading">
                <span className="card-kicker">NEW TO RESILIENCEX</span>
                <h2>Create your account</h2>
                <p>Choose your role to open the right workspace.</p>
              </div>
              <form className="auth-form" onSubmit={submitRegistration}>
                <label htmlFor="signup-name">Full name</label>
                <input id="signup-name" autoComplete="name" value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Jordan Lee" required />
                <label htmlFor="signup-email">Company email</label>
                <input id="signup-email" type="email" autoComplete="email" value={registrationEmail} onChange={(event) => setRegistrationEmail(event.target.value)} placeholder="you@company.com" required />
                <label htmlFor="signup-role">Your role</label>
                <select id="signup-role" value={role} onChange={(event) => setRole(event.target.value as UserRole)}>
                  <option value="operations-manager">{roleLabels['operations-manager']}</option>
                  <option value="hub-operator">{roleLabels['hub-operator']}</option>
                  <option value="security-admin">{roleLabels['security-admin']}</option>
                </select>
                <label htmlFor="signup-password">Create password</label>
                <input id="signup-password" type="password" autoComplete="new-password" value={registrationPassword} onChange={(event) => setRegistrationPassword(event.target.value)} placeholder="At least 8 characters" minLength={8} required />
                {registrationError && <div className="auth-error" role="alert">{registrationError}</div>}
                <button type="submit" className="auth-primary-button">Create account <Icon name="chevron" size={16} /></button>
                <p className="auth-form-note">Your account is saved in this browser for this demo.</p>
              </form>
            </article>
          ) : (
            <article className="auth-card login-card-new auth-panel" id="login-panel" role="tabpanel" aria-labelledby="login-tab">
              <div className="auth-card-heading">
                <span className="card-kicker">ALREADY REGISTERED</span>
                <h2>Welcome back</h2>
                <p>Sign in to continue to your workspace.</p>
              </div>
              <form className="auth-form" onSubmit={onSubmit}>
                <label htmlFor="login-identifier">Employee ID or company email</label>
                <input id="login-identifier" autoComplete="username" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="RX-1042 or name@company.com" required />
                <div className="auth-password-label">
                  <label htmlFor="login-password">Password</label>
                  <button type="button" className="auth-text-button" onClick={() => setError('For this demo, use a sample account below. Password recovery is managed by your identity provider in production.')}>Forgot password?</button>
                </div>
                <div className="auth-password-input">
                  <input id="login-password" type={showPassword ? 'text' : 'password'} autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Enter your password" required />
                  <button type="button" onClick={() => setShowPassword(!showPassword)} aria-label={showPassword ? 'Hide password' : 'Show password'}>{showPassword ? 'Hide' : 'Show'}</button>
                </div>
                {error && <div className="auth-error" role="alert">{error}</div>}
                <button type="submit" className="auth-primary-button" disabled={isLoading}>{isLoading ? 'Signing in…' : 'Sign in'} <Icon name="chevron" size={16} /></button>
              </form>

              <div className="auth-demo-access">
                <div className="auth-demo-heading"><span>TRY A DEMO ROLE</span><span>Quick access</span></div>
                {demoAccounts.map((account) => (
                  <button type="button" className="auth-demo-account" key={account.id} onClick={() => onSelectDemo(account)} disabled={isLoading}>
                    <span className="auth-demo-avatar">{account.name.split(' ').map((part) => part[0]).join('')}</span>
                    <span><strong>{roleLabels[account.role]}</strong><small>{account.name}</small></span>
                    <Icon name="chevron" size={15} />
                  </button>
                ))}
                <p>Demo only — do not use real credentials.</p>
              </div>
            </article>
          )}
        </section>
      </main>
      <footer className="auth-footer"><span>© {currentYear} ResilienceX</span><span>Business continuity for logistics teams</span></footer>
    </div>
  )
}
