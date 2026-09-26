import { useCallback, useState } from 'react';
import type { DeploySelection } from '../components/DeployModal';

/**
 * Opens the deploy modal, optionally preselected. Each opening is a new `session`; key the modal by it, so it
 * starts fresh while a closing one keeps its exit animation.
 */
export function useDeployModal() {
  const [state, setState] = useState({ open: false, session: 0, selection: null as DeploySelection | null });

  const open = useCallback((selection: DeploySelection | null = null) => {
    setState((current) => ({ open: true, session: current.session + 1, selection }));
  }, []);

  const onOpenChange = useCallback((open: boolean) => setState((current) => ({ ...current, open })), []);

  return { ...state, openDeploy: open, onOpenChange } as const;
}
