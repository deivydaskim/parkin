import { useEffect } from 'react'
import { useForm, useWatch, type Control } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { AxiosError } from 'axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Switch } from '@/components/ui/switch'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  AccessMode,
  FullBehavior,
  defaultLotLayout,
  lotFormSchema,
  type LotFormInput,
} from '../schemas'
import { AccessModeToggle } from './AccessModeToggle'
import { FullBehaviorSelect } from './FullBehaviorSelect'
import { TimezoneSelect } from './TimezoneSelect'

const emptyDefaults: LotFormInput = {
  name: '',
  address: '',
  timezone: '',
  accessMode: AccessMode.Open,
  fullBehavior: FullBehavior.Block,
  hasLayout: false,
  layout: defaultLotLayout,
}

type Props = {
  defaultValues?: Partial<LotFormInput>
  onSubmit: (values: LotFormInput) => void
  isSubmitting?: boolean
  /** Raw mutation error — used to surface field-specific server validation (e.g. duplicate name). */
  error?: unknown
  submitLabel?: string
}

export function LotForm({
  defaultValues,
  onSubmit,
  isSubmitting = false,
  error,
  submitLabel = 'Save',
}: Props) {
  const form = useForm<LotFormInput>({
    resolver: zodResolver(lotFormSchema),
    defaultValues: { ...emptyDefaults, ...defaultValues },
  })
  const hasLayout = useWatch({ control: form.control, name: 'hasLayout' })

  useEffect(() => {
    if (!(error instanceof AxiosError) || error.response?.status !== 400) return
    const errors = error.response.data?.errors as
      Record<string, string[]> | undefined
    if (!errors) return

    for (const [key, messages] of Object.entries(errors)) {
      const field = (key.charAt(0).toLowerCase() +
        key.slice(1)) as keyof LotFormInput
      const message = messages[0]
      if (field in emptyDefaults && message) {
        form.setError(field, { type: 'server', message })
      }
    }
  }, [error, form])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input autoComplete="off" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="address"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Address</FormLabel>
              <FormControl>
                <Input autoComplete="off" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="timezone"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Timezone</FormLabel>
              <FormControl>
                <TimezoneSelect value={field.value} onChange={field.onChange} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="accessMode"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Access mode</FormLabel>
              <FormControl>
                <AccessModeToggle
                  value={field.value}
                  onChange={field.onChange}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="fullBehavior"
          render={({ field }) => (
            <FormItem>
              <FormLabel>When full</FormLabel>
              <FormControl>
                <FullBehaviorSelect
                  value={field.value}
                  onChange={field.onChange}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <fieldset className="space-y-3 rounded-md border p-3">
          <FormField
            control={form.control}
            name="hasLayout"
            render={({ field }) => (
              <FormItem className="flex items-center justify-between gap-3">
                <div>
                  <FormLabel>Lot footprint</FormLabel>
                  <p className="text-xs text-muted-foreground">
                    Ground size and levels for the 3D view. Optional.
                  </p>
                </div>
                <FormControl>
                  <Switch
                    checked={field.value}
                    onCheckedChange={field.onChange}
                    aria-label="Define the lot footprint"
                  />
                </FormControl>
              </FormItem>
            )}
          />

          {hasLayout ? (
            <div className="grid grid-cols-3 gap-3">
              <LayoutNumberField
                control={form.control}
                name="layout.widthMeters"
                label="Width (m)"
              />
              <LayoutNumberField
                control={form.control}
                name="layout.lengthMeters"
                label="Length (m)"
              />
              <LayoutNumberField
                control={form.control}
                name="layout.levelCount"
                label="Levels"
              />
            </div>
          ) : null}
        </fieldset>

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
      </form>
    </Form>
  )
}

type LayoutNumberFieldProps = {
  control: Control<LotFormInput>
  name: 'layout.widthMeters' | 'layout.lengthMeters' | 'layout.levelCount'
  label: string
}

function LayoutNumberField({ control, name, label }: LayoutNumberFieldProps) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <FormLabel>{label}</FormLabel>
          <FormControl>
            <Input
              type="number"
              inputMode="decimal"
              step={name === 'layout.levelCount' ? 1 : 0.5}
              name={field.name}
              ref={field.ref}
              onBlur={field.onBlur}
              value={Number.isNaN(field.value) ? '' : field.value}
              onChange={(event) => field.onChange(event.target.valueAsNumber)}
            />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  )
}
