<script lang="ts">
/** A value that turns into a placeholder of its own size while its region loads. */
export interface SkeletonStateSlotProps {
  /** Overrides the nearest `SkeletonStateGroup`'s loading flag. */
  readonly isLoading?: boolean | undefined;
  /** The placeholder's corners: `text` for words, `rect` for blocks, `circle` for pills. Default `text`. */
  readonly shape?: "text" | "rect" | "circle";
  /** Renders a block-level wrapper for block content — bars, charts, cards. */
  readonly isBlock?: boolean;
}

/** @internal The placeholder corners per shape; the size always comes from the content. */
const ShapeClass = {
  text: "rounded-sm",
  rect: "rounded-md",
  circle: "rounded-full",
} as const;
</script>
<script setup lang="ts">
import { computed } from "vue";
import { useSkeletonStateGroup } from "./SkeletonStateContext";

/** Keeps a value mounted but invisible while loading, so labels and layout hold still. */
defineOptions({ name: "SkeletonStateSlot" });
/* `isLoading: undefined` is load-bearing: Vue casts an absent `Boolean` prop to `false`, which would override
   the group's flag. */
const props = withDefaults(defineProps<SkeletonStateSlotProps>(), {
  isLoading: undefined,
  shape: "text",
  isBlock: false,
});
defineSlots<{ default?(): unknown }>();
const group = useSkeletonStateGroup();
const loading = computed(
  () => props.isLoading ?? group?.isLoading.value ?? false,
);
</script>
<template>
  <component
    :is="props.isBlock ? 'div' : 'span'"
    :class="[
      props.isBlock ? 'block' : 'inline-block',
      loading &&
        `pointer-events-none select-none animate-pulse bg-muted text-transparent motion-reduce:animate-none [&_*]:invisible ${ShapeClass[props.shape]}`,
    ]"
    :aria-hidden="loading || undefined"
    :data-loading="loading || undefined"
    ><slot
  /></component>
</template>
