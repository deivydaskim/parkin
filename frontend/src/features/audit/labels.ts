import type {
  AuditActorType,
  AuditEntityType,
  AuditLogEntry,
} from './schemas'

const subjectLabels: Record<string, string> = {
  user: 'Staff user',
  lot: 'Lot',
  space: 'Space',
  driver: 'Driver',
  plate: 'Plate',
  grant: 'Grant',
  api_key: 'API key',
  reservation: 'Reservation',
  access_event: 'Access event',
}

const verbLabels: Record<string, string> = {
  create: 'created',
  change_role: 'role changed',
  disable: 'disabled',
  enable: 'enabled',
  revoke: 'revoked',
  layout_applied: 'layout applied',
  ingested: 'recorded',
}

function words(value: string) {
  return value.replaceAll('_', ' ')
}

export function humanizeAction(action: string) {
  const [subject = '', verb = ''] = action.split('.')
  const subjectLabel = subjectLabels[subject] ?? capitalize(words(subject))
  const verbLabel = verbLabels[verb] ?? words(verb)
  return verb ? `${subjectLabel} ${verbLabel}` : subjectLabel
}

function capitalize(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

export const entityTypeLabels: Record<AuditEntityType, string> = {
  User: 'Staff user',
  ParkingLot: 'Parking lot',
  ParkingSpace: 'Parking space',
  Driver: 'Driver',
  Plate: 'Plate',
  AccessGrant: 'Access grant',
  ApiKey: 'API key',
  Reservation: 'Reservation',
}

export function entityTypeLabel(entityType: string) {
  return entityTypeLabels[entityType as AuditEntityType] ?? entityType
}

export const actorTypeLabels: Record<AuditActorType, string> = {
  Staff: 'Staff',
  System: 'System',
  Api: 'API key',
}

export function actorDisplayName(entry: AuditLogEntry) {
  if (entry.actorName) return entry.actorName
  if (entry.actorType === 'System') return 'System'
  return entry.actorId ? `${entry.actorId.slice(0, 8)}…` : 'Unknown'
}
