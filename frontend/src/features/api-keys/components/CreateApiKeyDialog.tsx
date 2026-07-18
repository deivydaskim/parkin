import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Check, Copy } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
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
        <Button>New API key</Button>
      </DialogTrigger>
      <DialogContent>
        {rawKey ? (
          <>
            <DialogHeader>
              <DialogTitle>API key created</DialogTitle>
              <DialogDescription>
                Copy this key now — it will not be shown again.
              </DialogDescription>
            </DialogHeader>

            <div className="flex items-center gap-2">
              <Input readOnly value={rawKey} className="font-mono text-sm" />
              <Button
                type="button"
                variant="outline"
                size="icon-sm"
                onClick={handleCopy}
                aria-label="Copy API key"
              >
                {copied ? <Check /> : <Copy />}
              </Button>
            </div>

            <Button onClick={() => handleOpenChange(false)}>Done</Button>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Generate API key</DialogTitle>
            </DialogHeader>

            <Form {...form}>
              <form
                onSubmit={form.handleSubmit(onSubmit)}
                className="space-y-4"
              >
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

                <Button
                  type="submit"
                  className="w-full"
                  disabled={createApiKeyMutation.isPending}
                >
                  {createApiKeyMutation.isPending
                    ? 'Generating…'
                    : 'Generate key'}
                </Button>
              </form>
            </Form>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
