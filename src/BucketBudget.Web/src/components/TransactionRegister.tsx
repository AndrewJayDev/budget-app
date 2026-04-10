import React, { useState, useRef, type KeyboardEvent } from 'react'
import { Plus, Trash2, Check, Split, Pencil, X } from 'lucide-react'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from './ui/table'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Checkbox } from './ui/checkbox'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select'
import { Badge } from './ui/badge'
import {
  createTransaction,
  updateTransaction,
  deleteTransaction,
  type TransactionDto,
  type BucketDto,
  type CreateTransactionData,
} from '@/lib/api'
import { formatMilliunits, parseAmountToMilliunits, todayIso } from '@/lib/utils'
import { SplitTransactionDialog } from './SplitTransactionDialog'

interface EditState {
  id: string | null  // null = new row
  date: string
  payee: string
  bucketId: string
  memo: string
  outflow: string
  inflow: string
  isCleared: boolean
}

const EMPTY_EDIT: EditState = {
  id: null,
  date: todayIso(),
  payee: '',
  bucketId: '',
  memo: '',
  outflow: '',
  inflow: '',
  isCleared: false,
}

interface TransactionRegisterProps {
  accountId: string
  currency: string
  transactions: TransactionDto[]
  buckets: BucketDto[]
  onChanged: () => void
  reconcileMode: boolean
}

function bucketName(buckets: BucketDto[], id: string | null | undefined): string {
  if (!id) return ''
  return buckets.find(b => b.id === id)?.name ?? ''
}

