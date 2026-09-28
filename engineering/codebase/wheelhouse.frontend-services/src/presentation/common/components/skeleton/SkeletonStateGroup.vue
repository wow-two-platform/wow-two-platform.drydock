<script lang="ts">
/** A loading region: its labels stay, its slotted values turn into placeholders together. */
export interface SkeletonStateGroupProps {
  /** Whether the region is loading; every `SkeletonStateSlot` inside follows it. */
  readonly isLoading: boolean;
  /** The one announcement for the region while it loads. Default `Loading…`. */
  readonly label?: string;
}
</script>
<script setup lang="ts">
import { computed, provide } from "vue";
import { skeletonStateGroupKey } from "./SkeletonStateContext";

/** Marks a region busy, announces once, and switches every slot inside at the same moment. */
defineOptions({ name: "SkeletonStateGroup" });
const props = defineProps<SkeletonStateGroupProps>();
defineSlots<{ default?(): unknown }>();
provide(skeletonStateGroupKey, { isLoading: computed(() => props.isLoading) });
</script>
<template>
  <div :aria-busy="props.isLoading || undefined">
    <span v-if="props.isLoading" role="status" class="sr-only">{{
      props.label ?? "Loading…"
    }}</span>
    <slot />
  </div>
</template>
