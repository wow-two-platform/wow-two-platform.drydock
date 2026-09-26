import { onScopeDispose, ref } from "vue";

/** Tracks explicit refreshes without allowing disposed components to retain delayed state. */
export function useRefresh(
  refetch: () => unknown,
  { minDuration = 400 }: { minDuration?: number } = {},
) {
  const refreshing = ref(false);
  const delays = new Map<ReturnType<typeof setTimeout>, () => void>();
  let pending = 0;
  let active = true;

  onScopeDispose(() => {
    active = false;
    for (const [timer, resolve] of delays) {
      clearTimeout(timer);
      resolve();
    }
    delays.clear();
  });

  async function refresh(): Promise<void> {
    pending += 1;
    refreshing.value = true;
    const delay = new Promise<void>((resolve) => {
      const timer = setTimeout(() => {
        delays.delete(timer);
        resolve();
      }, minDuration);
      delays.set(timer, resolve);
    });
    try {
      await Promise.all([Promise.resolve().then(refetch), delay]);
    } finally {
      pending -= 1;
      if (active && pending === 0) refreshing.value = false;
    }
  }

  return { refresh, refreshing } as const;
}
