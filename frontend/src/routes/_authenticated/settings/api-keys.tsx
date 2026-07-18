import { createFileRoute } from '@tanstack/react-router'
import { ApiKeyTable } from '@/features/api-keys/components/ApiKeyTable'
import { CreateApiKeyDialog } from '@/features/api-keys/components/CreateApiKeyDialog'
import { useApiKeys } from '@/features/api-keys/queries'

export const Route = createFileRoute('/_authenticated/settings/api-keys')({
  component: ApiKeysPage,
})

function ApiKeysPage() {
  const { data, isLoading } = useApiKeys()

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">API keys</h1>
        <CreateApiKeyDialog />
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <ApiKeyTable apiKeys={data ?? []} />
      )}
    </div>
  )
}
