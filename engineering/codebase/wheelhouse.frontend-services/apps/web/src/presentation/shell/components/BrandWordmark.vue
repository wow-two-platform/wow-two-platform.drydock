<script lang="ts">
/** Sizes the Wheelhouse wordmark. */
export interface BrandWordmarkProps {
  /** The rendered height in CSS pixels; the width follows the artwork. */
  readonly height?: number;
}

/** @internal The exported artwork's width and height, from `product/brand/soft-folds`. */
const Artwork = { width: 691, height: 96 } as const;
</script>
<script setup lang="ts">
import { computed } from 'vue';
import darkInk from '../assets/wordmark-dark-ink.png';
import lightInk from '../assets/wordmark-light-ink.png';

/**
 * Shows the Soft folds wordmark, the primary horizontal composition: dark lettering on light surfaces and light
 * lettering on dark ones. Placement and minimum sizes: `product/brand/brand.md`.
 */
defineOptions({ name: 'BrandWordmark' });
const props = withDefaults(defineProps<BrandWordmarkProps>(), { height: 24 });
defineSlots<{}>();
const width = computed(() => Math.round((props.height * Artwork.width) / Artwork.height));
</script>
<template>
  <span class="inline-flex shrink-0">
    <img :src="darkInk" alt="Wheelhouse" :width="width" :height="props.height" class="dark:hidden" />
    <img :src="lightInk" alt="Wheelhouse" :width="width" :height="props.height" class="hidden dark:block" />
  </span>
</template>
