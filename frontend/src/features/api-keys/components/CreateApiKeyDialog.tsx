import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Check, Copy, KeyRound, Plus, TriangleAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
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
import { useCreateApiKey } from '../queries'
import { createApiKeySchema, type CreateApiKeyInput } from '../schemas'

const emptyDefaults: CreateApiKeyInput = { name: '' }

export function CreateApiKeyDialog() {
  const [open, setOpen] = useState(false)
  const [rawKey, setRawKey] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const createApiKeyMutation = useCreateApiKey()

  const form = useForm<CreateApiKeyInput>({
    resolver: zodResolver(createApiKeySchema),
    defaultValues: emptyDefaults,
  })

  function handleOpenChange(next: boolean) {
    setOpen(next)
    if (!next) {
      form.reset(emptyDefaults)
      createApiKeyMutation.reset()
      setRawKey(null)
      setCopied(false)
    }
  }

  function onSubmit(values: CreateApiKeyInput) {
    createApiKeyMutation.mutate(values, {
      onSuccess: (response) => setRawKey(response.key),
    })
  }

  async function handleCopy() {
    if (!rawKey) return
    await navigator.clipboard.writeText(rawKey)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button>
          <Plus />
          New API key
        </Button>
      </DialogTrigger>
      <DialogContent
        onInteractOutside={(event) => {
          if (rawKey) event.preventDefault()
        }}
      >
        {rawKey ? (
          <>
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <KeyRound className="size-5 text-success" />
                API key created
              </DialogTitle>
              <DialogDescription>
                Configure your gate or integration with this key.
              </DialogDescription>
            </DialogHeader>

            <div className="flex items-start gap-2 rounded-lg border border-warning/50 bg-warning/15 p-3 text-sm">
              <TriangleAlert className="mt-0.5 size-4 shrink-0 text-warning-foreground dark:text-warning" />
              <p>
                <span className="font-semibold">Copy it now.</span> For
                security the full key is never shown again — if you lose it,
                revoke it and create a new one.
              </p>
            </div>

            <div className="flex items-center gap-2">
              <Input
                readOnly
                value={rawKey}
                className="font-mono text-sm"
                onFocus={(event) => event.target.select()}
                aria-label="New API key"
              />
              <Button
                type="button"
                variant={copied ? 'secondary' : 'default'}
                onClick={handleCopy}
              >
                {copied ? <Check /> : <Copy />}
                {copied ? 'Copied' : 'Copy'}
              </Button>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => handleOpenChange(false)}>
                I've saved it
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Generate API key</DialogTitle>
              <DialogDescription>
                Gates and plate readers authenticate with an API key sent as
                the X-Api-Key header.
              </DialogDescription>
            </DialogHeader>

            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Name</FormLabel>
                      <FormControl>
                        <Input
                          autoComplete="off"
                          placeholder="North Garage entry reader"
                          {...field}
                        />
                      </FormControl>
                      <FormDescription>
                        Helps you recognise it in the audit log.
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Button
                  type="submit"
                  className="w-full"
                  disabled={createApiKeyMutation.isPending}
                >
                  {createApiKeyMutation.isPending ? 'Generating…' : 'Generate key'}
                </Button>
              </form>
            </Form>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
