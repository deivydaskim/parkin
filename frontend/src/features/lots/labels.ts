import type { FullBehavior } from './schemas'

export const fullBehaviorLabels: Record<FullBehavior, string> = {
  Block: 'Block when full',
  AllowOverflow: 'Allow overflow',
}

export const fullBehaviorHints: Record<FullBehavior, string> = {
  Block: 'Denies entry when full',
  AllowOverflow: 'Admits past capacity when full',
}
