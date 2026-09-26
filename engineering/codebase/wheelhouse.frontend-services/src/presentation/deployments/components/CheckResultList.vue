<script lang="ts">
import type { TargetCheck } from "@/domain/deployments";

/** Defines the readiness probes to display. */
export interface CheckResultListProps {
  /** The target's completed readiness response. */
  readonly check: TargetCheck;
}
</script>

<script setup lang="ts">
import { computed } from "vue";
import { CircleCheck, CircleX } from "lucide-vue-next";

/** Renders read-only readiness probes with failed checks first. */
defineOptions({ name: "CheckResultList" });
const props = defineProps<CheckResultListProps>();
const checks = computed(() =>
  [...props.check.checks].sort(
    (left, right) => Number(left.ok) - Number(right.ok),
  ),
);
</script>

<template>
  <ul class="flex flex-col gap-2 text-sm" aria-label="Readiness checks">
    <li v-for="item in checks" :key="item.name" class="flex items-start gap-2">
      <CircleCheck
        v-if="item.ok"
        :size="16"
        class="mt-0.5 shrink-0 text-success"
        aria-label="Passed"
      />
      <CircleX
        v-else
        :size="16"
        class="mt-0.5 shrink-0 text-destructive"
        aria-label="Failed"
      />
      <div class="min-w-0">
        <span class="font-medium">{{ item.name }}</span>
        <p :class="item.ok ? 'text-muted-foreground' : 'text-destructive'">
          {{ item.detail }}
        </p>
      </div>
    </li>
  </ul>
</template>
