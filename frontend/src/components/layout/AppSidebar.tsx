import { Link, useRouterState, type LinkProps } from '@tanstack/react-router'
import {
  Car,
  DoorOpen,
  KeyRound,
  LayoutDashboard,
  ParkingSquare,
  ScrollText,
  Users,
  type LucideIcon,
} from 'lucide-react'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  useSidebar,
} from '@/components/ui/sidebar'
import { BrandMark } from '@/components/BrandMark'
import { useHasRole } from '@/features/auth/permissions'
import { UserMenu } from './UserMenu'

type NavItem = {
  label: string
  to: LinkProps['to'] & string
  icon: LucideIcon
  exact?: boolean
}

type NavGroup = {
  label: string
  items: NavItem[]
  adminOnly?: boolean
}

const navGroups: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      { label: 'Dashboard', to: '/', icon: LayoutDashboard, exact: true },
      { label: 'Gate console', to: '/gate', icon: DoorOpen },
    ],
  },
  {
    label: 'Manage',
    items: [
      { label: 'Parking lots', to: '/lots', icon: ParkingSquare },
      { label: 'Drivers', to: '/drivers', icon: Car },
    ],
  },
  {
    label: 'Administration',
    adminOnly: true,
    items: [
      { label: 'Staff', to: '/settings/users', icon: Users },
      { label: 'API keys', to: '/settings/api-keys', icon: KeyRound },
      { label: 'Audit log', to: '/settings/audit', icon: ScrollText },
    ],
  },
]

function isActive(pathname: string, item: NavItem) {
  if (item.exact) return pathname === item.to
  return pathname === item.to || pathname.startsWith(`${item.to}/`)
}

export function AppSidebar() {
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const isAdmin = useHasRole('SystemAdmin')
  const { isMobile, setOpenMobile } = useSidebar()

  function handleNavigate() {
    if (isMobile) setOpenMobile(false)
  }

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild className="hover:bg-transparent">
              <Link to="/" onClick={handleNavigate}>
                <BrandMark />
                <div className="grid flex-1 text-left leading-tight">
                  <span className="text-base font-semibold tracking-tight text-sidebar-accent-foreground">
                    Parkin
                  </span>
                  <span className="text-xs text-sidebar-foreground/60">
                    Operator console
                  </span>
                </div>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>

      <SidebarContent>
        {navGroups
          .filter((group) => !group.adminOnly || isAdmin)
          .map((group) => (
            <SidebarGroup key={group.label}>
              <SidebarGroupLabel className="text-sidebar-foreground/50 uppercase tracking-wider">
                {group.label}
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {group.items.map((item) => (
                    <SidebarMenuItem key={item.to}>
                      <SidebarMenuButton
                        asChild
                        isActive={isActive(pathname, item)}
                        tooltip={item.label}
                        className="text-sidebar-foreground/80 data-[active=true]:bg-sidebar-primary data-[active=true]:text-sidebar-primary-foreground data-[active=true]:shadow-sm"
                      >
                        <Link to={item.to} onClick={handleNavigate}>
                          <item.icon />
                          <span>{item.label}</span>
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          ))}
      </SidebarContent>

      <SidebarFooter>
        <UserMenu />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}
