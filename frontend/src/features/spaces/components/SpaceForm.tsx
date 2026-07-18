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
import { SpaceType, spaceFormSchema, type SpaceFormInput } from '../schemas'
import { SpaceTypeSelect } from './SpaceTypeSelect'

const emptyDefaults: SpaceFormInput = {
  label: '',
  type: SpaceType.General,
}

type Props = {
  defaultValues?: Partial<SpaceFormInput>
  onSubmit: (values: SpaceFormInput) => void
  isSubmitting?: boolean
  /** Raw mutation error — used to surface field-specific server validation (e.g. duplicate label). */
  error?: unknown
  submitLabel?: string
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

  useEffect(() => {
    if (!(error instanceof AxiosError) || error.response?.status !== 400) return
    const errors = error.response.data?.errors as
      Record<string, string[]> | undefined
    if (!errors) return

    for (const [key, messages] of Object.entries(errors)) {
      const field = (key.charAt(0).toLowerCase() +
        key.slice(1)) as keyof SpaceFormInput
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
                <SpaceTypeSelect value={field.value} onChange={field.onChange} />
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
