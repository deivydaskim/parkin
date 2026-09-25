import { defaultPlacement, type SpaceFormInput } from './schemas'
import type { SpacePlacement, SpaceType } from './schemas'

type SpaceLike = {
  label: string
  type: SpaceType
  zone: string | null
  placement: SpacePlacement | null
}

export function spaceToFormValues(space: SpaceLike): SpaceFormInput {
  return {
    label: space.label,
    type: space.type,
    zone: space.zone ?? '',
    isPlaced: space.placement !== null,
    placement: space.placement ?? defaultPlacement,
  }
}
