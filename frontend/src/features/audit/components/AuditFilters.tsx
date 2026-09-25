import { useState } from 'react'
import { ChevronDown, Filter, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useUsers } from '@/features/users/queries'
import { cn } from '@/lib/utils'
import { actorTypeLabels, entityTypeLabels } from '../labels'
import {
  auditActorTypeSchema,
  auditEntityTypes,
  type AuditFilters as AuditFiltersValue,
} from '../schemas'

type Props = {
  value: AuditFiltersValue
  onChange: (next: AuditFiltersValue) => void
}

const ANY = '__any__'

function activeCount(filters: AuditFiltersValue) {
  return Object.values(filters).filter(Boolean).length
}

export function AuditFilters({ value, onChange }: Props) {
  const [open, setOpen] = useState(activeCount(value) > 0)
  const { data: usersData } = useUsers({ perPage: 100 })
  const staff = usersData?.items ?? []
  const count = activeCount(value)
  const actorIsKnown = !value.actor || staff.some((user) => user.id === value.actor)

  function update(patch: Partial<AuditFiltersValue>) {
    onChange({ ...value, ...patch })
  }

  return (
    <div className="rounded-xl border bg-card">
      <div className="flex items-center justify-between gap-2 p-3">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setOpen((current) => !current)}
          aria-expanded={open}
        >
          <Filter />
          Filters
          {count > 0 ? (
            <span className="rounded-full bg-primary px-1.5 text-xs text-primary-foreground tabular-nums">
              {count}
            </span>
          ) : null}
          <ChevronDown
            className={cn('transition-transform', open && 'rotate-180')}
          />
        </Button>
        {count > 0 ? (
          <Button
            variant="ghost"
            size="sm"
            onClick={() =>
              onChange({ from: '', to: '', actor: '', actorType: '', entity: '' })
            }
          >
            <X />
            Clear all
          </Button>
        ) : null}
      </div>

      {open ? (
        <div className="grid gap-4 border-t p-4 sm:grid-cols-2 lg:grid-cols-5">
          <div className="space-y-2">
            <Label htmlFor="audit-from">From</Label>
            <Input
              id="audit-from"
              type="date"
              value={value.from ?? ''}
              max={value.to || undefined}
              onChange={(event) => update({ from: event.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="audit-to">To</Label>
            <Input
              id="audit-to"
              type="date"
              value={value.to ?? ''}
              min={value.from || undefined}
              onChange={(event) => update({ to: event.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="audit-actor">Staff member</Label>
            <Select
              value={value.actor || ANY}
              onValueChange={(next) => update({ actor: next === ANY ? '' : next })}
            >
              <SelectTrigger id="audit-actor" className="w-full">
                <SelectValue placeholder="Anyone" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ANY}>Anyone</SelectItem>
                <SelectGroup>
                  <SelectLabel>Staff</SelectLabel>
                  {staff.map((user) => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.displayName}
                    </SelectItem>
                  ))}
                </SelectGroup>
                {!actorIsKnown && value.actor ? (
                  <SelectItem value={value.actor} className="font-mono text-xs">
                    {value.actor}
                  </SelectItem>
                ) : null}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="audit-actor-type">Actor type</Label>
            <Select
              value={value.actorType || ANY}
              onValueChange={(next) =>
                update({
                  actorType: next === ANY ? '' : (next as AuditFiltersValue['actorType']),
                })
              }
            >
              <SelectTrigger id="audit-actor-type" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ANY}>Any</SelectItem>
                {auditActorTypeSchema.options.map((actorType) => (
                  <SelectItem key={actorType} value={actorType}>
                    {actorTypeLabels[actorType]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="audit-entity">Record type</Label>
            <Select
              value={value.entity || ANY}
              onValueChange={(next) =>
                update({
                  entity: next === ANY ? '' : (next as AuditFiltersValue['entity']),
                })
              }
            >
              <SelectTrigger id="audit-entity" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ANY}>Any</SelectItem>
                {auditEntityTypes.map((entityType) => (
                  <SelectItem key={entityType} value={entityType}>
                    {entityTypeLabels[entityType]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
      ) : null}
    </div>
  )
}
