<script lang="ts">
/** Defines a current host or container resource reading. */
export interface ResourceMeterProps {
  readonly label: string;
  readonly value: number | null;
  readonly detail?: string;
  readonly refreshing?: boolean;
}
</script>
<script setup lang="ts">
import { MeterBar, SkeletonState } from '@wow-two-beta/ui-vue/presentation/feedback';
import { USAGE_THRESHOLDS } from '@/domain/servers';

/** Presents a current resource snapshot without inventing history or unknown values. */
defineOptions({ name: 'ResourceMeter' });
const props = defineProps<ResourceMeterProps>();
</script>
<template>
  <div class="min-w-0 rounded-xl border border-border bg-card p-3">
    <div class="mb-2 flex items-center justify-between gap-3 text-xs">
      <span class="truncate text-muted-foreground">{{ props.label }}</span
      ><SkeletonState v-if="props.refreshing" class="h-4 w-10" /><span v-else class="font-medium tabular-nums">{{
        props.value == null ? 'Unavailable' : `${Math.round(props.value)}%`
      }}</span>
    </div>
    <SkeletonState v-if="props.refreshing" class="h-1.5 w-full" />
    <MeterBar
      v-else-if="props.value != null"
      :value="props.value"
      :thresholds="USAGE_THRESHOLDS"
      :label="props.label"
      size="sm"
    />
    <div v-else class="h-1.5 rounded-full bg-muted" />
    <p v-if="props.detail" class="mt-2 truncate text-xs text-muted-foreground" :title="props.detail">
      {{ props.detail }}
    </p>
  </div>
</template>
