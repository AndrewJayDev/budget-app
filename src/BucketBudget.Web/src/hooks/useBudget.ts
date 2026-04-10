import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { formatYearMonth } from '@/lib/utils'

export function useMonthBudget(year: number, month: number) {
  return useQuery({
    queryKey: ['budget', formatYearMonth(year, month)],
    queryFn: () => api.getMonthBudget(year, month),
  })
}

export function useAccounts() {
  return useQuery({
    queryKey: ['accounts'],
    queryFn: () => api.getAccounts(),
  })
}

export function useUpsertAllocation(year: number, month: number) {
  const queryClient = useQueryClient()
  const key = formatYearMonth(year, month)

  return useMutation({
    mutationFn: ({ bucketId, allocatedMilliunits }: { bucketId: string; allocatedMilliunits: number }) =>
      api.upsertAllocation(year, month, bucketId, allocatedMilliunits),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budget', key] })
    },
  })
}

export function useBatchUpsertAllocations(year: number, month: number) {
  const queryClient = useQueryClient()
  const key = formatYearMonth(year, month)

  return useMutation({
    mutationFn: (updates: { bucketId: string; allocatedMilliunits: number }[]) =>
      Promise.all(
        updates.map((u) => api.upsertAllocation(year, month, u.bucketId, u.allocatedMilliunits))
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budget', key] })
    },
  })
}
