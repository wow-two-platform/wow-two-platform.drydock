<script lang="ts">
/** Defines the placeholder row count for history loading. */
export interface HistoryTableSkeletonProps {
  /** The number of placeholder rows. */
  readonly rows?: number;
}
</script>

<script setup lang="ts">
/** Renders neutral loading placeholders without fictitious deployment records. */
defineOptions({ name: "HistoryTableSkeleton" });
const props = withDefaults(defineProps<HistoryTableSkeletonProps>(), {
  rows: 5,
});
</script>

<template>
  <div
    role="status"
    aria-label="Loading deployments"
    class="divide-y divide-border rounded-xl border border-border"
  >
    <div
      v-for="row in props.rows"
      :key="row"
      class="flex gap-6 p-4 motion-safe:animate-pulse"
    >
      <span class="h-4 w-20 rounded bg-muted" /><span
        class="h-4 flex-1 rounded bg-muted"
      />
      <span class="h-4 w-16 rounded bg-muted" />
    </div>
  </div>
</template>
