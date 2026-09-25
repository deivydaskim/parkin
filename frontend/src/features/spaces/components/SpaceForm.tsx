import { useEffect } from 'react'
import {
  useForm,
  useWatch,
  type Control,
  type FieldPath,
} from 'react-hook-form'
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
  SpaceType,
  defaultPlacement,
  spaceFormSchema,
  type SpaceFormInput,
} from '../schemas'
import { SpaceTypeSelect } from './SpaceTypeSelect'

const emptyDefaults: SpaceFormInput = {
  label: '',
  type: SpaceType.General,
  zone: '',
  isPlaced: false,
  placement: defaultPlacement,
}

type Props = {
  defaultValues?: Partial<SpaceFormInput>
  onSubmit: (values: SpaceFormInput) => void
  isSubmitting?: boolean
  /** Raw mutation error — used to surface field-specific server validation (e.g. duplicate label). */
  error?: unknown
  submitLabel?: string
}

const serverFieldMap: Record<string, FieldPath<SpaceFormInput>> = {
  label: 'label',
  type: 'type',
  zone: 'zone',
  placement: 'placement.x',
  x: 'placement.x',
  y: 'placement.y',
  rotationDegrees: 'placement.rotationDegrees',
  level: 'placement.level',
  width: 'placement.width',
  length: 'placement.length',
}

function toFormField(serverKey: string) {
  const lastSegment = serverKey.split('.').at(-1) ?? serverKey
  const normalized = lastSegment.charAt(0).toLowerCase() + lastSegment.slice(1)
  return serverFieldMap[normalized]
}

export function SpaceForm({
  defaultValues,
  onSubmit,
  isSubmitting = false,
  error,
  submitLabel = 'Save',
}: Props) {
  const form = useForm<SpaceFormInput>({
    resolver: zodResolver(spaceFormSchema),
    defaultValues: { ...emptyDefaults, ...defaultValues },
  })
  const isPlaced = useWatch({ control: form.control, name: 'isPlaced' })

  useEffect(() => {
    if (!(error instanceof AxiosError) || error.response?.status !== 400) return
    const errors = error.response.data?.errors as
      Record<string, string[]> | undefined
    if (!errors) return

    for (const [key, messages] of Object.entries(errors)) {
      const field = toFormField(key)
      const message = messages[0]
      if (field && message) {
        form.setError(field, { type: 'server', message })
      }
    }
  }, [error, form])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="label"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Label</FormLabel>
              <FormControl>
                <Input autoComplete="off" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="type"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Type</FormLabel>
              <FormControl>
                <SpaceTypeSelect
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
          name="zone"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Zone</FormLabel>
              <FormControl>
                <Input
                  autoComplete="off"
                  placeholder="e.g. North, Level 2"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <fieldset className="space-y-3 rounded-md border p-3">
          <FormField
            control={form.control}
            name="isPlaced"
            render={({ field }) => (
              <FormItem className="flex items-center justify-between gap-3">
                <div>
                  <FormLabel>Placement on the lot map</FormLabel>
                  <p className="text-xs text-muted-foreground">
                    Metres from the lot origin; bay centre point.
                  </p>
                </div>
                <FormControl>
                  <Switch
                    checked={field.value}
                    onCheckedChange={field.onChange}
                    aria-label="Place this space on the lot map"
                  />
                </FormControl>
              </FormItem>
            )}
          />

          {isPlaced ? (
            <div className="grid grid-cols-2 gap-3">
              <NumberField
                control={form.control}
                name="placement.x"
                label="X (m)"
                step={0.1}
              />
              <NumberField
                control={form.control}
                name="placement.y"
                label="Y (m)"
                step={0.1}
              />
              <NumberField
                control={form.control}
                name="placement.rotationDegrees"
                label="Rotation (°)"
                step={15}
              />
              <NumberField
                control={form.control}
                name="placement.level"
                label="Level"
                step={1}
              />
              <NumberField
                control={form.control}
                name="placement.width"
                label="Width (m)"
                step={0.1}
              />
              <NumberField
                control={form.control}
                name="placement.length"
                label="Length (m)"
                step={0.1}
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

type NumberFieldProps = {
  control: Control<SpaceFormInput>
  name:
    | 'placement.x'
    | 'placement.y'
    | 'placement.rotationDegrees'
    | 'placement.level'
    | 'placement.width'
    | 'placement.length'
  label: string
  step: number
}

function NumberField({ control, name, label, step }: NumberFieldProps) {
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
              step={step}
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
