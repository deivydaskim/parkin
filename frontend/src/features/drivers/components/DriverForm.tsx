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
import { driverFormSchema, type DriverFormInput } from '../schemas'

const emptyDefaults: DriverFormInput = {
  name: '',
  contact: '',
}

type Props = {
  defaultValues?: Partial<DriverFormInput>
  onSubmit: (values: DriverFormInput) => void
  isSubmitting?: boolean
  /** Raw mutation error — used to surface field-specific server validation. */
  error?: unknown
  submitLabel?: string
}

export function DriverForm({
  defaultValues,
  onSubmit,
  isSubmitting = false,
  error,
  submitLabel = 'Save',
}: Props) {
  const form = useForm<DriverFormInput>({
    resolver: zodResolver(driverFormSchema),
    defaultValues: { ...emptyDefaults, ...defaultValues },
  })

  useEffect(() => {
    if (!(error instanceof AxiosError) || error.response?.status !== 400) return
    const errors = error.response.data?.errors as
      Record<string, string[]> | undefined
    if (!errors) return

    for (const [key, messages] of Object.entries(errors)) {
      const field = (key.charAt(0).toLowerCase() +
        key.slice(1)) as keyof DriverFormInput
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
          name="contact"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Contact</FormLabel>
              <FormControl>
                <Input autoComplete="off" {...field} />
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
