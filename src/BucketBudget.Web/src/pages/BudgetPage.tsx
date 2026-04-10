import { useState, useCallback, useMemo, useRef } from 'react'
import { ChevronLeft, ChevronRight, ChevronDown, Zap, AlertCircle, Moon, Sun } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from '@/components/ui/collapsible'
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from '@/components/ui/tooltip'
import { useMonthBudget, useAccounts, useUpsertAllocation, useBatchUpsertAllocations } from '@/hooks/useBudget'
import { useTheme } from '@/hooks/useTheme'
import { formatCurrency, parseAmountInput, addMonths, monthLabel } from '@/lib/utils'
import type { BucketSummaryDto, BucketGroupSummaryDto } from '@/lib/api'
import { cn } from '@/lib/utils'

// --- Color coding ---
function bucketStatusClass(bucket: BucketSummaryDto): string {
  if (bucket.availableMilliunits < 0) return 'text-red-500 dark:text-red-400'
  if (bucket.availableMilliunits === 0 && bucket.allocatedMilliunits === 0) return 'text-muted-foreground'
  if (bucket.availableMilliunits < bucket.allocatedMilliunits) return 'text-yellow-500 dark:text-yellow-400'
  return 'text-green-600 dark:text-green-400'
}

function bucketAvailableBg(bucket: BucketSummaryDto): string {
  if (bucket.availableMilliunits < 0) return 'bg-red-50 dark:bg-red-950/30'
  if (bucket.availableMilliunits === 0 && bucket.allocatedMilliunits === 0) return ''
  if (bucket.availableMilliunits < bucket.allocatedMilliunits) return 'bg-yellow-50 dark:bg-yellow-950/30'
  return 'bg-green-50 dark:bg-green-950/20'
}

// --- Inline editable cell ---
interface EditableCellProps {
  bucketId: string
  value: number
  onSave: (bucketId: string, milliunits: number) => void
  disabled?: boolean
}

function EditableCell({ bucketId, value, onSave, disabled }: EditableCellProps) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState('')
  const cancelledRef = useRef(false)

  const startEdit = () => {
    if (disabled) return
    cancelledRef.current = false
    setDraft((value / 1000).toFixed(2))
    setEditing(true)
  }

  const commit = () => {
    if (cancelledRef.current) return
    const milliunits = parseAmountInput(draft)
    onSave(bucketId, milliunits)
    setEditing(false)
  }

  const cancel = () => {
    cancelledRef.current = true
    setEditing(false)
  }

  if (editing) {
    return (
      <Input
        autoFocus
        className="h-7 w-28 text-right text-sm px-2"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onBlur={commit}
        onKeyDown={(e) => {
          if (e.key === 'Enter') commit()
          if (e.key === 'Escape') cancel()
        }}
      />
    )
  }

  return (
    <button
      onClick={startEdit}
      disabled={disabled}
      className={cn(
        'w-28 text-right text-sm rounded px-2 py-1 transition-colors',
        disabled
          ? 'cursor-default'
          : 'hover:bg-accent hover:text-accent-foreground cursor-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'
      )}
    >
      {formatCurrency(value)}
    </button>
  )
}

// --- Bucket row ---
interface BucketRowProps {
  bucket: BucketSummaryDto
  onSave: (bucketId: string, milliunits: number) => void
  isSaving: boolean
}

function BucketRow({ bucket, onSave, isSaving }: BucketRowProps) {
  return (
    <div
      className={cn(
        'grid grid-cols-[1fr_auto_auto_auto] items-center gap-2 px-4 py-1.5 border-b border-border/50 last:border-0',
        bucketAvailableBg(bucket)
      )}
    >
      <span className="text-sm truncate pl-4">{bucket.name}</span>
      <EditableCell bucketId={bucket.id} value={bucket.allocatedMilliunits} onSave={onSave} disabled={isSaving} />
      <span className="w-28 text-right text-sm text-muted-foreground">
        {formatCurrency(bucket.activityMilliunits)}
      </span>
      <span className={cn('w-28 text-right text-sm font-medium', bucketStatusClass(bucket))}>
        {formatCurrency(bucket.availableMilliunits)}
      </span>
    </div>
  )
}

// --- Group header ---
interface GroupHeaderProps {
  group: BucketGroupSummaryDto
  isOpen: boolean
}

