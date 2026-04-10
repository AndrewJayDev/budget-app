import React, { useState, useRef } from 'react'
import { Upload, AlertCircle } from 'lucide-react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from './ui/dialog'
import { Button } from './ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from './ui/table'
import { bulkCreateTransactions, type CreateTransactionData } from '@/lib/api'
import { parseAmountToMilliunits, todayIso } from '@/lib/utils'

interface CsvImportDialogProps {
  open: boolean
  onClose: () => void
  accountId: string
  onImported: () => void
}

interface ParsedRow {
  date: string
  payee: string
  amount: string
  memo: string
  isCleared: boolean
}

function parseCSV(text: string): string[][] {
  const lines = text.split(/\r?\n/).filter(l => l.trim())
  return lines.map(line => {
    const fields: string[] = []
    let cur = ''
    let inQuote = false
    for (let i = 0; i < line.length; i++) {
      const c = line[i]
      if (c === '"') {
        if (inQuote && line[i + 1] === '"') { cur += '"'; i++ }
        else inQuote = !inQuote
      } else if (c === ',' && !inQuote) {
        fields.push(cur.trim()); cur = ''
      } else {
        cur += c
      }
    }
    fields.push(cur.trim())
    return fields
  })
}

function detectColumns(headers: string[]): { date: number; payee: number; amount: number; outflow: number; inflow: number; memo: number } {
  const h = headers.map(h => h.toLowerCase().replace(/[^a-z]/g, ''))
  const find = (...names: string[]) => names.reduce((found, name) => found !== -1 ? found : h.indexOf(name), -1)
  return {
    date: find('date', 'transactiondate'),
    payee: find('payee', 'description', 'memo', 'name'),
    amount: find('amount', 'value'),
    outflow: find('outflow', 'debit', 'withdrawal'),
    inflow: find('inflow', 'credit', 'deposit'),
    memo: find('memo', 'notes', 'note', 'reference'),
  }
}

export function CsvImportDialog({ open, onClose, accountId, onImported }: CsvImportDialogProps) {
  const [rows, setRows] = useState<ParsedRow[]>([])
  const [error, setError] = useState<string | null>(null)
  const [importing, setImporting] = useState(false)
  const [fileName, setFileName] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setFileName(file.name)
    setError(null)

    const reader = new FileReader()
    reader.onload = (ev) => {
      const text = ev.target?.result as string
      const raw = parseCSV(text)
      if (raw.length < 2) { setError('CSV has no data rows'); return }
      const hdrs = raw[0]
      const cols = detectColumns(hdrs)

      const parsed: ParsedRow[] = raw.slice(1).map(row => {
        let amount = ''
        if (cols.outflow !== -1 && cols.inflow !== -1) {
          const out = row[cols.outflow] || '0'
          const infl = row[cols.inflow] || '0'
          const outVal = parseAmountToMilliunits(out)
          const inflVal = parseAmountToMilliunits(infl)
          const net = inflVal - outVal
          amount = String(net / 1000)
        } else if (cols.amount !== -1) {
          amount = row[cols.amount] || '0'
        }

        return {
          date: cols.date !== -1 ? normalizeDate(row[cols.date]) : todayIso(),
          payee: cols.payee !== -1 ? row[cols.payee] || 'Unknown' : 'Unknown',
          amount,
          memo: cols.memo !== -1 ? row[cols.memo] || '' : '',
          isCleared: false,
        }
      }).filter(r => r.payee !== '' || r.amount !== '')

      setRows(parsed)
    }
    reader.readAsText(file)
  }

  function normalizeDate(raw: string): string {
    if (!raw) return todayIso()
    // Try MM/DD/YYYY
    const match = raw.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/)
    if (match) return `${match[3]}-${match[1].padStart(2, '0')}-${match[2].padStart(2, '0')}`
    // Try YYYY-MM-DD
    if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw
    return todayIso()
  }

  async function handleImport() {
    if (!rows.length) return
    setImporting(true)
    setError(null)
    try {
      const transactions: CreateTransactionData[] = rows.map(r => ({
        accountId,
        payee: r.payee,
        amountMilliunits: parseAmountToMilliunits(r.amount),
        date: r.date,
        memo: r.memo || null,
        isCleared: r.isCleared,
      }))
      await bulkCreateTransactions(transactions)
      onImported()
      onClose()
      setRows([])
      setFileName(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Import failed')
    } finally {
      setImporting(false)
    }
  }

  function handleClose() {
    if (!importing) {
      onClose()
      setRows([])
      setFileName(null)

      setError(null)
    }
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) handleClose() }}>
      <DialogContent className="max-w-3xl max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle>Import CSV</DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto space-y-4">
          {/* File picker */}
          <div
            className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center cursor-pointer hover:border-blue-400 transition-colors"
            onClick={() => fileRef.current?.click()}
          >
            <Upload className="h-8 w-8 text-gray-400 mx-auto mb-2" />
            <p className="text-sm text-gray-600">
              {fileName ? fileName : 'Click to select a CSV file'}
            </p>
            <p className="text-xs text-gray-400 mt-1">
              Supports Date, Payee, Amount, Outflow/Inflow columns
            </p>
            <input ref={fileRef} type="file" accept=".csv" className="hidden" onChange={handleFile} />
          </div>

          {error && (
            <div className="flex items-center gap-2 text-red-600 text-sm bg-red-50 p-3 rounded-md">
              <AlertCircle className="h-4 w-4 shrink-0" />
              {error}
            </div>
          )}

          {rows.length > 0 && (
            <div>
              <p className="text-sm text-gray-600 mb-2">{rows.length} transactions to import</p>
              <div className="border rounded-md overflow-auto max-h-64">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date</TableHead>
                      <TableHead>Payee</TableHead>
                      <TableHead className="text-right">Amount</TableHead>
                      <TableHead>Memo</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rows.map((row, i) => {
                      const milliunits = parseAmountToMilliunits(row.amount)
                      const isNeg = milliunits < 0
                      return (
                        <TableRow key={i}>
                          <TableCell className="text-xs">{row.date}</TableCell>
                          <TableCell className="text-xs">{row.payee}</TableCell>
                          <TableCell className={`text-right text-xs font-mono ${isNeg ? 'text-red-600' : 'text-green-700'}`}>
                            {(milliunits / 1000).toFixed(2)}
                          </TableCell>
                          <TableCell className="text-xs text-gray-500">{row.memo}</TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </div>
            </div>
          )}
        </div>

        <DialogFooter className="mt-4">
          <Button variant="outline" onClick={handleClose} disabled={importing}>Cancel</Button>
          <Button onClick={handleImport} disabled={!rows.length || importing}>
            {importing ? 'Importing...' : `Import ${rows.length} transactions`}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
