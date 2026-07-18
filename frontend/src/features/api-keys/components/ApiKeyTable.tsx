import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import type { ApiKey } from '../schemas'
import { RevokeApiKeyButton } from './RevokeApiKeyButton'

type Props = {
  apiKeys: ApiKey[]
}

export function ApiKeyTable({ apiKeys }: Props) {
  if (apiKeys.length === 0) {
    return <p className="text-sm text-muted-foreground">No API keys found.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Key</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Created</TableHead>
          <TableHead>Revoked</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {apiKeys.map((apiKey) => (
          <TableRow key={apiKey.id}>
            <TableCell className="font-medium">{apiKey.name}</TableCell>
            <TableCell className="font-mono text-sm">
              {apiKey.prefix}…
            </TableCell>
            <TableCell>{apiKey.status}</TableCell>
            <TableCell>
              {new Date(apiKey.createdAt).toLocaleString()}
            </TableCell>
            <TableCell>
              {apiKey.revokedAt
                ? new Date(apiKey.revokedAt).toLocaleString()
                : '—'}
            </TableCell>
            <TableCell>
              {apiKey.status === 'Active' && (
                <RevokeApiKeyButton apiKeyId={apiKey.id} name={apiKey.name} />
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
