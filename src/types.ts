// ResilienceX — Enterprise Domain Types & API Contracts

export type Page =
  | 'Overview'
  | 'Systems'
  | 'Dependencies'
  | 'Operations'
  | 'Incidents'
  | 'ImpactAnalysis'
  | 'Shipments'
  | 'Continuity'
  | 'Recovery'
  | 'IamManagement'

// ==========================================
// CONSTANTS & TYPES (erasableSyntaxOnly compliant)
// ==========================================

export const SystemType = {
  TMS: 1,
  WMS: 2,
  YMS: 3,
  Telematics: 4,
  EDI: 5,
  ERP: 6,
  Other: 7,
} as const
export type SystemType = (typeof SystemType)[keyof typeof SystemType]

export const SystemCriticality = {
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
} as const
export type SystemCriticality = (typeof SystemCriticality)[keyof typeof SystemCriticality]

export const SystemEnvironment = {
  Development: 1,
  Staging: 2,
  Production: 3,
} as const
export type SystemEnvironment = (typeof SystemEnvironment)[keyof typeof SystemEnvironment]

export const SystemLifecycleStatus = {
  Active: 1,
  Inactive: 2,
} as const
export type SystemLifecycleStatus = (typeof SystemLifecycleStatus)[keyof typeof SystemLifecycleStatus]

export const DependencyType = {
  Mandatory: 1,
  Optional: 2,
} as const
export type DependencyType = (typeof DependencyType)[keyof typeof DependencyType]

export const OperationCriticality = {
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
} as const
export type OperationCriticality = (typeof OperationCriticality)[keyof typeof OperationCriticality]

export const OperationLifecycleStatus = {
  Active: 1,
  Inactive: 2,
} as const
export type OperationLifecycleStatus = (typeof OperationLifecycleStatus)[keyof typeof OperationLifecycleStatus]

export const AvailabilityRule = {
  AllRequired: 1,
  AnyAvailable: 2,
} as const
export type AvailabilityRule = (typeof AvailabilityRule)[keyof typeof AvailabilityRule]

export const IncidentSeverity = {
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
} as const
export type IncidentSeverity = (typeof IncidentSeverity)[keyof typeof IncidentSeverity]

export const IncidentStatus = {
  Open: 1,
  Investigating: 2,
  Mitigating: 3,
  Resolved: 4,
  Closed: 5,
} as const
export type IncidentStatus = (typeof IncidentStatus)[keyof typeof IncidentStatus]

export const IncidentType = {
  CyberAttack: 1,
  Ransomware: 2,
  SystemOutage: 3,
  HardwareFailure: 4,
  NetworkDisruption: 5,
  DataCorruption: 6,
  PowerFailure: 7,
  Other: 8,
} as const
export type IncidentType = (typeof IncidentType)[keyof typeof IncidentType]

export const OperationAvailabilityStatus = {
  PotentiallyAffected: 1,
  RequiresVerification: 2,
  Unknown: 3,
} as const
export type OperationAvailabilityStatus = (typeof OperationAvailabilityStatus)[keyof typeof OperationAvailabilityStatus]

export const OperationImpactSeverity = {
  Low: 1,
  Medium: 2,
  High: 3,
  Critical: 4,
} as const
export type OperationImpactSeverity = (typeof OperationImpactSeverity)[keyof typeof OperationImpactSeverity]

export const ImpactType = {
  Direct: 1,
  Indirect: 2,
} as const
export type ImpactType = (typeof ImpactType)[keyof typeof ImpactType]

// ==========================================
// HUMAN-READABLE LABEL MAPPINGS
// ==========================================

export const systemTypeLabels: Record<SystemType, string> = {
  [SystemType.TMS]: 'TMS (Transport Management)',
  [SystemType.WMS]: 'WMS (Warehouse Management)',
  [SystemType.YMS]: 'YMS (Yard Management)',
  [SystemType.Telematics]: 'Telematics & IoT',
  [SystemType.EDI]: 'EDI Gateway',
  [SystemType.ERP]: 'ERP Enterprise Core',
  [SystemType.Other]: 'Other System',
}

export const systemCriticalityLabels: Record<SystemCriticality, string> = {
  [SystemCriticality.Low]: 'Low',
  [SystemCriticality.Medium]: 'Medium',
  [SystemCriticality.High]: 'High',
  [SystemCriticality.Critical]: 'Critical',
}

export const systemEnvironmentLabels: Record<SystemEnvironment, string> = {
  [SystemEnvironment.Development]: 'Development',
  [SystemEnvironment.Staging]: 'Staging',
  [SystemEnvironment.Production]: 'Production',
}

export const systemLifecycleStatusLabels: Record<SystemLifecycleStatus, string> = {
  [SystemLifecycleStatus.Active]: 'Active',
  [SystemLifecycleStatus.Inactive]: 'Inactive',
}

export const dependencyTypeLabels: Record<DependencyType, string> = {
  [DependencyType.Mandatory]: 'Mandatory',
  [DependencyType.Optional]: 'Optional',
}

