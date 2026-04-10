import { useState } from 'react'
import { Upload, Scale, Plus, LogOut, BarChart2 } from 'lucide-react'
import { useAccounts } from '@/hooks/useAccounts'
import { useTransactions } from '@/hooks/useTransactions'
import { useBuckets } from '@/hooks/useBuckets'
import { TransactionRegister } from '@/components/TransactionRegister'
import { ReconcilePanel } from '@/components/ReconcilePanel'
import { CsvImportDialog } from '@/components/CsvImportDialog'
import { ReportsPage } from '@/pages/ReportsPage'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { createAccount } from '@/lib/api'
import { clearToken } from '@/lib/auth'
import { formatMilliunits } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface AccountRegisterPageProps {
  onLogout: () => void
}

export function AccountRegisterPage({ onLogout }: AccountRegisterPageProps) {
  const { accounts, loading: accountsLoading, reload: reloadAccounts } = useAccounts()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [page, setPage] = useState<'accounts' | 'reports'>('accounts')
  const { transactions, loading: txLoading, reload: reloadTx } = useTransactions(selectedId)
  const { buckets } = useBuckets()

  const [reconcileMode, setReconcileMode] = useState(false)
  const [csvOpen, setCsvOpen] = useState(false)
  const [newAccountName, setNewAccountName] = useState('')
  const [showNewAccount, setShowNewAccount] = useState(false)

  const selectedAccount = accounts.find(a => a.id === selectedId) ?? null

  async function handleCreateAccount() {
    if (!newAccountName.trim()) return
    try {
      const { id } = await createAccount({ name: newAccountName.trim(), currencyCode: 'USD' })
      await reloadAccounts()
      setSelectedId(id)
      setNewAccountName('')
      setShowNewAccount(false)
    } catch (e) {
      console.error(e)
    }
  }

  function handleLogout() {
    clearToken()
    onLogout()
  }

  const openAccounts = accounts.filter(a => !a.isClosed)
  const closedAccounts = accounts.filter(a => a.isClosed)

  return (
    <div className="flex h-screen bg-gray-100 overflow-hidden">
      {/* Sidebar */}
      <div className="w-56 bg-gray-900 text-gray-100 flex flex-col shrink-0">
        <div className="p-4 border-b border-gray-700">
          <h1 className="font-bold text-lg text-white">BucketBudget</h1>
        </div>

        <div className="flex-1 overflow-y-auto p-3 space-y-1">
          <button
            onClick={() => setPage('reports')}
            className={cn(
              'w-full text-left px-3 py-2 rounded-md text-sm transition-colors flex items-center gap-2 mb-2',
              page === 'reports'
                ? 'bg-blue-600 text-white'
                : 'text-gray-300 hover:bg-gray-700 hover:text-white'
            )}
          >
            <BarChart2 className="h-4 w-4 shrink-0" />
            Reports
          </button>
          <p className="text-xs font-semibold text-gray-400 px-2 py-1 uppercase tracking-wider">Accounts</p>

          {accountsLoading ? (
            <p className="text-xs text-gray-500 px-2">Loading...</p>
          ) : (
            openAccounts.map(account => (
              <button
                key={account.id}
                onClick={() => { setSelectedId(account.id); setReconcileMode(false); setPage('accounts') }}
                className={cn(
                  'w-full text-left px-3 py-2 rounded-md text-sm transition-colors',
                  page === 'accounts' && selectedId === account.id
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                )}
              >
                <div className="flex items-center justify-between">
                  <span className="truncate">{account.name}</span>
                  <span className={cn(
                    'text-xs font-mono ml-1 shrink-0',
                    selectedId === account.id ? 'text-blue-200' : 'text-gray-400',
                    account.balanceMilliunits < 0 && 'text-red-400'
                  )}>
                    {(account.balanceMilliunits / 1000).toFixed(0)}
                  </span>
                </div>
                <div className={cn('text-xs', selectedId === account.id ? 'text-blue-200' : 'text-gray-500')}>
                  {account.currencyCode}
                </div>
              </button>
            ))
          )}

          {closedAccounts.length > 0 && (
            <>
              <p className="text-xs font-semibold text-gray-500 px-2 py-1 uppercase tracking-wider mt-3">Closed</p>
              {closedAccounts.map(account => (
                <button
                  key={account.id}
                  onClick={() => { setSelectedId(account.id); setReconcileMode(false); setPage('accounts') }}
                  className={cn(
                    'w-full text-left px-3 py-2 rounded-md text-sm transition-colors opacity-50',
                    page === 'accounts' && selectedId === account.id ? 'bg-gray-600 text-white' : 'text-gray-400 hover:bg-gray-700'
                  )}
                >
                  <span className="truncate">{account.name}</span>
                </button>
              ))}
            </>
          )}

          {/* New account form */}
          {showNewAccount ? (
            <div className="px-2 pt-2 space-y-2">
              <input
                className="w-full bg-gray-800 text-white text-sm rounded px-2 py-1.5 border border-gray-600 focus:outline-none focus:border-blue-400"
                placeholder="Account name"
                value={newAccountName}
                onChange={e => setNewAccountName(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter') handleCreateAccount()
                  if (e.key === 'Escape') setShowNewAccount(false)
                }}
                autoFocus
              />
              <div className="flex gap-1">
                <button
                  onClick={handleCreateAccount}
                  className="flex-1 text-xs bg-blue-600 hover:bg-blue-500 text-white py-1 rounded"
                >
                  Add
                </button>
                <button
                  onClick={() => setShowNewAccount(false)}
                  className="flex-1 text-xs bg-gray-700 hover:bg-gray-600 text-gray-200 py-1 rounded"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <button
              onClick={() => setShowNewAccount(true)}
              className="w-full text-left px-3 py-2 text-xs text-gray-500 hover:text-gray-300 flex items-center gap-1"
            >
              <Plus className="h-3 w-3" />
              Add account
            </button>
          )}
        </div>

        <div className="p-3 border-t border-gray-700">
          <button
            onClick={handleLogout}
            className="flex items-center gap-2 text-xs text-gray-400 hover:text-gray-200 w-full px-2 py-1.5"
          >
            <LogOut className="h-3.5 w-3.5" />
            Sign out
          </button>
        </div>
      </div>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {page === 'reports' ? (
          <ReportsPage />
        ) : selectedAccount ? (
          <>
            {/* Header */}
            <div className="bg-white border-b px-6 py-3 flex items-center justify-between shrink-0">
              <div>
                <div className="flex items-center gap-3">
                  <h2 className="text-xl font-semibold text-gray-900">{selectedAccount.name}</h2>
                  <Badge variant="secondary" className="text-xs">{selectedAccount.currencyCode}</Badge>
                  {selectedAccount.isClosed && <Badge variant="outline" className="text-xs">Closed</Badge>}
                </div>
                <div className={cn(
                  'text-sm font-mono mt-0.5',
                  selectedAccount.balanceMilliunits < 0 ? 'text-red-600' : 'text-gray-600'
                )}>
                  Balance: {formatMilliunits(selectedAccount.balanceMilliunits, selectedAccount.currencyCode)}
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCsvOpen(true)}
                >
                  <Upload className="h-4 w-4 mr-1" />
                  Import CSV
                </Button>
                <Button
                  variant={reconcileMode ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setReconcileMode(r => !r)}
                >
                  <Scale className="h-4 w-4 mr-1" />
                  {reconcileMode ? 'Exit Reconcile' : 'Reconcile'}
                </Button>
              </div>
            </div>

            {/* Body */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {reconcileMode && (
                <ReconcilePanel
                  transactions={transactions}
                  currency={selectedAccount.currencyCode}
                  onDone={() => { setReconcileMode(false); reloadTx() }}
                  onCancel={() => setReconcileMode(false)}
                />
              )}

              {txLoading ? (
                <div className="text-center text-gray-400 py-12">Loading transactions...</div>
              ) : (
                <TransactionRegister
                  accountId={selectedAccount.id}
                  currency={selectedAccount.currencyCode}
                  transactions={transactions}
                  buckets={buckets}
                  onChanged={() => { reloadTx(); reloadAccounts() }}
                  reconcileMode={reconcileMode}
                />
              )}
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center text-gray-400">
            <div className="text-4xl mb-4">💰</div>
            <p className="text-lg font-medium text-gray-600">Select an account</p>
            <p className="text-sm mt-1">Choose an account from the sidebar to view its register</p>
            {accounts.length === 0 && !accountsLoading && (
              <button
                onClick={() => setShowNewAccount(true)}
                className="mt-4 text-sm text-blue-600 hover:underline"
              >
                Create your first account →
              </button>
            )}
          </div>
        )}
      </div>

      {/* CSV import dialog */}
      {selectedAccount && (
        <CsvImportDialog
          open={csvOpen}
          onClose={() => setCsvOpen(false)}
          accountId={selectedAccount.id}
          onImported={() => { reloadTx(); reloadAccounts() }}
        />
      )}
    </div>
  )
}
