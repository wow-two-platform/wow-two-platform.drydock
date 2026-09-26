import { computed } from "vue";
import { useAppQuery } from "@/bootstrap/query";
import { systemApi } from "@/integration/system";

/** Whether the management API answers and whether it drives the rehearsal rig. */
export function useApiConnection() {
  const { data, error } = useAppQuery({
    key: ["system", "status"],
    queryFn: ({ signal }) => systemApi.getStatus(signal),
    meta: { suppressGlobalError: true },
  });
  const connection = computed(() =>
    error.value ? "offline" : data.value ? "online" : "checking",
  );
  const localRig = computed(() => data.value?.localRig === true);
  return { connection, localRig } as const;
}
