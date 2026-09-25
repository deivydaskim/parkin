import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, LogIn, LogOut } from 'lucide-react'
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
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import { cn } from '@/lib/utils'
import { useManualEvent } from '../queries'
import {
  Direction,
  manualEventFormSchema,
  type Direction as DirectionValue,
  type ManualEventFormInput,
} from '../schemas'
import { DecisionCard } from './DecisionCard'

type Props = {
  lotId: string
  size?: 'default' | 'large'
  disabled?: boolean
}

export function ManualEntryForm({ lotId, size = 'default', disabled }: Props) {
  const manualEventMutation = useManualEvent(lotId)
  const large = size === 'large'

  const form = useForm<ManualEventFormInput>({
    resolver: zodResolver(manualEventFormSchema),
    defaultValues: { plate: '', direction: Direction.Enter },
  })

  function handleSubmit(values: ManualEventFormInput) {
    manualEventMutation.mutate(values, {
      onSuccess: () => {
        form.reset({ plate: '', direction: values.direction })
        form.setFocus('plate')
      },
    })
  }

  const result = manualEventMutation.data
  const submitted = manualEventMutation.variables
  const isBusy = manualEventMutation.isPending || disabled

  return (
    <div className={cn('space-y-4', large && 'space-y-6')}>
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className={cn(
            'grid gap-4',
            large
              ? 'grid-cols-1'
              : 'sm:grid-cols-[minmax(0,1fr)_auto_auto] sm:items-end',
          )}
        >
          <FormField
            control={form.control}
            name="plate"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Plate number</FormLabel>
                <FormControl>
                  <Input
                    placeholder="ABC 123"
                    autoComplete="off"
                    autoCapitalize="characters"
                    spellCheck={false}
                    autoFocus={large}
                    disabled={disabled}
                    className={cn(
                      'font-mono font-semibold tracking-widest uppercase',
                      large && 'h-16 text-center text-3xl md:text-3xl',
                    )}
                    {...field}
                    onChange={(event) =>
                      field.onChange(event.target.value.toUpperCase())
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="direction"
            render={({ field }) => (
              <FormItem>
                <FormLabel className={cn(!large && 'sm:sr-only')}>
                  Direction
                </FormLabel>
                <FormControl>
                  <ToggleGroup
                    type="single"
                    variant="outline"
                    size={large ? 'lg' : 'default'}
                    value={field.value}
                    onValueChange={(next) => {
                      if (next) field.onChange(next as DirectionValue)
                    }}
                    disabled={disabled}
                    className={cn('w-full', large && '[&>*]:h-12')}
                    aria-label="Direction"
                  >
                    <ToggleGroupItem
                      value={Direction.Enter}
                      className="flex-1 data-[state=on]:bg-primary data-[state=on]:text-primary-foreground"
                    >
                      <LogIn />
                      Enter
                    </ToggleGroupItem>
                    <ToggleGroupItem
                      value={Direction.Exit}
                      className="flex-1 data-[state=on]:bg-primary data-[state=on]:text-primary-foreground"
                    >
                      <LogOut />
                      Exit
                    </ToggleGroupItem>
                  </ToggleGroup>
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <Button
            type="submit"
            size="lg"
            disabled={isBusy}
            className={cn(large && 'h-14 text-base')}
          >
            {manualEventMutation.isPending ? (
              <>
                <Loader2 className="animate-spin" />
                Recording…
              </>
            ) : (
              'Record'
            )}
          </Button>
        </form>
      </Form>

      {result && submitted ? (
        <DecisionCard
          decision={result}
          input={submitted}
          size={large ? 'default' : 'compact'}
        />
      ) : null}
    </div>
  )
}
