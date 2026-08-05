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
import {
  auditEntityTypes,
  auditFiltersSchema,
  type AuditFilters as AuditFiltersInput,
} from '../schemas'

type Props = {
  defaultValues: AuditFiltersInput
  onApply: (filters: AuditFiltersInput) => void
  onClear: () => void
}

const ANY_VALUE = 'ANY'

export function AuditFilters({ defaultValues, onApply, onClear }: Props) {
  const form = useForm<AuditFiltersInput>({
    resolver: zodResolver(auditFiltersSchema),
    defaultValues,
  })

  function handleSubmit(values: AuditFiltersInput) {
    onApply(values)
  }

  function handleClear() {
    form.reset({ from: '', to: '', actor: '', actorType: '', entity: '' })
    onClear()
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(handleSubmit)}
        className="flex flex-wrap items-end gap-3"
      >
        <FormField
          control={form.control}
          name="from"
          render={({ field }) => (
            <FormItem>
              <FormLabel>From</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="to"
          render={({ field }) => (
            <FormItem>
              <FormLabel>To</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="actor"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Actor ID</FormLabel>
              <FormControl>
                <Input
                  placeholder="Staff/API-key GUID"
                  className="w-64"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="actorType"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Actor type</FormLabel>
              <Select
                value={field.value || ANY_VALUE}
                onValueChange={(value) =>
                  field.onChange(value === ANY_VALUE ? '' : value)
                }
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Any" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value={ANY_VALUE}>Any</SelectItem>
                  <SelectItem value="Staff">Staff</SelectItem>
                  <SelectItem value="System">System</SelectItem>
                  <SelectItem value="Api">Api</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="entity"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Entity type</FormLabel>
              <Select
                value={field.value || ANY_VALUE}
                onValueChange={(value) =>
                  field.onChange(value === ANY_VALUE ? '' : value)
                }
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Any" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value={ANY_VALUE}>Any</SelectItem>
                  {auditEntityTypes.map((entityType) => (
                    <SelectItem key={entityType} value={entityType}>
                      {entityType}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex gap-2">
          <Button type="submit">Apply filters</Button>
          <Button type="button" variant="outline" onClick={handleClear}>
            Clear
          </Button>
        </div>
      </form>
    </Form>
  )
}
