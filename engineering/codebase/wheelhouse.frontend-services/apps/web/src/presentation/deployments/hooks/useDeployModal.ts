import { ref, shallowRef } from "vue";

import type { DeploySelection } from "../components/DeployModal.vue";

/** Manages one deployment dialog; key it by session to reset each opening. */
export function useDeployModal() {
  const open = ref(false);
  const session = ref(0);
  const selection = shallowRef<DeploySelection | null>(null);

  function openDeploy(value: DeploySelection | null = null): void {
    selection.value = value;
    session.value += 1;
    open.value = true;
  }

  function onOpenChange(value: boolean): void {
    open.value = value;
  }

  return { open, session, selection, openDeploy, onOpenChange };
}
