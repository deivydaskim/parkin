import type { ComponentType } from 'react'
import type { LinkProps } from '@tanstack/react-router'

export type CrumbLink = {
  label: string
  to?: LinkProps['to']
}

declare module '@tanstack/react-router' {
  interface StaticDataRouteOption {
    crumb?: string | ComponentType
    parentCrumbs?: CrumbLink[]
  }
}