function GroupTotals({ group }: { group: BucketGroupSummaryDto }) {
  const totalAllocated = group.buckets.reduce((s, b) => s + b.allocatedMilliunits, 0)
  const totalActivity = group.buckets.reduce((s, b) => s + b.activityMilliunits, 0)
  const totalAvailable = group.buckets.reduce((s, b) => s + b.availableMilliunits, 0)
  return (
    <>
      <span className="w-28 text-right text-sm font-semibold">{formatCurrency(totalAllocated)}</span>
      <span className="w-28 text-right text-sm font-semibold text-muted-foreground">{formatCurrency(totalActivity)}</span>
      <span className={cn('w-28 text-right text-sm font-semibold', totalAvailable < 0 ? 'text-red-500' : 'text-foreground')}>
        {formatCurrency(totalAvailable)}
      </span>
    </>
  )
}

function GroupHeader({ group, isOpen }: GroupHeaderProps) {
  return (
    <CollapsibleTrigger asChild>
      <div className="grid grid-cols-[1fr_auto_auto_auto] items-center gap-2 px-4 py-2 bg-muted/50 hover:bg-muted cursor-pointer select-none border-b border-border">
        <div className="flex items-center gap-2 font-semibold text-sm">
          {isOpen ? <ChevronDown className="h-4 w-4 shrink-0" /> : <ChevronRight className="h-4 w-4 shrink-0" />}
          {group.name}
        </div>
        <GroupTotals group={group} />
      </div>
    </CollapsibleTrigger>
  )
}

// --- Auto-assign options ---
interface AutoAssignProps {
  readyToAssignMilliunits: number
  bucketGroups: BucketGroupSummaryDto[]
  year: number
  month: number
  onAssign: (updates: { bucketId: string; allocatedMilliunits: number }[]) => void
  disabled: boolean
}

function AutoAssignMenu({ readyToAssignMilliunits, bucketGroups, onAssign, disabled }: AutoAssignProps) {
  const [open, setOpen] = useState(false)

  if (readyToAssignMilliunits <= 0) return null

  const handleFundUnderfunded = () => {
    const updates: { bucketId: string; allocatedMilliunits: number }[] = []
    for (const group of bucketGroups) {
      for (const bucket of group.buckets) {
        if (bucket.availableMilliunits < 0) {
          // Top up to zero
          updates.push({
            bucketId: bucket.id,
            allocatedMilliunits: bucket.allocatedMilliunits + Math.abs(bucket.availableMilliunits),
          })
        }
      }
    }
    if (updates.length > 0) onAssign(updates)
    setOpen(false)
  }

  return (
    <div className="relative">
      <Button variant="outline" size="sm" onClick={() => setOpen((o) => !o)} disabled={disabled}>
        <Zap className="h-4 w-4" />
        Auto-assign
        <ChevronDown className="h-3 w-3 opacity-60" />
      </Button>
      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-1 z-20 w-52 rounded-md border bg-popover shadow-md p-1">
            <button
              className="w-full text-left text-sm px-3 py-2 rounded hover:bg-accent transition-colors"
              onClick={handleFundUnderfunded}
            >
              Fund overspent buckets
            </button>
          </div>
        </>
      )}
    </div>
  )
}

// --- Ready to Assign banner ---
interface ReadyToAssignProps {
  readyToAssignMilliunits: number
}

function ReadyToAssignBanner({ readyToAssignMilliunits }: ReadyToAssignProps) {
  const isNegative = readyToAssignMilliunits < 0

  return (
    <div
      className={cn(
        'flex items-center justify-between rounded-lg border px-4 py-3 mb-4',
        isNegative
          ? 'border-red-300 bg-red-50 dark:border-red-800 dark:bg-red-950/30'
          : 'border-green-300 bg-green-50 dark:border-green-800 dark:bg-green-950/20'
      )}
    >
      <div className="flex items-center gap-2">
        {isNegative && <AlertCircle className="h-4 w-4 text-red-500 shrink-0" />}
        <span className="text-sm font-medium">Ready to Assign</span>
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="text-xs text-muted-foreground cursor-help underline decoration-dotted">
                What is this?
              </span>
            </TooltipTrigger>
            <TooltipContent className="max-w-xs">
              Total account balances minus all money assigned to buckets this month.
              Assign this money to buckets so every dollar has a job.
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
      <span
        className={cn(
          'text-lg font-bold',
          isNegative ? 'text-red-600 dark:text-red-400' : 'text-green-700 dark:text-green-400'
        )}
      >
        {formatCurrency(readyToAssignMilliunits)}
      </span>
    </div>
  )
}

