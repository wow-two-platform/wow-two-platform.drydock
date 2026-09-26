<script lang="ts">
/** Defines the state of an operational read, including retained data during errors. */
export interface LoadStateProps {
  readonly loading: boolean;
  readonly error?: { readonly message: string } | string | null;
  readonly empty?: boolean;
  readonly emptyTitle?: string;
  readonly emptyDescription?: string;
  readonly hasData?: boolean;
}
</script>
<script setup lang="ts">
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { EmptyState } from '@wow-two-beta/ui-vue/presentation/display';
import { SkeletonState } from '@wow-two-beta/ui-vue/presentation/feedback';

/** Keeps pending, failed, empty, and retained operational data distinct. */
defineOptions({ name: 'LoadState' });
const props = defineProps<LoadStateProps>();
const emit = defineEmits<{ retry: [] }>();
defineSlots<{ default(): unknown; skeleton(): unknown; emptyActions(): unknown }>();
</script>
<template>
  <div v-if="props.loading" role="status" aria-label="Loading data" class="space-y-3">
    <slot name="skeleton"> <SkeletonState class="h-8 w-2/3" /><SkeletonState class="h-24 w-full" /> </slot>
  </div>
  <template v-else>
    <div
      v-if="props.error"
      role="alert"
      class="mb-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-destructive/30 bg-destructive-soft p-4 text-sm text-destructive-soft-foreground"
    >
      <span>{{ typeof props.error === 'string' ? props.error : props.error.message }}</span>
      <Button size="sm" variant="outline" @click="emit('retry')">Retry</Button>
    </div>
    <template v-if="!props.error || props.hasData">
      <div v-if="props.empty" class="py-8 text-center">
        <EmptyState :title="props.emptyTitle ?? 'Nothing here yet'" :description="props.emptyDescription ?? ''" />
        <slot name="emptyActions" />
      </div>
      <slot v-else />
    </template>
  </template>
</template>
