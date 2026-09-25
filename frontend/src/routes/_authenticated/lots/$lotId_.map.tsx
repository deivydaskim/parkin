import { createFileRoute } from '@tanstack/react-router'
import { LotView } from '@/features/lot-view/components/LotView'

export const Route = createFileRoute('/_authenticated/lots/$lotId_/map')({
  component: LotMapPage,
})

function LotMapPage() {
  const { lotId } = Route.useParams()
  return <LotView lotId={lotId} />
}
