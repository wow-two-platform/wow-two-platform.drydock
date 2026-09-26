import { useAppQuery } from '@wow-two-beta/ui/query';
import { systemApi } from '@/integration/system';

/** Whether the management API answers, and whether it drives the local rehearsal rig. */
export function useApiConnection() {
  const { data, error } = useAppQuery({
    key: ['system', 'status'],
    queryFn: ({ signal }) => systemApi.getStatus(signal),
    meta: { suppressGlobalError: true },
  });
  const connection = error ? 'offline' : data ? 'online' : 'checking';
  return { connection, localRig: data?.localRig === true } as const;
}
