import { Fragment } from 'react'
import { Link, useMatches } from '@tanstack/react-router'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import '@/app/route-meta'

export function Breadcrumbs() {
  const matches = useMatches()
  const current = [...matches].reverse().find((match) => match.staticData.crumb)

  if (!current) return null

  const { crumb: Crumb, parentCrumbs = [] } = current.staticData

  return (
    <Breadcrumb className="min-w-0">
      <BreadcrumbList className="flex-nowrap">
        {parentCrumbs.map((parent) => (
          <Fragment key={parent.label}>
            <BreadcrumbItem className="hidden sm:inline-flex">
              {parent.to ? (
                <BreadcrumbLink asChild>
                  <Link to={parent.to}>{parent.label}</Link>
                </BreadcrumbLink>
              ) : (
                <span>{parent.label}</span>
              )}
            </BreadcrumbItem>
            <BreadcrumbSeparator className="hidden sm:block" />
          </Fragment>
        ))}
        <BreadcrumbItem className="min-w-0">
          <BreadcrumbPage className="truncate">
            {typeof Crumb === 'string' ? Crumb : Crumb ? <Crumb /> : null}
          </BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  )
}
