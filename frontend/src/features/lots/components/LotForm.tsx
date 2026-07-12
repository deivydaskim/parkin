import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { AxiosError } from 'axios'
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
import { lotFormSchema, type LotFormInput } from '../schemas'
import { AccessModeToggle } from './AccessModeToggle'
import { FullBehaviorSelect } from './FullBehaviorSelect'

const emptyDefaults: LotFormInput = {
  name: '',
  address: '',
  timezone: '',
  accessMode: 'OPEN',
  fullBehavior: 'BLOCK',
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
                <Input
                  autoComplete="off"
                  placeholder="America/New_York"
                  {...field}
                />
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

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
      </form>
    </Form>
  )
}
