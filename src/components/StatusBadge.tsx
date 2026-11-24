import {
  SystemLifecycleStatus,
  SystemCriticality,
  OperationCriticality,
  OperationLifecycleStatus,
  AvailabilityRule,
  IncidentSeverity,
  IncidentStatus,
  DependencyType,
  OperationAvailabilityStatus,
  OperationImpactSeverity,
  systemCriticalityLabels,
  systemLifecycleStatusLabels,
  operationCriticalityLabels,
  operationLifecycleStatusLabels,
  incidentSeverityLabels,
  incidentStatusLabels,
  dependencyTypeLabels,
  operationAvailabilityStatusLabels,
  operationImpactSeverityLabels,
} from '../types'

interface StatusBadgeProps {
  type:
    | 'systemStatus'
    | 'systemCriticality'
    | 'operationStatus'
    | 'operationCriticality'
    | 'availabilityRule'
    | 'incidentStatus'
    | 'incidentSeverity'
    | 'dependencyType'
    | 'availabilityStatus'
    | 'impactSeverity'
  value: number
}

export default function StatusBadge({ type, value }: StatusBadgeProps) {
  switch (type) {
    case 'systemStatus': {
      const active = value === SystemLifecycleStatus.Active
      return (
        <span className={`cc-badge ${active ? 'cc-badge-green' : 'cc-badge-neutral'}`}>
          <span className="cc-pulse-dot" style={{ backgroundColor: active ? 'var(--cc-green)' : '#666666' }} />
          {systemLifecycleStatusLabels[value as SystemLifecycleStatus] || 'Unknown'}
        </span>
      )
    }

    case 'systemCriticality': {
      const crit = value as SystemCriticality
      let colorClass = 'cc-crit-low'
      if (crit === SystemCriticality.Critical) colorClass = 'cc-crit-critical'
      else if (crit === SystemCriticality.High) colorClass = 'cc-crit-high'
      else if (crit === SystemCriticality.Medium) colorClass = 'cc-crit-medium'

      return (
        <span className={`cc-crit-tag ${colorClass}`}>
          {systemCriticalityLabels[crit] || 'Unknown'}
        </span>
      )
    }

    case 'operationStatus': {
      const active = value === OperationLifecycleStatus.Active
      return (
        <span className={`cc-badge ${active ? 'cc-badge-green' : 'cc-badge-neutral'}`}>
          {operationLifecycleStatusLabels[value as OperationLifecycleStatus] || 'Unknown'}
        </span>
      )
    }

    case 'operationCriticality': {
      const crit = value as OperationCriticality
      let colorClass = 'cc-crit-low'
      if (crit === OperationCriticality.Critical) colorClass = 'cc-crit-critical'
      else if (crit === OperationCriticality.High) colorClass = 'cc-crit-high'
      else if (crit === OperationCriticality.Medium) colorClass = 'cc-crit-medium'

      return (
        <span className={`cc-crit-tag ${colorClass}`}>
          {operationCriticalityLabels[crit] || 'Unknown'}
        </span>
      )
    }

    case 'availabilityRule': {
      const rule = value as AvailabilityRule
      const isAll = rule === AvailabilityRule.AllRequired
      return (
        <span className={`cc-badge ${isAll ? 'cc-badge-amber' : 'cc-badge-cyan'}`}>
          {isAll ? 'ALL REQUIRED' : 'ANY AVAILABLE'}
        </span>
      )
    }

    case 'incidentStatus': {
      const status = value as IncidentStatus
      let badgeClass = 'cc-badge-neutral'
      if (status === IncidentStatus.Open) badgeClass = 'cc-badge-red'
      else if (status === IncidentStatus.Investigating) badgeClass = 'cc-badge-amber'
      else if (status === IncidentStatus.Mitigating) badgeClass = 'cc-badge-cyan'
      else if (status === IncidentStatus.Resolved) badgeClass = 'cc-badge-green'

      return (
        <span className={`cc-badge ${badgeClass}`}>
          {incidentStatusLabels[status] || 'Unknown'}
        </span>
      )
    }

    case 'incidentSeverity': {
      const sev = value as IncidentSeverity
      let colorClass = 'cc-crit-low'
      if (sev === IncidentSeverity.Critical) colorClass = 'cc-crit-critical'
      else if (sev === IncidentSeverity.High) colorClass = 'cc-crit-high'
      else if (sev === IncidentSeverity.Medium) colorClass = 'cc-crit-medium'

      return (
        <span className={`cc-crit-tag ${colorClass}`}>
          {incidentSeverityLabels[sev] || 'Unknown'}
        </span>
      )
    }

    case 'dependencyType': {
      const dep = value as DependencyType
      const isMandatory = dep === DependencyType.Mandatory
      return (
        <span className={`cc-badge ${isMandatory ? 'cc-badge-amber' : 'cc-badge-neutral'}`}>
          {dependencyTypeLabels[dep] || 'Unknown'}
        </span>
      )
    }

    case 'availabilityStatus': {
      const avail = value as OperationAvailabilityStatus
      const isPotentiallyAffected = avail === OperationAvailabilityStatus.PotentiallyAffected
      return (
        <span className={`cc-badge ${isPotentiallyAffected ? 'cc-badge-red' : 'cc-badge-amber'}`}>
          {operationAvailabilityStatusLabels[avail] || 'Unknown'}
        </span>
      )
    }

    case 'impactSeverity': {
      const sev = value as OperationImpactSeverity
      let colorClass = 'cc-crit-low'
      if (sev === OperationImpactSeverity.Critical) colorClass = 'cc-crit-critical'
      else if (sev === OperationImpactSeverity.High) colorClass = 'cc-crit-high'
      else if (sev === OperationImpactSeverity.Medium) colorClass = 'cc-crit-medium'

      return (
        <span className={`cc-crit-tag ${colorClass}`}>
          {operationImpactSeverityLabels[sev] || 'Unknown'}
        </span>
      )
    }

    default:
      return <span className="cc-badge cc-badge-neutral">{String(value)}</span>
  }
}
