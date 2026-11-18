export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T | null
}

export interface LoginResult {
  token: string
  email: string
  username?: string
}

export interface RegisterResult {
  id: number
  username: string
  email: string
}

export async function loginUser(email: string, password: string): Promise<ApiResponse<LoginResult>> {
  const response = await fetch('/api/authentication/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password }),
  })

  const data = await response.json()
  return data
}

export async function registerUser(username: string, email: string, password: string): Promise<ApiResponse<RegisterResult>> {
  const response = await fetch('/api/authentication/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, email, password }),
  })

  const data = await response.json()
  return data
}

export function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const base64Url = token.split('.')[1]
    if (!base64Url) return null
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch {
    return null
  }
}