export function TransactionRegister({
  accountId,
  currency,
  transactions,
  buckets,
  onChanged,
  reconcileMode,
}: TransactionRegisterProps) {
  const [editing, setEditing] = useState<EditState | null>(null)
  const [saving, setSaving] = useState(false)
  const [splitTarget, setSplitTarget] = useState<TransactionDto | null>(null)
  const [addingNew, setAddingNew] = useState(false)
  const payeeRef = useRef<HTMLInputElement>(null)

  // Derived balance stats
  const cleared = transactions.filter(t => t.isCleared).reduce((s, t) => s + t.amountMilliunits, 0)
  const uncleared = transactions.filter(t => !t.isCleared).reduce((s, t) => s + t.amountMilliunits, 0)

  function startEdit(tx: TransactionDto) {
    const amt = tx.amountMilliunits
    setEditing({
      id: tx.id,
      date: tx.date,
      payee: tx.payee,
      bucketId: tx.bucketId ?? '',
      memo: tx.memo ?? '',
      outflow: amt < 0 ? String(-amt / 1000) : '',
      inflow: amt > 0 ? String(amt / 1000) : '',
      isCleared: tx.isCleared,
    })
  }

  function startNew() {
    setEditing({ ...EMPTY_EDIT, date: todayIso() })
    setAddingNew(true)
    setTimeout(() => payeeRef.current?.focus(), 50)
  }

  function cancelEdit() {
    setEditing(null)
    setAddingNew(false)
  }

  function editAmount(): number {
    if (!editing) return 0
    const infl = parseAmountToMilliunits(editing.inflow)
    const out = parseAmountToMilliunits(editing.outflow)
    return infl > 0 ? infl : -out
  }

  async function saveEdit() {
    if (!editing || saving) return
    const amount = editAmount()
    setSaving(true)
    try {
      if (editing.id) {
        await updateTransaction(editing.id, {
          bucketId: editing.bucketId || null,
          payee: editing.payee || 'Unknown',
          amountMilliunits: amount,
          date: editing.date,
          memo: editing.memo || null,
          isCleared: editing.isCleared,
        })
      } else {
        const data: CreateTransactionData = {
          accountId,
          bucketId: editing.bucketId || null,
          payee: editing.payee || 'Unknown',
          amountMilliunits: amount,
          date: editing.date,
          memo: editing.memo || null,
          isCleared: editing.isCleared,
        }
        await createTransaction(data)
      }
      setEditing(null)
      setAddingNew(false)
      onChanged()
    } catch (e) {
      console.error(e)
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this transaction?')) return
    try {
      await deleteTransaction(id)
      onChanged()
    } catch (e) {
      console.error(e)
    }
  }

  async function toggleCleared(tx: TransactionDto) {
    try {
      await updateTransaction(tx.id, {
        bucketId: tx.bucketId ?? null,
        payee: tx.payee,
        amountMilliunits: tx.amountMilliunits,
        date: tx.date,
        memo: tx.memo ?? null,
        isCleared: !tx.isCleared,
      })
      onChanged()
    } catch (e) {
      console.error(e)
    }
  }

  function handleKeyDown(e: KeyboardEvent, _field: string) {
    if (e.key === 'Escape') { cancelEdit(); return }
    if (e.key === 'Enter') { e.preventDefault(); saveEdit() }
    // Tab is handled natively
  }

  const isEditing = (id: string) => editing?.id === id

  const runningBalance = (() => {
    let balance = 0
    const map = new Map<string, number>()
    // Iterate in reverse (oldest first) to compute running balance
    const sorted = [...transactions].reverse()
    for (const tx of sorted) {
      balance += tx.amountMilliunits
      map.set(tx.id, balance)
    }
    return map
  })()

  return (
    <div className="space-y-2">
      {/* Balance bar */}
      <div className="flex items-center gap-6 px-2 py-2 bg-white rounded-lg border text-sm">
        <div>
          <span className="text-gray-500 text-xs">Cleared</span>
          <div className={`font-mono font-medium ${cleared < 0 ? 'text-red-600' : 'text-gray-900'}`}>
            {formatMilliunits(cleared, currency)}
          </div>
        </div>
        <span className="text-gray-300">+</span>
        <div>
          <span className="text-gray-500 text-xs">Uncleared</span>
          <div className={`font-mono font-medium ${uncleared < 0 ? 'text-red-600' : 'text-gray-900'}`}>
            {formatMilliunits(uncleared, currency)}
          </div>
        </div>
        <span className="text-gray-300">=</span>
        <div>
          <span className="text-gray-500 text-xs">Working Balance</span>
          <div className={`font-mono font-medium ${(cleared + uncleared) < 0 ? 'text-red-600' : 'text-green-700'}`}>
            {formatMilliunits(cleared + uncleared, currency)}
          </div>
        </div>
        <div className="ml-auto">
          {!addingNew && !editing && (
            <Button size="sm" onClick={startNew}>
              <Plus className="h-4 w-4 mr-1" />
              Add Transaction
            </Button>
          )}
        </div>
      </div>

      {/* Table */}
      <div className="border rounded-lg bg-white overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="bg-gray-50">
              <TableHead className="w-28">Date</TableHead>
              <TableHead>Payee</TableHead>
              <TableHead>Bucket</TableHead>
              <TableHead>Memo</TableHead>
              <TableHead className="text-right w-28">Outflow</TableHead>
              <TableHead className="text-right w-28">Inflow</TableHead>
              <TableHead className="text-right w-28">Balance</TableHead>
              <TableHead className="w-10 text-center">✓</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {/* New row */}
            {addingNew && editing && editing.id === null && (
              <EditRow
                key="new"
                editing={editing}
                buckets={buckets}
                saving={saving}
                payeeRef={payeeRef}
                onChange={setEditing}
                onSave={saveEdit}
                onCancel={cancelEdit}
                onKeyDown={handleKeyDown}
              />
            )}

            {transactions.map(tx => {
              const isOut = tx.amountMilliunits < 0
              const bal = runningBalance.get(tx.id) ?? 0

              if (isEditing(tx.id) && editing) {
                return (
                  <EditRow
                    key={tx.id}
                    editing={editing}
                    buckets={buckets}
                    saving={saving}
                    onChange={setEditing}
                    onSave={saveEdit}
                    onCancel={cancelEdit}
                    onKeyDown={handleKeyDown}
                  />
                )
              }

              return (
                <TableRow
                  key={tx.id}
                  className={`group cursor-pointer ${tx.isCleared ? 'bg-white' : 'bg-yellow-50/40'} ${reconcileMode ? 'hover:bg-blue-50' : ''}`}
                  onDoubleClick={() => !reconcileMode && startEdit(tx)}
                >
                  <TableCell className="text-xs font-mono">{tx.date}</TableCell>
                  <TableCell className="text-sm font-medium">{tx.payee}</TableCell>
                  <TableCell className="text-xs text-gray-500">
                    {tx.bucketId ? (
                      <Badge variant="secondary" className="text-xs py-0">{bucketName(buckets, tx.bucketId)}</Badge>
                    ) : (
                      <span className="text-gray-300 italic">unassigned</span>
                    )}
                  </TableCell>
                  <TableCell className="text-xs text-gray-400">{tx.memo}</TableCell>
                  <TableCell className="text-right text-xs font-mono">
                    {isOut && (
                      <span className="text-red-600">
                        {formatMilliunits(-tx.amountMilliunits, currency)}
                      </span>
                    )}
                  </TableCell>
                  <TableCell className="text-right text-xs font-mono">
                    {!isOut && (
                      <span className="text-green-700">
                        {formatMilliunits(tx.amountMilliunits, currency)}
                      </span>
                    )}
                  </TableCell>
                  <TableCell className={`text-right text-xs font-mono ${bal < 0 ? 'text-red-600' : 'text-gray-700'}`}>
                    {formatMilliunits(bal, currency)}
                  </TableCell>
                  <TableCell className="text-center">
                    <Checkbox
                      checked={tx.isCleared}
                      onCheckedChange={() => toggleCleared(tx)}
                      onClick={e => e.stopPropagation()}
                    />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      {!reconcileMode && (
                        <>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6 text-gray-400 hover:text-blue-500"
                            onClick={(e) => { e.stopPropagation(); startEdit(tx) }}
                            title="Edit"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6 text-gray-400 hover:text-purple-500"
                            onClick={(e) => { e.stopPropagation(); setSplitTarget(tx) }}
                            title="Split"
                          >
                            <Split className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6 text-gray-400 hover:text-red-500"
                            onClick={(e) => { e.stopPropagation(); handleDelete(tx.id) }}
                            title="Delete"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              )
            })}

            {transactions.length === 0 && !addingNew && (
              <TableRow>
                <TableCell colSpan={9} className="text-center text-gray-400 py-12 text-sm">
                  No transactions yet. Click "Add Transaction" to get started.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Split dialog */}
      {splitTarget && (
        <SplitTransactionDialog
          open={!!splitTarget}
          onClose={() => setSplitTarget(null)}
          accountId={accountId}
          date={splitTarget.date}
          payee={splitTarget.payee}
          totalMilliunits={splitTarget.amountMilliunits}
          currency={currency}
          buckets={buckets}
          onSaved={() => { setSplitTarget(null); onChanged() }}
        />
      )}
    </div>
  )
}

// --- Inline edit row ---

interface EditRowProps {
  editing: EditState
  buckets: BucketDto[]
  saving: boolean
  payeeRef?: React.RefObject<HTMLInputElement | null>
  onChange: (s: EditState) => void
  onSave: () => void
  onCancel: () => void
  onKeyDown: (e: KeyboardEvent, field: string) => void
}

function EditRow({ editing, buckets, saving, payeeRef, onChange, onSave, onCancel, onKeyDown }: EditRowProps) {
  const set = (field: keyof EditState) => (e: React.ChangeEvent<HTMLInputElement>) =>
    onChange({ ...editing, [field]: e.target.value })

  return (
    <TableRow className="bg-blue-50 border-2 border-blue-300">
      {/* Date */}
      <TableCell>
        <Input
          type="date"
          value={editing.date}
          onChange={set('date')}
          onKeyDown={(e) => onKeyDown(e, 'date')}
          className="h-7 text-xs w-full"
        />
      </TableCell>
      {/* Payee */}
      <TableCell>
        <Input
          ref={payeeRef}
          placeholder="Payee"
          value={editing.payee}
          onChange={set('payee')}
          onKeyDown={(e) => onKeyDown(e, 'payee')}
          className="h-7 text-xs"
        />
      </TableCell>
      {/* Bucket */}
      <TableCell>
        <Select
          value={editing.bucketId}
          onValueChange={(v) => onChange({ ...editing, bucketId: v })}
        >
          <SelectTrigger className="h-7 text-xs">
            <SelectValue placeholder="Bucket (opt.)" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">No bucket</SelectItem>
            {buckets.map(b => (
              <SelectItem key={b.id} value={b.id}>{b.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </TableCell>
      {/* Memo */}
      <TableCell>
        <Input
          placeholder="Memo"
          value={editing.memo}
          onChange={set('memo')}
          onKeyDown={(e) => onKeyDown(e, 'memo')}
          className="h-7 text-xs"
        />
      </TableCell>
      {/* Outflow */}
      <TableCell>
        <Input
          placeholder="0.00"
          value={editing.outflow}
          onChange={(e) => onChange({ ...editing, outflow: e.target.value, inflow: e.target.value ? '' : editing.inflow })}
          onKeyDown={(e) => onKeyDown(e, 'outflow')}
          className="h-7 text-xs font-mono text-right"
        />
      </TableCell>
      {/* Inflow */}
      <TableCell>
        <Input
          placeholder="0.00"
          value={editing.inflow}
          onChange={(e) => onChange({ ...editing, inflow: e.target.value, outflow: e.target.value ? '' : editing.outflow })}
          onKeyDown={(e) => onKeyDown(e, 'inflow')}
          className="h-7 text-xs font-mono text-right"
        />
      </TableCell>
      {/* Balance (empty in edit mode) */}
      <TableCell />
      {/* Cleared */}
      <TableCell className="text-center">
        <Checkbox
          checked={editing.isCleared}
          onCheckedChange={(v) => onChange({ ...editing, isCleared: !!v })}
        />
      </TableCell>
      {/* Actions */}
      <TableCell>
        <div className="flex items-center gap-1">
          <Button
            size="icon"
            variant="default"
            className="h-6 w-6"
            onClick={onSave}
            disabled={saving}
            title="Save (Enter)"
          >
            <Check className="h-3.5 w-3.5" />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            className="h-6 w-6 text-gray-400"
            onClick={onCancel}
            title="Cancel (Escape)"
          >
            <X className="h-3.5 w-3.5" />
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}
