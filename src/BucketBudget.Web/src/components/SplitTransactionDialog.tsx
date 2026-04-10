import { useState, useEffect } from 'react'
import { Plus, Trash2, AlertCircle } from 'lucide-react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from './ui/dialog'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select'
import { createTransaction, deleteTransaction, type CreateTransactionData, type BucketDto } from '@/lib/api'
import { parseAmountToMilliunits, formatMilliunits } from '@/lib/utils'

interface SplitLine {
  bucketId: string
  memo: string
  amount: string
}

interface SplitTransactionDialogProps {
  open: boolean
  onClose: () => void
  accountId: string
  /** If set, this transaction is deleted after the split lines are created */
  originalId?: string
  date: string
  payee: string
  totalMilliunits: number
  currency: string
  buckets: BucketDto[]
  onSaved: () => void
}

export function SplitTransactionDialog({
  open,
  onClose,
  accountId,
  originalId,
  date,
  payee,
  totalMilliunits,
  currency,
  buckets,
  onSaved,
}: SplitTransactionDialogProps) {
  const [lines, setLines] = useState<SplitLine[]>([
    { bucketId: '', memo: '', amount: '' },
    { bucketId: '', memo: '', amount: '' },
  ])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setLines([
        { bucketId: '', memo: '', amount: '' },
        { bucketId: '', memo: '', amount: '' },
      ])
      setError(null)
    }
  }, [open])

  const totalAlloc = lines.reduce((s, l) => s + parseAmountToMilliunits(l.amount), 0)
  const remaining = totalMilliunits - totalAlloc
  const isBalanced = Math.abs(remaining) < 1

  function updateLine(idx: number, field: keyof SplitLine, val: string) {
    setLines(prev => prev.map((l, i) => i === idx ? { ...l, [field]: val } : l))
  }

  function addLine() {
    setLines(prev => [...prev, { bucketId: '', memo: '', amount: '' }])
  }

  function removeLine(idx: number) {
    if (lines.length <= 2) return
    setLines(prev => prev.filter((_, i) => i !== idx))
  }

  async function handleSave() {
    if (!isBalanced) { setError('Split amounts must equal the total transaction amount'); return }

    setSaving(true)
    setError(null)
    try {
      for (const line of lines) {
        const milliunits = parseAmountToMilliunits(line.amount)
        if (milliunits === 0) continue
        const tx: CreateTransactionData = {
          accountId,
          bucketId: line.bucketId || null,
          payee,
          amountMilliunits: milliunits,
          date,
          memo: line.memo || null,
          isCleared: false,
        }
        await createTransaction(tx)
      }
      // Delete the original transaction now that it has been replaced by split lines
      if (originalId) await deleteTransaction(originalId)
      onSaved()
      onClose()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save split')
    } finally {
      setSaving(false)
    }
  }

  const isNeg = totalMilliunits < 0

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o && !saving) onClose() }}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Split Transaction</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="text-sm text-gray-600">
            <span className="font-medium">{payee}</span>
            {' · '}
            <span className={`font-mono font-medium ${isNeg ? 'text-red-600' : 'text-green-700'}`}>
              {formatMilliunits(totalMilliunits, currency)}
            </span>
            {' on '}
            {date}
          </div>

          {/* Split lines */}
          <div className="space-y-2">
            <div className="grid grid-cols-[1fr_1fr_auto_auto] gap-2 text-xs font-medium text-gray-500 px-1">
              <span>Bucket</span>
              <span>Memo</span>
              <span>Amount</span>
              <span></span>
            </div>
            {lines.map((line, i) => (
              <div key={i} className="grid grid-cols-[1fr_1fr_auto_auto] gap-2 items-center">
                <Select value={line.bucketId} onValueChange={(v) => updateLine(i, 'bucketId', v)}>
                  <SelectTrigger className="h-8 text-xs">
                    <SelectValue placeholder="Bucket (opt.)" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="">No bucket</SelectItem>
                    {buckets.map(b => (
                      <SelectItem key={b.id} value={b.id}>{b.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Input
                  placeholder="Memo"
                  value={line.memo}
                  onChange={(e) => updateLine(i, 'memo', e.target.value)}
                  className="h-8 text-xs"
                />
                <Input
                  placeholder="0.00"
                  value={line.amount}
                  onChange={(e) => updateLine(i, 'amount', e.target.value)}
                  className="h-8 w-24 text-xs font-mono text-right"
                />
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => removeLine(i)}
                  disabled={lines.length <= 2}
                  className="h-8 w-8 text-gray-400 hover:text-red-500"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            ))}
          </div>

          <Button variant="ghost" size="sm" onClick={addLine} className="text-blue-600 hover:text-blue-700 px-0">
            <Plus className="h-4 w-4 mr-1" />
            Add line
          </Button>

          {/* Balance indicator */}
          <div className={`text-sm p-2 rounded-md flex items-center justify-between ${isBalanced ? 'bg-green-50 text-green-700' : 'bg-yellow-50 text-yellow-700'}`}>
            <span>Remaining to assign:</span>
            <span className="font-mono font-medium">{formatMilliunits(remaining, currency)}</span>
          </div>

          {error && (
            <div className="flex items-center gap-2 text-red-600 text-sm bg-red-50 p-2 rounded-md">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {error}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={!isBalanced || saving}>
            {saving ? 'Saving...' : 'Save Split'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
