import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Info } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { LotCombobox } from '@/features/lots/components/LotCombobox'
import { useLot } from '@/features/lots/queries'
import { AccessMode } from '@/features/lots/schemas'
import { grantFormSchema, type GrantFormInput } from '../schemas'

type Props = {
  onSubmit: (values: GrantFormInput) => void
  isSubmitting?: boolean
}

export function GrantForm({ onSubmit, isSubmitting = false }: Props) {
  const [lotLabel, setLotLabel] = useState<string | null>(null)
  const form = useForm<GrantFormInput>({
    resolver: zodResolver(grantFormSchema),
    defaultValues: { lotId: '', validFrom: '', validTo: '' },
  })
  const lotId = useWatch({ control: form.control, name: 'lotId' })
  const { data: selectedLot } = useLot(lotId)

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="lotId"
          render={({ field, fieldState }) => (
            <FormItem>
              <FormLabel>Lot</FormLabel>
              <FormControl>
                <LotCombobox
                  value={field.value || null}
                  selectedLabel={lotLabel}
                  onChange={(option) => {
                    setLotLabel(option?.label ?? null)
                    field.onChange(option?.id ?? '')
                  }}
                  aria-invalid={fieldState.invalid}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {selectedLot?.accessMode === AccessMode.Open ? (
          <p className="flex items-start gap-2 rounded-lg border border-info/30 bg-info/10 p-3 text-sm">
            <Info className="mt-0.5 size-4 shrink-0 text-info" aria-hidden />
            <span>
              <span className="font-medium">{selectedLot.name}</span> is an open
              lot, so this grant has no effect until the lot is switched to
              restricted.
            </span>
          </p>
        ) : null}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            control={form.control}
            name="validFrom"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Valid from</FormLabel>
                <FormControl>
                  <Input type="date" {...field} />
                </FormControl>
                <FormDescription>Empty means today.</FormDescription>
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
                <FormDescription>Empty means no end date.</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <Button type="submit" className="w-full" disabled={isSubmitting}>
          {isSubmitting ? 'Granting…' : 'Grant access'}
        </Button>
      </form>
    </Form>
  )
}
