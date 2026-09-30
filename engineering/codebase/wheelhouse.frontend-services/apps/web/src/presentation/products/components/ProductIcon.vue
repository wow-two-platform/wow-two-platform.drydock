<script lang="ts">
/** A product's mark: the icon its repository carries, else a monogram tinted by its name. */
export interface ProductIconProps {
  /** The catalog slug; without one (a product outside the catalog) the monogram shows. */
  readonly productSlug?: string | null | undefined;
  /** The product's name, for the monogram and its tint. */
  readonly name: string;
  readonly size?: "sm" | "md" | "lg";
}

/** @internal The box per size; the image keeps a small inset so edge-to-edge marks still breathe. */
const SizeClass = {
  sm: "size-8 rounded-lg text-xs",
  md: "size-10 rounded-lg text-sm",
  lg: "size-12 rounded-xl text-base",
} as const;
</script>
<script setup lang="ts">
import { computed, ref, watch } from "vue";

/** Renders the product's own icon, falling back to a stable tinted monogram so products stay distinguishable. */
defineOptions({ name: "ProductIcon" });
const props = withDefaults(defineProps<ProductIconProps>(), { size: "md" });
defineSlots<{}>();

const failed = ref(false);
/** A new product gets a fresh attempt at its icon. */
watch(
  () => props.productSlug,
  () => {
    failed.value = false;
  },
);
const showsImage = computed(() => Boolean(props.productSlug) && !failed.value);
const monogram = computed(
  () => props.name.trim().slice(0, 1).toUpperCase() || "?",
);
/** The same name always lands on the same hue; mixing with theme tokens keeps it readable in light and dark. */
const tint = computed(() => {
  const hue = [...props.name].reduce(
    (sum, character) => (sum * 31 + character.charCodeAt(0)) % 360,
    17,
  );
  return {
    backgroundColor: `color-mix(in oklch, hsl(${hue} 70% 50%) 18%, var(--color-card))`,
    color: `color-mix(in oklch, hsl(${hue} 70% 40%) 75%, var(--color-foreground))`,
  };
});
</script>
<template>
  <span
    class="relative flex shrink-0 items-center justify-center overflow-hidden border border-border font-semibold"
    :class="[SizeClass[props.size], showsImage ? 'bg-card' : '']"
    :style="showsImage ? undefined : tint"
    aria-hidden="true"
  >
    <img
      v-if="showsImage"
      :src="`/api/products/${encodeURIComponent(props.productSlug ?? '')}/icon`"
      alt=""
      loading="lazy"
      class="size-full object-contain p-1"
      @error="failed = true"
    />
    <template v-else>{{ monogram }}</template>
  </span>
</template>
