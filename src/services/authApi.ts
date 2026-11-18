export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T | null
}

export interface LoginResult {
  token: string
  email: string
  username: string
}

export interface RegisterResult {
  id: number
  username: string
  email: string
}

async function postJson<T>(path: string, body: unknown): Promise<ApiResponse<T>> {
  const response = await fetch(path, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
  })

  let data: ApiResponse<T>
  try {
    data = await response.json() as ApiResponse<T>
  } catch {
    throw new Error('The authentication service returned an invalid response.')
  }

  if (!response.ok && data.success) {
    throw new Error(data.message || `Authentication failed (${response.status}).`)
  }
  return data
}

export function loginUser(email: string, password: string): Promise<ApiResponse<LoginResult>> {
  return postJson('/api/authentication/login', { email, password })
}

export function registerUser(username: string, email: string, password: string): Promise<ApiResponse<RegisterResult>> {
  return postJson('/api/authentication/register', { username, email, password })
}

export function googleLogin(credential: string): Promise<ApiResponse<LoginResult>> {
  return postJson('/api/authentication/google', { credential })
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
