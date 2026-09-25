import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useManualEvent } from '../queries'
import {
  Decision,
  Direction,
  manualEventFormSchema,
  type AccessEventDecision,
  type DenyReason,
  type ManualEventFormInput,
} from '../schemas'

type Props = {
  lotId: string
}

const denyReasonLabels: Record<DenyReason, string> = {
  NotAuthorized: 'not authorized for this lot',
  LotFull: 'lot is full',
  NoOpenSession: 'no open session for this plate',
  LotArchived: 'lot is archived',
  LotNotFound: 'lot not found',
}

function describeDecision(
  decision: AccessEventDecision,
  input: ManualEventFormInput,
) {
  const action = input.direction === Direction.Enter ? 'Entry' : 'Exit'

  if (decision.decision === Decision.Deny) {
    return `${action} denied for ${input.plate}: ${denyReasonLabels[decision.reason!]}.`
  }

  if (decision.reservedSpaceLabel) {
    return `${action} allowed for ${input.plate} — reserved space ${decision.reservedSpaceLabel}.`
  }

  return `${action} allowed for ${input.plate}${decision.pool ? ` (${decision.pool.toLowerCase()} pool)` : ''}.`
}

export function ManualEntryForm({ lotId }: Props) {
  const manualEventMutation = useManualEvent(lotId)

  const form = useForm<ManualEventFormInput>({
    resolver: zodResolver(manualEventFormSchema),
    defaultValues: { plate: '', direction: Direction.Enter },
  })

  function handleSubmit(values: ManualEventFormInput) {
    manualEventMutation.mutate(values, {
      onSuccess: () => {
        form.reset({ plate: '', direction: values.direction })
      },
    })
  }

  const result = manualEventMutation.data
  const submitted = manualEventMutation.variables

  return (
    <section>
      <h2 className="text-lg font-semibold">Manual entry / exit</h2>
      <p className="mb-4 text-sm text-muted-foreground">
        For plate-reader failures and walk-ups. Runs the same checks as the
        gate.
      </p>

      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex items-end gap-2"
        >
          <FormField
            control={form.control}
            name="plate"
            render={({ field }) => (
              <FormItem className="w-48">
                <FormLabel>Plate</FormLabel>
                <FormControl>
                  <Input placeholder="ABC 123" autoComplete="off" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="direction"
            render={({ field }) => (
              <FormItem className="w-32">
                <FormLabel>Direction</FormLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value={Direction.Enter}>Enter</SelectItem>
                    <SelectItem value={Direction.Exit}>Exit</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />

          <Button type="submit" disabled={manualEventMutation.isPending}>
            {manualEventMutation.isPending ? 'Recording…' : 'Record'}
          </Button>
        </form>
      </Form>

      {result && submitted ? (
        <p
          role="status"
          className={
            result.decision === Decision.Allow
              ? 'mt-3 text-sm font-medium'
              : 'mt-3 text-sm font-medium text-destructive'
          }
        >
          {describeDecision(result, submitted)}
        </p>
      ) : null}
    </section>
  )
}
