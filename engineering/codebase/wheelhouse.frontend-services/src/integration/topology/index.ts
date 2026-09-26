import type { ServiceTopology } from "@/domain/topology";
import { requestData } from "@/integration/common";
import { ServiceTopologySchema } from "./schemas";

/** Reads the saved Compose declaration for an exact target without executing any operation. */
export const topologyApi = {
  getTargetTopology: (target: string, signal?: AbortSignal) =>
    requestData<ServiceTopology>(
      `/api/deployments/targets/${encodeURIComponent(target)}/topology`,
      ServiceTopologySchema.refine((value) => value.targetId === target),
      { signal },
    ),
};
