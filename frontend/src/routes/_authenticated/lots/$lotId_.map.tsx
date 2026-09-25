import { createFileRoute } from '@tanstack/react-router'
import { LotView } from '@/features/lot-view/components/LotView'
import { LotCrumb } from '@/features/lots/components/LotCrumb'

function MapCrumb() {
  return (
    <>
      <LotCrumb /> · 3D layout
    </>
  )
}

export const Route = createFileRoute('/_authenticated/lots/$lotId_/map')({
  staticData: {
    crumb: MapCrumb,
    parentCrumbs: [{ label: 'Parking lots', to: '/lots' }],
  },
  component: LotMapPage,
})

function LotMapPage() {
  const { lotId } = Route.useParams()
  return <LotView lotId={lotId} />
}
