import { defaultLotLayout, type Lot, type LotFormInput } from './schemas'

export function lotToFormValues(lot: Lot): LotFormInput {
  return {
    name: lot.name,
    address: lot.address ?? '',
    timezone: lot.timezone,
    accessMode: lot.accessMode,
    fullBehavior: lot.fullBehavior,
    hasLayout: lot.layout !== null,
    layout: lot.layout ?? defaultLotLayout,
  }
}
