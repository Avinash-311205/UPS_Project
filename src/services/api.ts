import type {
  ApiResponse,
  LogisticsSystemResponseDTO,
  CreateLogisticsSystemDTO,
  UpdateLogisticsSystemDTO,
  SystemDependencyResponseDTO,
  CreateSystemDependencyDTO,
  UpdateSystemDependencyDTO,
  BusinessOperationResponseDTO,
  CreateBusinessOperationDTO,
  UpdateBusinessOperationDTO,
  SystemBusinessOperationResponseDTO,
  IncidentResponseDTO,
  CreateIncidentDTO,
  UpdateIncidentDTO,
  UpdateIncidentStatusDTO,
  AddIncidentNoteDTO,
  IncidentAuditLogResponseDTO,
  ImpactAnalysisResponseDTO,
  IamUserResponseDTO,
  CreateIamUserDTO,
  ResetIamUserPasswordDTO,
  RoleResponseDTO,
  AssignIamUserRoleDTO,
  UserStatus,
} from '../types'

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }

  get isConflict(): boolean {
    return this.status === 409
  }

  get isForbidden(): boolean {
    return this.status === 403
  }
}

const TOKEN_KEY = 'resiliencex-auth-token'

function getAuthHeader(): Record<string, string> {
  const token = window.sessionStorage.getItem(TOKEN_KEY)
  if (!token) return {}
  return { Authorization: `Bearer ${token}` }
}

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
  const headers = {
    'Content-Type': 'application/json',
    ...getAuthHeader(),
    ...(options.headers || {}),
  }

  let response: Response
  try {
    response = await fetch(endpoint, {
      ...options,
      headers,
    })
  } catch (error) {
    throw new Error(
      error instanceof Error
        ? `Network communication failure: ${error.message}`
        : 'Network communication failure. Please verify backend availability.'
    )
  }

  let json: ApiResponse<T> | null = null
  try {
    json = (await response.json()) as ApiResponse<T>
  } catch {
    // If not valid JSON, check status
    if (!response.ok) {
      throw new ApiError(
        `Server returned error status ${response.status} (${response.statusText}).`,
        response.status
      )
    }
  }

  if (!response.ok) {
    const errorMsg = json?.message || `Request failed with HTTP status ${response.status}.`
    throw new ApiError(errorMsg, response.status)
  }

  if (!json) {
    throw new Error('Invalid empty response from backend API.')
  }

  return json
}

