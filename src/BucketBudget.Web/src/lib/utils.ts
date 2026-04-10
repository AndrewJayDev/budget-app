import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatMilliunits(milliunits: number, currency = 'USD'): string {
  const amount = milliunits / 1000
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(amount)
}

export function parseAmountToMilliunits(value: string): number {
  const cleaned = value.replace(/[^0-9.-]/g, '')
  const num = parseFloat(cleaned)
  if (isNaN(num)) return 0
  return Math.round(num * 1000)
}

export function formatDateOnly(date: string | null | undefined): string {
  if (!date) return ''
  // date is "YYYY-MM-DD"
  return date
}

export function todayIso(): string {
  return new Date().toISOString().split('T')[0]
}
