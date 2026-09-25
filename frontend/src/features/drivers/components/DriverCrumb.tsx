import { useParams } from '@tanstack/react-router'
import { useDriver } from '../queries'

export function DriverCrumb() {
  const { driverId } = useParams({ strict: false })
  const { data: driver } = useDriver(driverId ?? '')
  return <>{driver?.name ?? 'Driver'}</>
}
