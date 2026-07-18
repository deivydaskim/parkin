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
import { useLots } from '@/features/lots/queries'
import { grantFormSchema, type GrantFormInput } from '../schemas'

type Props = {
  onSubmit: (values: GrantFormInput) => void
  isSubmitting?: boolean
}

export function GrantForm({ onSubmit, isSubmitting = false }: Props) {
  const { data: lotsData } = useLots({ perPage: 100, status: 'Active' })

  const form = useForm<GrantFormInput>({
    resolver: zodResolver(grantFormSchema),
    defaultValues: { lotId: '', validFrom: '', validTo: '' },
  })

  function handleSubmit(values: GrantFormInput) {
    onSubmit(values)
    form.reset()
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(handleSubmit)}
        className="flex items-end gap-2"
      >
        <FormField
          control={form.control}
          name="lotId"
          render={({ field }) => (
            <FormItem className="flex-1">
              <FormLabel>Lot</FormLabel>
              <Select value={field.value} onValueChange={field.onChange}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a lot" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {(lotsData?.items ?? []).map((lot) => (
                    <SelectItem key={lot.id} value={lot.id}>
                      {lot.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="validFrom"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Valid from</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="validTo"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Valid to</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Granting…' : 'Grant access'}
        </Button>
      </form>
    </Form>
  )
}
