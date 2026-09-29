const LETTERS = 'ABCDEFGHJKLMNPRSTUVXYZ'
const DIGITS = '0123456789'

function pick(source: string, random: () => number) {
  return source[Math.floor(random() * source.length)]
}

export function normalizePlate(input: string) {
  return input.replace(/\s+/g, '').toUpperCase()
}

export function generateVisitorPlate(random: () => number = Math.random) {
  const letters = Array.from({ length: 3 }, () => pick(LETTERS, random)).join(
    '',
  )
  const digits = Array.from({ length: 3 }, () => pick(DIGITS, random)).join('')
  return `${letters}${digits}`
}
