import { useState, useEffect, useCallback, useRef } from 'react'
import { ChevronDown, ChevronRight, ChevronLeft, Wand2 } from 'lucide-react'
import {
  getMonthBudget, getAccounts, upsertBucketAllocation,
  type MonthBudgetDto, type AccountDto,
} from '@/lib/api'
import { formatMilliunits, parseAmountToMilliunits } from '@/lib/utils'
import { cn } from '@/lib/utils'

function fmtYearMonth(year: number, month: number): string {
  return new Date(year, month - 1, 1).toLocaleString('default', { month: 'long', year: 'numeric' })
}

export function BudgetPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [budget, setBudget] = useState<MonthBudgetDto | null>(null)
  const [accounts, setAccounts] = useState<AccountDto[]>([])
  const [loading, setLoading] = useState(true)
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set())
  const [editingBucketId, setEditingBucketId] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')
  const [saving, setSaving] = useState(false)
  const [autoAssigning, setAutoAssigning] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [budgetData, accts] = await Promise.all([
        getMonthBudget(year, month),
        getAccounts(false),
      ])
      setBudget(budgetData)
      setAccounts(accts)
    } catch (e) {
      console.error(e)
    } finally {
      setLoading(false)
    }
  }, [year, month])

  useEffect(() => { load() }, [load])

  useEffect(() => {
    if (editingBucketId && inputRef.current) {
      inputRef.current.focus()
      inputRef.current.select()
    }
  }, [editingBucketId])

  function prevMonth() {
    setEditingBucketId(null)
    if (month === 1) { setYear(y => y - 1); setMonth(12) }
    else setMonth(m => m - 1)
  }

  function nextMonth() {
    setEditingBucketId(null)
    if (month === 12) { setYear(y => y + 1); setMonth(1) }
    else setMonth(m => m + 1)
  }

  function toggleGroup(id: string) {
    setCollapsedGroups(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }

  function startEdit(bucketId: string, currentMilliunits: number) {
    setEditingBucketId(bucketId)
    setEditValue((currentMilliunits / 1000).toFixed(2))
  }

  async function commitEdit(bucketId: string) {
    if (saving) return
    const newMilliunits = parseAmountToMilliunits(editValue)
    setSaving(true)
    try {
      await upsertBucketAllocation(year, month, bucketId, newMilliunits)
      await load()
    } catch (e) {
      console.error(e)
    } finally {
      setSaving(false)
      setEditingBucketId(null)
    }
  }

  function cancelEdit() {
    setEditingBucketId(null)
    setEditValue('')
  }

  async function handleAutoAssign() {
    if (!budget || autoAssigning || readyToAssign <= 0) return
    const allBuckets = budget.bucketGroups.flatMap(g => g.buckets)

    // Strategy: first cover overspent buckets, then distribute remainder evenly to zero-allocated buckets
    const overspent = allBuckets.filter(b => (b.availableMilliunits ?? 0) < 0)
    let remaining = readyToAssign

    setAutoAssigning(true)
    try {
      // Phase 1: cover overspent
      for (const b of overspent) {
        const deficit = -(b.availableMilliunits ?? 0)
        if (deficit <= 0 || remaining <= 0) continue
        const toAdd = Math.min(deficit, remaining)
        const newAlloc = (b.allocatedMilliunits ?? 0) + toAdd
        await upsertBucketAllocation(year, month, b.id, newAlloc)
        remaining -= toAdd
      }

      // Phase 2: distribute evenly to unallocated buckets
      if (remaining > 0) {
        const unallocated = allBuckets.filter(b => (b.allocatedMilliunits ?? 0) === 0)
        if (unallocated.length > 0) {
          const perBucket = Math.floor(remaining / unallocated.length)
          if (perBucket > 0) {
            for (const b of unallocated) {
              await upsertBucketAllocation(year, month, b.id, perBucket)
            }
          }
        }
      }

      await load()
    } catch (e) {
      console.error(e)
    } finally {
      setAutoAssigning(false)
    }
  }

  // Compute Ready to Assign
  const allBuckets = budget?.bucketGroups.flatMap(g => g.buckets) ?? []
  const totalAssigned = allBuckets.reduce((s, b) => s + (b.allocatedMilliunits ?? 0), 0)
  const totalAccountBalance = accounts.reduce((s, a) => s + a.balanceMilliunits, 0)
  const readyToAssign = totalAccountBalance - totalAssigned

  return (
    <div className="flex-1 flex flex-col overflow-hidden bg-gray-50 dark:bg-gray-900">
      {/* Header with month selector */}
      <div className="bg-white dark:bg-gray-800 border-b px-6 py-3 flex items-center justify-between shrink-0">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Budget</h2>
        <div className="flex items-center gap-1">
          <button
            onClick={prevMonth}
            className="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="text-sm font-medium w-36 text-center text-gray-700 dark:text-gray-300">
            {fmtYearMonth(year, month)}
          </span>
          <button
            onClick={nextMonth}
            className="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6 space-y-4">
        {/* Ready to Assign banner */}
        <div className={cn(
          'rounded-lg p-4 flex items-center justify-between border',
          readyToAssign > 0
            ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-700'
            : readyToAssign === 0
            ? 'bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-600'
            : 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-700'
        )}>
          <div>
            <p className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
              Ready to Assign
            </p>
            <p className={cn(
              'text-2xl font-bold mt-0.5',
              readyToAssign > 0
                ? 'text-green-700 dark:text-green-400'
                : readyToAssign === 0
                ? 'text-gray-700 dark:text-gray-300'
                : 'text-red-700 dark:text-red-400'
            )}>
              {formatMilliunits(readyToAssign)}
            </p>
            {readyToAssign < 0 && (
              <p className="text-xs text-red-600 dark:text-red-400 mt-0.5">
                You've assigned more than your total balance
              </p>
            )}
          </div>
          {readyToAssign > 0 && (
            <button
              onClick={handleAutoAssign}
              disabled={autoAssigning}
              className="flex items-center gap-1.5 text-xs bg-green-600 hover:bg-green-500 disabled:opacity-60 text-white px-3 py-1.5 rounded-md font-medium transition-colors"
            >
              <Wand2 className="h-3.5 w-3.5" />
              {autoAssigning ? 'Assigning…' : 'Auto-Assign'}
            </button>
          )}
        </div>

        {/* Budget table */}
        {loading ? (
          <div className="text-center text-gray-400 py-12">Loading budget…</div>
        ) : !budget || budget.bucketGroups.length === 0 ? (
          <div className="text-center text-gray-400 py-12">
            No budget categories yet. Add bucket groups and buckets to get started.
          </div>
        ) : (
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
            {/* Column headers */}
            <div className="grid grid-cols-[1fr_148px_148px_148px] border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-4 py-2">
              <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Category</div>
              <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider text-right pr-2">Assigned</div>
              <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider text-right pr-2">Activity</div>
              <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider text-right pr-2">Available</div>
            </div>

            {budget.bucketGroups.map(group => {
              const collapsed = collapsedGroups.has(group.id)
              const groupAssigned = group.buckets.reduce((s, b) => s + (b.allocatedMilliunits ?? 0), 0)
              const groupActivity = group.buckets.reduce((s, b) => s + (b.activityMilliunits ?? 0), 0)
              const groupAvailable = group.buckets.reduce((s, b) => s + (b.availableMilliunits ?? 0), 0)

              return (
                <div key={group.id} className="border-t border-gray-100 dark:border-gray-700 first:border-t-0">
                  {/* Group header row */}
                  <button
                    onClick={() => toggleGroup(group.id)}
                    className="w-full grid grid-cols-[1fr_148px_148px_148px] px-4 py-2 bg-gray-50 dark:bg-gray-900/80 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-left"
                  >
                    <div className="flex items-center gap-1.5 text-sm font-semibold text-gray-700 dark:text-gray-200">
                      {collapsed
                        ? <ChevronRight className="h-3.5 w-3.5 shrink-0 text-gray-400" />
                        : <ChevronDown className="h-3.5 w-3.5 shrink-0 text-gray-400" />}
                      {group.name}
                    </div>
                    <div className="text-sm font-medium text-gray-600 dark:text-gray-400 text-right pr-2">
                      {formatMilliunits(groupAssigned)}
                    </div>
                    <div className={cn(
                      'text-sm font-medium text-right pr-2',
                      groupActivity < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'
                    )}>
                      {formatMilliunits(groupActivity)}
                    </div>
                    <div className={cn(
                      'text-sm font-medium text-right pr-2',
                      groupAvailable > 0 ? 'text-green-600 dark:text-green-400'
                        : groupAvailable === 0 ? 'text-yellow-600 dark:text-yellow-400'
                        : 'text-red-600 dark:text-red-400'
                    )}>
                      {formatMilliunits(groupAvailable)}
                    </div>
                  </button>

                  {/* Bucket rows */}
                  {!collapsed && group.buckets.map(bucket => {
                    const available = bucket.availableMilliunits ?? 0
                    const isEditing = editingBucketId === bucket.id

                    return (
                      <div
                        key={bucket.id}
                        className="grid grid-cols-[1fr_148px_148px_148px] px-4 py-1.5 border-t border-gray-50 dark:border-gray-700/60 hover:bg-gray-50 dark:hover:bg-gray-900/40 transition-colors items-center"
                      >
                        {/* Name */}
                        <div className="text-sm text-gray-700 dark:text-gray-300 pl-5 truncate pr-2">
                          {bucket.name}
                        </div>

                        {/* Assigned — inline editable */}
                        <div className="text-right pr-1">
                          {isEditing ? (
                            <input
                              ref={inputRef}
                              type="text"
                              value={editValue}
                              onChange={e => setEditValue(e.target.value)}
                              onBlur={() => commitEdit(bucket.id)}
                              onKeyDown={e => {
                                if (e.key === 'Enter') { e.preventDefault(); commitEdit(bucket.id) }
                                if (e.key === 'Escape') { e.preventDefault(); cancelEdit() }
                              }}
                              className="w-full text-right text-sm border border-blue-400 rounded px-2 py-0.5 focus:outline-none focus:ring-1 focus:ring-blue-500 bg-white dark:bg-gray-700 dark:text-white"
                            />
                          ) : (
                            <button
                              onClick={() => startEdit(bucket.id, bucket.allocatedMilliunits ?? 0)}
                              className="text-sm text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 w-full text-right px-2 py-0.5 rounded hover:bg-blue-50 dark:hover:bg-blue-900/30 transition-colors font-mono"
                            >
                              {formatMilliunits(bucket.allocatedMilliunits ?? 0)}
                            </button>
                          )}
                        </div>

                        {/* Activity */}
                        <div className={cn(
                          'text-sm text-right pr-2 font-mono',
                          (bucket.activityMilliunits ?? 0) < 0
                            ? 'text-red-600 dark:text-red-400'
                            : 'text-gray-500 dark:text-gray-400'
                        )}>
                          {formatMilliunits(bucket.activityMilliunits ?? 0)}
                        </div>

                        {/* Available — color-coded pill */}
                        <div className="text-right pr-1">
                          <span className={cn(
                            'inline-block text-sm font-medium font-mono px-2 py-0.5 rounded',
                            available > 0
                              ? 'text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/20'
                              : available === 0
                              ? 'text-yellow-700 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20'
                              : 'text-red-700 dark:text-red-400 bg-red-50 dark:bg-red-900/20'
                          )}>
                            {formatMilliunits(available)}
                          </span>
                        </div>
                      </div>
                    )
                  })}
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
