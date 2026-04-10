import { useState } from 'react'
import { CheckCircle, X } from 'lucide-react'
import { Button } from './ui/button'
import { Input } from './ui/input'
import { Label } from './ui/label'
import { type TransactionDto } from '@/lib/api'
import { formatMilliunits, parseAmountToMilliunits } from '@/lib/utils'

interface ReconcilePanelProps {
  transactions: TransactionDto[]
  currency: string
  onDone: () => void
  onCancel: () => void
}

export function ReconcilePanel({ transactions, currency, onDone, onCancel }: ReconcilePanelProps) {
  const [statementBalance, setStatementBalance] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const clearedMilliunits = transactions
    .filter(t => t.isCleared)
    .reduce((s, t) => s + t.amountMilliunits, 0)

  const targetMilliunits = parseAmountToMilliunits(statementBalance)
  const difference = targetMilliunits - clearedMilliunits
  const isBalanced = Math.abs(difference) < 1

  async function handleFinish() {
    if (!isBalanced) return
    setSaving(true)
    setError(null)
    try {
      // All currently cleared transactions stay cleared; nothing more to do
      // In a full reconciliation workflow, we'd mark them as "reconciled" status
      // For now, finishing reconciliation just closes the panel
      onDone()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to finish reconciliation')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="font-semibold text-blue-900 flex items-center gap-2">
          <CheckCircle className="h-5 w-5" />
          Reconcile Account
        </h3>
        <Button variant="ghost" size="icon" onClick={onCancel} className="h-7 w-7 text-blue-600">
          <X className="h-4 w-4" />
        </Button>
      </div>

      <p className="text-sm text-blue-700">
        Mark transactions as cleared until your cleared balance matches your bank statement.
        Check the <span className="font-medium">✓</span> column to toggle cleared status.
      </p>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label className="text-blue-800 text-xs">Cleared Balance</Label>
          <div className={`font-mono font-semibold mt-1 ${clearedMilliunits < 0 ? 'text-red-600' : 'text-green-700'}`}>
            {formatMilliunits(clearedMilliunits, currency)}
          </div>
        </div>
        <div>
          <Label className="text-blue-800 text-xs">Statement Balance</Label>
          <Input
            placeholder="0.00"
            value={statementBalance}
            onChange={(e) => setStatementBalance(e.target.value)}
            className="mt-1 h-8 font-mono"
          />
        </div>
      </div>

      {statementBalance !== '' && (
        <div className={`text-sm p-2 rounded flex items-center justify-between ${isBalanced ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'}`}>
          <span>Difference:</span>
          <span className="font-mono font-medium">{formatMilliunits(difference, currency)}</span>
        </div>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="flex gap-2">
        <Button variant="outline" size="sm" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
        <Button
          size="sm"
          onClick={handleFinish}
          disabled={!isBalanced || statementBalance === '' || saving}
        >
          {saving ? 'Finishing...' : 'Finish Reconciliation'}
        </Button>
      </div>
    </div>
  )
}
