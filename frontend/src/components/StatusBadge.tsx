import {
  Archive,
  Ban,
  CircleCheck,
  CircleDashed,
  CircleOff,
  Clock,
  EyeOff,
  Lock,
  LockOpen,
  ParkingSquare,
  ShieldCheck,
  Star,
  UserRound,
  XCircle,
  type LucideIcon,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

type Tone = 'success' | 'warning' | 'destructive' | 'info' | 'primary' | 'muted'

const toneClasses: Record<Tone, string> = {
  success:
    'border-success/30 bg-success/12 text-success dark:bg-success/15',
  warning:
    'border-warning/40 bg-warning/20 text-warning-foreground dark:text-warning',
  destructive:
    'border-destructive/30 bg-destructive/10 text-destructive dark:bg-destructive/15',
  info: 'border-info/30 bg-info/12 text-info',
  primary: 'border-primary/30 bg-primary/10 text-primary',
  muted: 'border-border bg-muted text-muted-foreground',
}

const statusConfig = {
  Active: { tone: 'success', icon: CircleCheck, label: 'Active' },
  Archived: { tone: 'muted', icon: Archive, label: 'Archived' },
  Inactive: { tone: 'muted', icon: CircleDashed, label: 'Inactive' },
  Disabled: { tone: 'destructive', icon: Ban, label: 'Disabled' },
  Revoked: { tone: 'destructive', icon: XCircle, label: 'Revoked' },
  Expired: { tone: 'muted', icon: Clock, label: 'Expired' },
  Anonymized: { tone: 'muted', icon: EyeOff, label: 'Anonymized' },
  Cancelled: { tone: 'muted', icon: CircleOff, label: 'Cancelled' },
  Allow: { tone: 'success', icon: CircleCheck, label: 'Allowed' },
  Deny: { tone: 'destructive', icon: XCircle, label: 'Denied' },
  Open: { tone: 'info', icon: LockOpen, label: 'Open' },
  Restricted: { tone: 'warning', icon: Lock, label: 'Restricted' },
  General: { tone: 'primary', icon: ParkingSquare, label: 'General' },
  Reserved: { tone: 'warning', icon: Star, label: 'Reserved' },
  SystemAdmin: { tone: 'primary', icon: ShieldCheck, label: 'System admin' },
  Operator: { tone: 'muted', icon: UserRound, label: 'Operator' },
} satisfies Record<string, { tone: Tone; icon: LucideIcon; label: string }>

export type BadgeStatus = keyof typeof statusConfig

type Props = {
  status: BadgeStatus
  label?: string
  className?: string
  hideIcon?: boolean
}

export function StatusBadge({ status, label, className, hideIcon }: Props) {
  const config = statusConfig[status]
  const Icon = config.icon

  return (
    <Badge
      variant="outline"
      className={cn(toneClasses[config.tone], className)}
    >
      {hideIcon ? null : <Icon aria-hidden />}
      {label ?? config.label}
    </Badge>
  )
}