// --- Column header row ---
function ColumnHeaders() {
  return (
    <div className="grid grid-cols-[1fr_auto_auto_auto] gap-2 px-4 py-2 border-b border-border bg-background sticky top-0 z-10">
      <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Bucket</span>
      <span className="w-28 text-right text-xs font-semibold uppercase tracking-wide text-muted-foreground">Assigned</span>
      <span className="w-28 text-right text-xs font-semibold uppercase tracking-wide text-muted-foreground">Activity</span>
      <span className="w-28 text-right text-xs font-semibold uppercase tracking-wide text-muted-foreground">Available</span>
    </div>
  )
}

// --- Main page ---
export function BudgetPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const { data: budget, isLoading: budgetLoading, error: budgetError } = useMonthBudget(year, month)
  const { data: accounts } = useAccounts()
  const { mutate: upsertAllocation, isPending: isSaving } = useUpsertAllocation(year, month)
  const { mutate: batchUpsert, isPending: isBatchSaving } = useBatchUpsertAllocations(year, month)

  const { theme, toggle: toggleTheme } = useTheme()
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set())

  const toggleGroup = useCallback((groupId: string) => {
    setCollapsedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(groupId)) next.delete(groupId)
      else next.add(groupId)
      return next
    })
  }, [])

  const readyToAssignMilliunits = useMemo(() => {
    if (!accounts || !budget) return 0
    const totalBalance = accounts.filter((a) => !a.isClosed).reduce((s, a) => s + a.balanceMilliunits, 0)
    const totalAssigned = budget.bucketGroups
      .flatMap((g) => g.buckets)
      .reduce((s, b) => s + b.allocatedMilliunits, 0)
    return totalBalance - totalAssigned
  }, [accounts, budget])

  const handleSave = useCallback(
    (bucketId: string, allocatedMilliunits: number) => {
      upsertAllocation({ bucketId, allocatedMilliunits })
    },
    [upsertAllocation]
  )

  const handleAutoAssign = useCallback(
    (updates: { bucketId: string; allocatedMilliunits: number }[]) => {
      batchUpsert(updates)
    },
    [batchUpsert]
  )

  const prevMonth = () => {
    const { year: y, month: m } = addMonths(year, month, -1)
    setYear(y)
    setMonth(m)
  }

  const nextMonth = () => {
    const { year: y, month: m } = addMonths(year, month, 1)
    setYear(y)
    setMonth(m)
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      {/* Top bar */}
      <header className="sticky top-0 z-20 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto max-w-4xl px-4 py-3 flex items-center justify-between gap-4">
          {/* Month navigation */}
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" onClick={prevMonth} aria-label="Previous month">
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <h1 className="text-lg font-semibold w-44 text-center">{monthLabel(year, month)}</h1>
            <Button variant="ghost" size="icon" onClick={nextMonth} aria-label="Next month">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" onClick={toggleTheme} aria-label="Toggle dark mode">
              {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </Button>
            {budget && (
              <AutoAssignMenu
              readyToAssignMilliunits={readyToAssignMilliunits}
              bucketGroups={budget.bucketGroups}
              year={year}
              month={month}
              onAssign={handleAutoAssign}
              disabled={isSaving || isBatchSaving}
            />
            )}
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-4xl px-4 py-4">
        {/* Ready to Assign */}
        <ReadyToAssignBanner readyToAssignMilliunits={readyToAssignMilliunits} />

        {/* Budget table */}
        <div className="rounded-lg border overflow-hidden">
          <ColumnHeaders />

          {budgetLoading && (
            <div className="flex items-center justify-center py-16 text-muted-foreground">
              <span className="text-sm">Loading budget…</span>
            </div>
          )}

          {budgetError && (
            <div className="flex items-center justify-center py-16 text-red-500 gap-2">
              <AlertCircle className="h-4 w-4" />
              <span className="text-sm">Failed to load budget. Check the API connection.</span>
            </div>
          )}

          {budget && budget.bucketGroups.length === 0 && (
            <div className="flex items-center justify-center py-16 text-muted-foreground">
              <span className="text-sm">No bucket groups yet. Create some buckets to get started.</span>
            </div>
          )}

          {budget &&
            budget.bucketGroups.map((group) => {
              const isOpen = !collapsedGroups.has(group.id)
              return (
                <Collapsible key={group.id} open={isOpen} onOpenChange={() => toggleGroup(group.id)}>
                  <GroupHeader group={group} isOpen={isOpen} />
                  <CollapsibleContent>
                    {group.buckets.map((bucket) => (
                      <BucketRow key={bucket.id} bucket={bucket} onSave={handleSave} isSaving={isSaving || isBatchSaving} />
                    ))}
                  </CollapsibleContent>
                </Collapsible>
              )
            })}
        </div>
      </main>
    </div>
  )
}
