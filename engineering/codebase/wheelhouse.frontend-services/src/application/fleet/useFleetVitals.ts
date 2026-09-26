import { useEffect } from 'react';
import { useAppQuery } from '@wow-two-beta/ui/query';
import { fleetApi } from '@/integration/fleet';
import { FleetKeys } from './FleetKeys';

const REFRESH_MS = 60_000;

/** Every target's host and container vitals, re-read each minute while the page is visible. */
export function useFleetVitals() {
  const vitals = useAppQuery({
    key: FleetKeys.vitals,
    queryFn: ({ signal }) => fleetApi.getVitals(signal),
    meta: { suppressGlobalError: true },
  });
  const { refetch } = vitals;

  // useAppQuery exposes no refetch interval, so the periodic read stays inside this hook. Several panels share
  // the query; joining an in-flight read keeps it to one SSH pass per interval.
  useEffect(() => {
    const timer = window.setInterval(() => {
      if (document.visibilityState === 'visible') void refetch({ cancelRefetch: false });
    }, REFRESH_MS);
    return () => window.clearInterval(timer);
  }, [refetch]);

  return vitals;
}
