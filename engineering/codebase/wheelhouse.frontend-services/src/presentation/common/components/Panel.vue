<script lang="ts">
/** Defines a titled operational region in the Wheelhouse workspace. */
export interface PanelProps {
  readonly title: string;
  readonly description?: string;
}
</script>
<script setup lang="ts">
import { Card } from '@wow-two-beta/ui-vue/presentation/display';

/** Groups related operational data and its local actions. */
defineOptions({ name: 'Panel' });
const props = defineProps<PanelProps>();
defineSlots<{ default(): unknown; actions(): unknown }>();
</script>
<template>
  <Card class="wh-glass min-w-0 overflow-hidden rounded-2xl border border-(--wh-glass-edge) bg-(--wh-glass-fill) shadow-(--wh-glass-shadow)">
    <header class="wh-glass-subtle flex flex-wrap items-center justify-between gap-3 border-b border-border px-5 py-4">
      <div class="min-w-0">
        <h2 class="text-base font-semibold tracking-tight">{{ props.title }}</h2>
        <p v-if="props.description" class="mt-1 text-sm text-muted-foreground">{{ props.description }}</p>
      </div>
      <div v-if="$slots.actions" class="flex flex-wrap items-center gap-2"><slot name="actions" /></div>
    </header>
    <div class="min-w-0 bg-card p-5"><slot /></div>
  </Card>
</template>
