import { Link } from '@tanstack/react-router'
import { RoleGate } from '@/features/auth/components/RoleGate'

export function AppNav() {
  return (
    <nav className="flex items-center gap-4 border-b px-6 py-3">
      <Link to="/" className="text-sm font-medium hover:underline">
        Dashboard
      </Link>

      <Link to="/lots" className="text-sm font-medium hover:underline">
        Parking lots
      </Link>

      <Link to="/drivers" className="text-sm font-medium hover:underline">
        Drivers
      </Link>

      <RoleGate roles={['SystemAdmin']}>
        <Link to="/settings/users" className="text-sm font-medium hover:underline">
          Settings
        </Link>
      </RoleGate>
    </nav>
  )
}
