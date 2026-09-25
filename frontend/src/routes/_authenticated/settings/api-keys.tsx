import { createFileRoute } from '@tanstack/react-router'
import { KeyRound } from 'lucide-react'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { ListSkeleton } from '@/components/Skeletons'
import { ApiKeyTable } from '@/features/api-keys/components/ApiKeyTable'
import { CreateApiKeyDialog } from '@/features/api-keys/components/CreateApiKeyDialog'
import { useApiKeys } from '@/features/api-keys/queries'

export const Route = createFileRoute('/_authenticated/settings/api-keys')({
  staticData: {
    crumb: 'API keys',
    parentCrumbs: [{ label: 'Administration' }],
  },
  component: ApiKeysPage,
})

function ApiKeysPage() {
  const { data, isLoading } = useApiKeys()
  const apiKeys = data ?? []

  return (
    <div className="space-y-6">
      <PageHeader
        title="API keys"
        description="Credentials for gates, plate readers and other integrations that post access events."
        actions={<CreateApiKeyDialog />}
      />

      {isLoading ? (
        <ListSkeleton rows={3} />
      ) : apiKeys.length === 0 ? (
        <EmptyState
          icon={KeyRound}
          title="No API keys yet"
          hint="Create one for each gate or plate reader so its events can be traced and revoked on their own."
          action={<CreateApiKeyDialog />}
        />
      ) : (
        <ApiKeyTable apiKeys={apiKeys} />
      )}
    </div>
  )
}
