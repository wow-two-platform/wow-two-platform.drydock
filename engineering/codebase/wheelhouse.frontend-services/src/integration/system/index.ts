import type { SystemStatus } from '@/domain/auth';
import { request } from '@/integration/common';

/** The service liveness endpoint; its body is not enveloped. */
export const systemApi = {
  getStatus: (signal?: AbortSignal) => request<SystemStatus>('/api/system/status', { signal }),
};
