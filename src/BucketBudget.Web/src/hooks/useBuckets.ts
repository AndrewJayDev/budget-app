import { useState, useEffect, useCallback } from 'react'
import { getMonthBudget, type BucketDto, type BucketGroupDto } from '@/lib/api'

export function useBuckets() {
  const [bucketGroups, setBucketGroups] = useState<BucketGroupDto[]>([])
  const [buckets, setBuckets] = useState<BucketDto[]>([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    const now = new Date()
    try {
      const data = await getMonthBudget(now.getFullYear(), now.getMonth() + 1)
      setBucketGroups(data.bucketGroups ?? [])
      const flat: BucketDto[] = (data.bucketGroups ?? []).flatMap(g => g.buckets ?? [])
      setBuckets(flat)
    } catch {
      // buckets may not be configured yet
      setBucketGroups([])
      setBuckets([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  return { buckets, bucketGroups, loading }
}
