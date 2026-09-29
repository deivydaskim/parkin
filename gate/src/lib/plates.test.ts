import { describe, expect, it } from 'vitest'
import { generateVisitorPlate, normalizePlate } from './plates'

describe('normalizePlate', () => {
  it('trims, upper-cases and removes spaces', () => {
    expect(normalizePlate('  ab c 123 ')).toBe('ABC123')
  })

  it('keeps hyphens', () => {
    expect(normalizePlate('ona-001')).toBe('ONA-001')
  })

  it('returns an empty string for whitespace', () => {
    expect(normalizePlate('   ')).toBe('')
  })
})

describe('generateVisitorPlate', () => {
  it('produces three letters followed by three digits', () => {
    for (let index = 0; index < 50; index++) {
      expect(generateVisitorPlate()).toMatch(/^[A-Z]{3}\d{3}$/)
    }
  })

  it('is deterministic for a fixed random source', () => {
    expect(generateVisitorPlate(() => 0)).toBe('AAA000')
  })

  it('survives normalization unchanged', () => {
    const plate = generateVisitorPlate()
    expect(normalizePlate(plate)).toBe(plate)
  })
})