export const operationCriticalityLabels: Record<OperationCriticality, string> = {
  [OperationCriticality.Low]: 'Low',
  [OperationCriticality.Medium]: 'Medium',
  [OperationCriticality.High]: 'High',
  [OperationCriticality.Critical]: 'Critical',
}

export const operationLifecycleStatusLabels: Record<OperationLifecycleStatus, string> = {
  [OperationLifecycleStatus.Active]: 'Active',
  [OperationLifecycleStatus.Inactive]: 'Inactive',
}

export const availabilityRuleLabels: Record<AvailabilityRule, string> = {
  [AvailabilityRule.AllRequired]: 'All Required (Strict)',
  [AvailabilityRule.AnyAvailable]: 'Any Available (Failover)',
}

export const incidentSeverityLabels: Record<IncidentSeverity, string> = {
  [IncidentSeverity.Low]: 'Low',
  [IncidentSeverity.Medium]: 'Medium',
  [IncidentSeverity.High]: 'High',
  [IncidentSeverity.Critical]: 'Critical',
}

export const incidentStatusLabels: Record<IncidentStatus, string> = {
  [IncidentStatus.Open]: 'Open',
  [IncidentStatus.Investigating]: 'Investigating',
  [IncidentStatus.Mitigating]: 'Mitigating',
  [IncidentStatus.Resolved]: 'Resolved',
  [IncidentStatus.Closed]: 'Closed',
}

export const incidentTypeLabels: Record<IncidentType, string> = {
  [IncidentType.CyberAttack]: 'Cyber Attack',
  [IncidentType.Ransomware]: 'Ransomware Outage',
  [IncidentType.SystemOutage]: 'System Outage',
  [IncidentType.HardwareFailure]: 'Hardware Failure',
  [IncidentType.NetworkDisruption]: 'Network Disruption',
  [IncidentType.DataCorruption]: 'Data Corruption',
  [IncidentType.PowerFailure]: 'Power Failure',
  [IncidentType.Other]: 'Other Disruption',
}

export const operationAvailabilityStatusLabels: Record<OperationAvailabilityStatus, string> = {
  [OperationAvailabilityStatus.PotentiallyAffected]: 'Potentially Affected',
  [OperationAvailabilityStatus.RequiresVerification]: 'Requires Verification',
  [OperationAvailabilityStatus.Unknown]: 'Unknown Status',
}

export const operationImpactSeverityLabels: Record<OperationImpactSeverity, string> = {
  [OperationImpactSeverity.Low]: 'Low Severity',
  [OperationImpactSeverity.Medium]: 'Medium Severity',
  [OperationImpactSeverity.High]: 'High Severity',
  [OperationImpactSeverity.Critical]: 'Critical Severity',
}

// Legacy types preserved for transition
export type LegacyPage = 'Overview' | 'Systems' | 'Incidents' | 'Shipments' | 'Continuity' | 'Recovery'
export type SystemStatus = 'Operational' | 'Degraded' | 'Isolated' | 'Recovering'
export type ShipmentStatus = 'On track' | 'At risk' | 'Continuity'
export type Modal = 'isolate' | 'approve' | 'decline' | 'restore' | 'reconcile' | null
export type System = { name: string; description: string; status: SystemStatus; latency: string; uptime: string; icon: string }
export type Shipment = { id: string; destination: string; customer: string; eta: string; status: ShipmentStatus; system: string; lastUpdate?: string }

// ==========================================
// API DTO CONTRACTS
// ==========================================

export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T | null
}

// Systems
export interface LogisticsSystemResponseDTO {
  id: string
  tenantId: string
  name: string
  systemType: SystemType
  description?: string | null
  businessPurpose?: string | null
  criticality: SystemCriticality
  environment: SystemEnvironment
  lifecycleStatus: SystemLifecycleStatus
  createdAt: string
  updatedAt?: string | null
}

export interface CreateLogisticsSystemDTO {
  name: string
  systemType: SystemType
  description?: string
  businessPurpose?: string
  criticality: SystemCriticality
  environment: SystemEnvironment
  lifecycleStatus: SystemLifecycleStatus
}

export interface UpdateLogisticsSystemDTO {
  name: string
  systemType: SystemType
  description?: string
  businessPurpose?: string
  criticality: SystemCriticality
  environment: SystemEnvironment
  lifecycleStatus: SystemLifecycleStatus
}

// Dependencies
export interface SystemDependencyResponseDTO {
  id: string
  tenantId: string
  dependentSystemId: string
  dependentSystem?: LogisticsSystemResponseDTO | null
  dependencySystemId: string
  dependencySystem?: LogisticsSystemResponseDTO | null
  dependencyType: DependencyType
  createdAt: string
  updatedAt?: string | null
}

export interface CreateSystemDependencyDTO {
  dependentSystemId: string
  dependencySystemId: string
  dependencyType: DependencyType
}

export interface UpdateSystemDependencyDTO {
  dependencyType: DependencyType
}

