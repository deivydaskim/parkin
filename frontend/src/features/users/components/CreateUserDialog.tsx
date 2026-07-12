import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { AxiosError } from 'axios'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
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
import { useCreateUser } from '../queries'
import { createUserSchema, type CreateUserInput } from '../schemas'
import { RoleSelect } from './RoleSelect'

const emptyDefaults: CreateUserInput = {
  email: '',
  password: '',
  displayName: '',
  role: 'Operator',
}

export function CreateUserDialog() {
  const [open, setOpen] = useState(false)
  const createUserMutation = useCreateUser()

  const form = useForm<CreateUserInput>({
    resolver: zodResolver(createUserSchema),
    defaultValues: emptyDefaults,
  })

  function handleOpenChange(next: boolean) {
    setOpen(next)
    if (!next) {
      form.reset(emptyDefaults)
      createUserMutation.reset()
    }
  }

  function onSubmit(values: CreateUserInput) {
    createUserMutation.mutate(values, {
      onSuccess: () => handleOpenChange(false),
      onError: (error) => {
        if (!(error instanceof AxiosError) || error.response?.status !== 400) return
        const errors = error.response.data?.errors as
          Record<string, string[]> | undefined
        if (!errors) return

        for (const [key, messages] of Object.entries(errors)) {
          const field = (key.charAt(0).toLowerCase() +
            key.slice(1)) as keyof CreateUserInput
          const message = messages[0]
          if (field in emptyDefaults && message) {
            form.setError(field, { type: 'server', message })
          }
        }
      },
    })
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button>New user</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create staff account</DialogTitle>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Email</FormLabel>
                  <FormControl>
                    <Input type="email" autoComplete="off" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Password</FormLabel>
                  <FormControl>
                    <Input
                      type="password"
                      autoComplete="new-password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="displayName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Display name</FormLabel>
                  <FormControl>
                    <Input autoComplete="off" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="role"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Role</FormLabel>
                  <FormControl>
                    <RoleSelect value={field.value} onChange={field.onChange} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <Button
              type="submit"
              className="w-full"
              disabled={createUserMutation.isPending}
            >
              {createUserMutation.isPending ? 'Creating…' : 'Create user'}
            </Button>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
