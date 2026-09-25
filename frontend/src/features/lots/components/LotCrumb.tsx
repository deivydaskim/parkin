import { useParams } from '@tanstack/react-router'
import { useLot } from '../queries'

export function LotCrumb() {
  const { lotId } = useParams({ strict: false })
  const { data: lot } = useLot(lotId ?? '')
  return <>{lot?.name ?? 'Lot'}</>
}