// Business Operations
export interface BusinessOperationResponseDTO {
  id: string
  tenantId: string
  name: string
  description?: string | null
  lifecycleStatus: OperationLifecycleStatus
  criticality: OperationCriticality
  availabilityRule: AvailabilityRule
  createdAt: string
  updatedAt?: string | null
  supportingSystemsCount: number
}

export interface CreateBusinessOperationDTO {
  name: string
  description?: string
  criticality: OperationCriticality
  availabilityRule: AvailabilityRule
}

export interface UpdateBusinessOperationDTO {
  name: string
  description?: string
  lifecycleStatus: OperationLifecycleStatus
  criticality: OperationCriticality
  availabilityRule: AvailabilityRule
}

export interface SystemBusinessOperationResponseDTO {
  id: string
  tenantId: string
  logisticsSystemId: string
  logisticsSystemName: string
  businessOperationId: string
  businessOperationName: string
  createdAt: string
}

// Incidents
export interface IncidentResponseDTO {
  id: string
  tenantId: string
  failedSystemId: string
  failedSystemName: string
  failedSystemCriticality: SystemCriticality
  title: string
  description?: string | null
  incidentType: IncidentType
  severity: IncidentSeverity
  status: IncidentStatus
  detectedAt: string
  createdAt: string
  updatedAt?: string | null
  resolvedAt?: string | null
}

export interface CreateIncidentDTO {
  failedSystemId: string
  title: string
  description?: string
  incidentType: IncidentType
  severity: IncidentSeverity
  detectedAt?: string
}

export interface UpdateIncidentDTO {
  title: string
  description?: string
  incidentType: IncidentType
  severity: IncidentSeverity
}

export interface UpdateIncidentStatusDTO {
  status: IncidentStatus
  resolutionNotes?: string
}

// Impact Analysis & Graph
export interface AffectedSystemDTO {
  systemId: string
  systemName: string
  systemType: SystemType
  criticality: SystemCriticality
  environment: SystemEnvironment
  lifecycleStatus: SystemLifecycleStatus
  dependencyType: DependencyType
  depth: number
  dependencyPath: string[]
  dependencyPathIds: string[]
}

export interface ImpactScoringBreakdownDTO {
  systemCriticality: SystemCriticality
  systemCriticalityWeight: number
  systemCriticalityScore: number
  dependencyType: DependencyType
  dependencyTypeWeight: number
  dependencyTypeScore: number
  operationCriticality: OperationCriticality
  operationCriticalityWeight: number
  operationCriticalityScore: number
  totalScore: number
}

export interface AffectedOperationDTO {
  operationId: string
  operationName: string
  operationCriticality: OperationCriticality
  availabilityRule: AvailabilityRule
  availabilityStatus: OperationAvailabilityStatus
  availabilityReason: string
  potentialImpactSeverity: OperationImpactSeverity
  numericSeverityScore: number
  scoringBreakdown: ImpactScoringBreakdownDTO
  impactType: ImpactType
  contributingSystemId: string
  contributingSystemName: string
  contributingDependencyType: DependencyType
  contributingDependencyPath: string[]
  contributingDependencyPathIds: string[]
  severityReason: string
  totalSupportingSystemsCount: number
  affectedSupportingSystemsCount: number
  affectedSupportingSystemNames: string[]
}

export interface ImpactAnalysisResponseDTO {
  failedSystem: LogisticsSystemResponseDTO
  totalAffectedSystemsCount: number
  directDependentsCount: number
  indirectDependentsCount: number
  directDependents: AffectedSystemDTO[]
  indirectDependents: AffectedSystemDTO[]
  totalAffectedOperationsCount: number
  criticalOperationsCount: number
  highOperationsCount: number
  mediumOperationsCount: number
  lowOperationsCount: number
  requiresVerificationOperationsCount: number
  affectedOperations: AffectedOperationDTO[]
}

// ==========================================
// IAM & ROOT ACCOUNT MANAGEMENT TYPES
// ==========================================

export const AccountType = {
  Root: 1,
  IAM: 2,
} as const
export type AccountType = (typeof AccountType)[keyof typeof AccountType]

export const accountTypeLabels: Record<AccountType, string> = {
  [AccountType.Root]: 'Root',
  [AccountType.IAM]: 'IAM User',
}

export const UserStatus = {
  Active: 1,
  Inactive: 2,
} as const
export type UserStatus = (typeof UserStatus)[keyof typeof UserStatus]

export const userStatusLabels: Record<UserStatus, string> = {
  [UserStatus.Active]: 'Active',
  [UserStatus.Inactive]: 'Inactive',
}

export interface IamUserResponseDTO {
  id: number
  username: string
  email: string
  tenantId: string | null
  accountType: AccountType
  status: UserStatus
  createdAt: string
  updatedAt?: string | null
}

export interface CreateIamUserDTO {
  username: string
  email: string
  password: string
}

export interface UpdateIamUserStatusDTO {
  status: UserStatus
}

export interface ResetIamUserPasswordDTO {
  newPassword: string
}

