<script lang="ts">
/** Namespace navigation within one vault. */
export interface NamespaceListProps {
  vault: string;
  selected: string;
}
</script>

<script setup lang="ts">
import { ref, watch } from "vue";
import { Plus } from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import { SkeletonState } from "@wow-two-beta/ui-vue/presentation/feedback";
import { useVaultNamespaces } from "@/application/secrets";
import LoadState from "@/presentation/common/components/LoadState.vue";
import NamespaceModal from "./NamespaceModal.vue";

/** Selects a namespace and exposes scoped namespace creation. */
defineOptions({ name: "NamespaceList" });
const props = defineProps<NamespaceListProps>();
const emit = defineEmits<{ select: [slug: string] }>();
defineSlots<{}>();

const { data, loading, error, refetch } = useVaultNamespaces(() => props.vault);
const adding = ref(false);

/** Selects an available namespace when the catalog arrives or removes the current selection. */
watch(
  data,
  (items) => {
    if (items && !items.some((item) => item.slug === props.selected))
      emit("select", items[0]?.slug ?? "");
  },
  { immediate: true },
);
/** Closes namespace creation when the vault changes. */
watch(
  () => props.vault,
  () => {
    adding.value = false;
  },
);
</script>

<template>
  <div class="flex flex-col gap-3">
    <div class="flex h-8 items-center justify-between gap-2">
      <span
        class="text-xs font-semibold uppercase tracking-wide text-muted-foreground"
        >Namespaces</span
      >
      <Button variant="ghost" tone="neutral" size="sm" @click="adding = true">
        <template #leading><Plus :size="14" /></template>
        New
      </Button>
    </div>
    <LoadState
      :loading="loading"
      :error="error"
      :has-data="data !== undefined"
      :empty="!data?.length"
      empty-title="No namespaces yet"
      empty-description="Create one per product environment."
      @retry="refetch"
    >
      <template #skeleton>
        <div
          class="flex flex-col gap-2"
          role="status"
          aria-label="Loading namespaces"
        >
          <SkeletonState class="h-12 w-full" /><SkeletonState
            class="h-12 w-full"
          />
        </div>
      </template>
      <ul class="flex flex-col gap-1">
        <li v-for="item in data ?? []" :key="item.slug">
          <button
            type="button"
            :aria-current="item.slug === selected ? 'true' : undefined"
            class="flex min-h-14 w-full flex-col justify-center rounded-xl px-3 py-2 text-left transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            :class="
              item.slug === selected
                ? 'bg-primary-soft text-primary-soft-foreground'
                : 'hover:bg-muted'
            "
            @click="emit('select', item.slug)"
          >
            <span class="w-full truncate font-mono text-xs font-medium">{{
              item.slug
            }}</span>
            <span
              class="w-full truncate text-xs"
              :class="
                item.slug === selected
                  ? 'text-primary-soft-foreground'
                  : 'text-muted-foreground'
              "
            >
              {{ item.name }}
            </span>
          </button>
        </li>
      </ul>
    </LoadState>
    <NamespaceModal
      :vault="vault"
      v-model:open="adding"
      @created="emit('select', $event)"
    />
  </div>
</template>