export const api = {
  // ==========================================
  // SYSTEMS
  // ==========================================
  async getSystems(includeInactive = false): Promise<LogisticsSystemResponseDTO[]> {
    const res = await request<LogisticsSystemResponseDTO[]>(
      `/api/logistics-systems?includeInactive=${includeInactive}`
    )
    return res.data || []
  },

  async getSystemById(id: string): Promise<LogisticsSystemResponseDTO> {
    const res = await request<LogisticsSystemResponseDTO>(`/api/logistics-systems/${id}`)
    if (!res.data) throw new Error(res.message || 'System not found')
    return res.data
  },

  async createSystem(data: CreateLogisticsSystemDTO): Promise<LogisticsSystemResponseDTO> {
    const res = await request<LogisticsSystemResponseDTO>('/api/logistics-systems', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to create system')
    return res.data
  },

  async updateSystem(id: string, data: UpdateLogisticsSystemDTO): Promise<LogisticsSystemResponseDTO> {
    const res = await request<LogisticsSystemResponseDTO>(`/api/logistics-systems/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update system')
    return res.data
  },

  async deactivateSystem(id: string): Promise<boolean> {
    const res = await request<boolean>(`/api/logistics-systems/${id}`, {
      method: 'DELETE',
    })
    return res.success
  },

  // ==========================================
  // SYSTEM DEPENDENCIES
  // ==========================================
  async getDependencies(): Promise<SystemDependencyResponseDTO[]> {
    const res = await request<SystemDependencyResponseDTO[]>('/api/system-dependencies')
    return res.data || []
  },

  async getDependencyById(id: string): Promise<SystemDependencyResponseDTO> {
    const res = await request<SystemDependencyResponseDTO>(`/api/system-dependencies/${id}`)
    if (!res.data) throw new Error(res.message || 'Dependency not found')
    return res.data
  },

  async createDependency(data: CreateSystemDependencyDTO): Promise<SystemDependencyResponseDTO> {
    const res = await request<SystemDependencyResponseDTO>('/api/system-dependencies', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to create dependency')
    return res.data
  },

  async updateDependency(id: string, data: UpdateSystemDependencyDTO): Promise<SystemDependencyResponseDTO> {
    const res = await request<SystemDependencyResponseDTO>(`/api/system-dependencies/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update dependency')
    return res.data
  },

  async deleteDependency(id: string): Promise<boolean> {
    const res = await request<boolean>(`/api/system-dependencies/${id}`, {
      method: 'DELETE',
    })
    return res.success
  },

  // ==========================================
  // BUSINESS OPERATIONS
  // ==========================================
  async getOperations(includeInactive = false): Promise<BusinessOperationResponseDTO[]> {
    const res = await request<BusinessOperationResponseDTO[]>(
      `/api/business-operations?includeInactive=${includeInactive}`
    )
    return res.data || []
  },

  async getOperationById(id: string): Promise<BusinessOperationResponseDTO> {
    const res = await request<BusinessOperationResponseDTO>(`/api/business-operations/${id}`)
    if (!res.data) throw new Error(res.message || 'Operation not found')
    return res.data
  },

  async createOperation(data: CreateBusinessOperationDTO): Promise<BusinessOperationResponseDTO> {
    const res = await request<BusinessOperationResponseDTO>('/api/business-operations', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to create operation')
    return res.data
  },

  async updateOperation(id: string, data: UpdateBusinessOperationDTO): Promise<BusinessOperationResponseDTO> {
    const res = await request<BusinessOperationResponseDTO>(`/api/business-operations/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update operation')
    return res.data
  },

  async deactivateOperation(id: string): Promise<boolean> {
    const res = await request<boolean>(`/api/business-operations/${id}`, {
      method: 'DELETE',
    })
    return res.success
  },

  async assignSystemToOperation(operationId: string, systemId: string): Promise<SystemBusinessOperationResponseDTO> {
    const res = await request<SystemBusinessOperationResponseDTO>(
      `/api/business-operations/${operationId}/systems/${systemId}`,
      { method: 'POST' }
    )
    if (!res.data) throw new Error(res.message || 'Failed to assign system')
    return res.data
  },

  async removeSystemFromOperation(operationId: string, systemId: string): Promise<boolean> {
    const res = await request<boolean>(
      `/api/business-operations/${operationId}/systems/${systemId}`,
      { method: 'DELETE' }
    )
    return res.success
  },

  async getSystemsForOperation(operationId: string): Promise<SystemBusinessOperationResponseDTO[]> {
    const res = await request<SystemBusinessOperationResponseDTO[]>(
      `/api/business-operations/${operationId}/systems`
    )
    return res.data || []
  },

  async getOperationsForSystem(systemId: string): Promise<SystemBusinessOperationResponseDTO[]> {
    const res = await request<SystemBusinessOperationResponseDTO[]>(
      `/api/business-operations/by-system/${systemId}`
    )
    return res.data || []
  },

  // ==========================================
  // INCIDENTS
  // ==========================================
  async getIncidents(filter?: {
    status?: number
    severity?: number
    type?: number
    failedSystemId?: string
  }): Promise<IncidentResponseDTO[]> {
    const params = new URLSearchParams()
    if (filter?.status !== undefined) params.append('status', String(filter.status))
    if (filter?.severity !== undefined) params.append('severity', String(filter.severity))
    if (filter?.type !== undefined) params.append('type', String(filter.type))
    if (filter?.failedSystemId) params.append('failedSystemId', filter.failedSystemId)

    const query = params.toString() ? `?${params.toString()}` : ''
    const res = await request<IncidentResponseDTO[]>(`/api/incidents${query}`)
    return res.data || []
  },

  async getIncidentById(id: string): Promise<IncidentResponseDTO> {
    const res = await request<IncidentResponseDTO>(`/api/incidents/${id}`)
    if (!res.data) throw new Error(res.message || 'Incident not found')
    return res.data
  },

  async createIncident(data: CreateIncidentDTO): Promise<IncidentResponseDTO> {
    const res = await request<IncidentResponseDTO>('/api/incidents', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to report incident')
    return res.data
  },

  async updateIncident(id: string, data: UpdateIncidentDTO): Promise<IncidentResponseDTO> {
    const res = await request<IncidentResponseDTO>(`/api/incidents/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update incident')
    return res.data
  },

  async updateIncidentStatus(id: string, data: UpdateIncidentStatusDTO): Promise<IncidentResponseDTO> {
    const res = await request<IncidentResponseDTO>(`/api/incidents/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to transition incident status')
    return res.data
  },

  async getIncidentImpactAnalysis(incidentId: string): Promise<ImpactAnalysisResponseDTO> {
    const res = await request<ImpactAnalysisResponseDTO>(`/api/incidents/${incidentId}/impact-analysis`)
    if (!res.data) throw new Error(res.message || 'Failed to calculate incident impact analysis')
    return res.data
  },

  async addIncidentNote(
    incidentId: string,
    data: AddIncidentNoteDTO
  ): Promise<IncidentAuditLogResponseDTO> {
    const res = await request<IncidentAuditLogResponseDTO>(`/api/incidents/${incidentId}/notes`, {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to add investigation note')
    return res.data
  },

  async getIncidentAuditLogs(incidentId: string): Promise<IncidentAuditLogResponseDTO[]> {
    const res = await request<IncidentAuditLogResponseDTO[]>(
      `/api/incidents/${incidentId}/audit-logs`
    )
    return res.data || []
  },

  // ==========================================
  // IMPACT ANALYSIS
  // ==========================================
  async getSystemImpactAnalysis(systemId: string): Promise<ImpactAnalysisResponseDTO> {
    const res = await request<ImpactAnalysisResponseDTO>(`/api/impact-analysis/${systemId}`)
    if (!res.data) throw new Error(res.message || 'Failed to calculate impact analysis')
    return res.data
  },

  // ==========================================
  // IAM MANAGEMENT (ROOT-ONLY)
  // ==========================================
  async getIamUsers(): Promise<IamUserResponseDTO[]> {
    const res = await request<IamUserResponseDTO[]>('/api/iam/users')
    return res.data || []
  },

  async createIamUser(data: CreateIamUserDTO): Promise<IamUserResponseDTO> {
    const res = await request<IamUserResponseDTO>('/api/iam/users', {
      method: 'POST',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to provision IAM user')
    return res.data
  },

  async updateIamUserStatus(id: number, status: UserStatus): Promise<IamUserResponseDTO> {
    const res = await request<IamUserResponseDTO>(`/api/iam/users/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update user status')
    return res.data
  },

  async resetIamUserPassword(id: number, data: ResetIamUserPasswordDTO): Promise<string> {
    const res = await request<object>(`/api/iam/users/${id}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(data),
    })
    return res.message || 'Password reset successfully'
  },

  async assignIamUserRole(id: number, data: AssignIamUserRoleDTO): Promise<IamUserResponseDTO> {
    const res = await request<IamUserResponseDTO>(`/api/iam/users/${id}/role`, {
      method: 'PUT',
      body: JSON.stringify(data),
    })
    if (!res.data) throw new Error(res.message || 'Failed to update IAM user role')
    return res.data
  },

  async getAvailableRoles(): Promise<RoleResponseDTO[]> {
    const res = await request<RoleResponseDTO[]>('/api/iam/roles')
    return res.data || []
  },
}
