<script lang="ts">
/** The registration action is owned by the route toolbar. */
export interface ProductsPanelProps {
  readonly registering: boolean;
}
</script>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import {
  ChevronRight,
  GitBranch,
  Package,
  Pencil,
  Search,
  Trash2,
} from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import { Badge } from "@wow-two-beta/ui-vue/presentation/display";
import {
  Alert,
  SkeletonState,
} from "@wow-two-beta/ui-vue/presentation/feedback";
import { SearchInput } from "@wow-two-beta/ui-vue/presentation/forms";
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalDescription,
} from "@wow-two-beta/ui-vue/presentation/overlays";
import { useProducts } from "@/application/products";
import { ProductStatus, type Product } from "@/domain/products";
import RegisterProductForm from "./RegisterProductForm.vue";

/** Presents portfolio selection with a contextual inspector and confirmed registry changes. */
defineOptions({ name: "ProductsPanel" });
const props = defineProps<ProductsPanelProps>();
const emit = defineEmits<{ "update:registering": [value: boolean] }>();

const statusVariant = {
  [ProductStatus.Active]: "success",
  [ProductStatus.Paused]: "warning",
  [ProductStatus.Killed]: "danger",
  [ProductStatus.Draft]: "neutral",
} as const;
const { products, loading, error, reload, create, update, remove } =
  useProducts();
const search = ref("");
const selectedId = ref<string | null>(null);
const editing = ref<Product | null>(null);
const confirmingDelete = ref(false);
const deleting = ref(false);
const deleteError = ref<string | null>(null);
const formPending = ref(false);
const visibleProducts = computed(() => {
  const term = search.value.trim().toLocaleLowerCase();
  return products.value.filter((product) =>
    `${product.name} ${product.slug} ${product.repo}`
      .toLocaleLowerCase()
      .includes(term),
  );
});
const selected = computed(
  () =>
    visibleProducts.value.find((product) => product.id === selectedId.value) ??
    null,
);
const formOpen = computed(() => props.registering || editing.value !== null);

/** Keeps the inspector attached to a visible product after searches or registry changes. */
watch(
  visibleProducts,
  (items) => {
    if (!items.some((item) => item.id === selectedId.value))
      selectedId.value = items[0]?.id ?? null;
  },
  { immediate: true },
);
/** Clears a stale deletion confirmation when the inspected product changes. */
watch(selectedId, () => {
  confirmingDelete.value = false;
  deleteError.value = null;
});
/** Switches registration into its own editor instance. */
watch(
  () => props.registering,
  (open) => {
    if (open) editing.value = null;
  },
);

/** Closes a settled form without dismissing a pending server write. */
function closeForm(): void {
  if (formPending.value) return;
  editing.value = null;
  emit("update:registering", false);
}

/** Starts editing the selected registry item. */
function editProduct(product: Product): void {
  emit("update:registering", false);
  editing.value = product;
}

/** Removes only the explicitly confirmed product after the server accepts the deletion. */
async function deleteProduct(): Promise<void> {
  if (!selected.value || deleting.value) return;
  const id = selected.value.id;
  deleting.value = true;
  deleteError.value = null;
  try {
    const result = await remove(id);
    if (result.ok) confirmingDelete.value = false;
    else deleteError.value = result.failure.message;
  } finally {
    deleting.value = false;
  }
}

/** Finishes the form after its submission lifecycle has settled. */
function saved(): void {
  formPending.value = false;
  closeForm();
}
</script>

<template>
  <div
    class="wh-glass grid min-h-[26rem] overflow-hidden rounded-2xl border lg:grid-cols-[20rem_minmax(0,1fr)]"
  >
    <section
      class="wh-glass-subtle min-w-0 border-b border-border p-4 sm:p-5 lg:border-b-0 lg:border-r"
      aria-label="Product registry"
    >
      <div class="mb-4 flex items-center justify-between gap-3">
        <h2 class="text-sm font-semibold">Your products</h2>
        <p
          v-if="!loading || products.length > 0"
          class="text-sm text-muted-foreground"
        >
          {{ products.length }}
          {{ products.length === 1 ? "product" : "products" }}
        </p>
        <SkeletonState v-else class="h-4 w-36" aria-hidden="true" />
      </div>
      <SearchInput
        v-model="search"
        aria-label="Search products"
        placeholder="Find a product…"
        class="mb-4 w-full"
      />
      <div v-if="error && products.length > 0" class="mb-4 space-y-3">
        <Alert
          severity="warning"
          title="Showing the last registry snapshot"
          :description="error"
        />
        <Button size="sm" variant="outline" @click="reload"
          >Retry refresh</Button
        >
      </div>
      <div
        v-if="loading && products.length === 0"
        class="space-y-2"
        role="status"
        aria-label="Loading products"
      >
        <div
          v-for="index in 3"
          :key="index"
          class="flex items-center gap-3 rounded-xl border border-border bg-card p-3"
          aria-hidden="true"
        >
          <SkeletonState class="size-10 shrink-0 rounded-lg" />
          <div class="flex-1 space-y-2">
            <SkeletonState class="h-4 w-3/4" /><SkeletonState
              class="h-3 w-1/2"
            />
          </div>
        </div>
      </div>
      <div v-else-if="error && products.length === 0" class="space-y-3">
        <Alert
          severity="danger"
          title="Registry unavailable"
          :description="error"
        />
        <Button variant="outline" @click="reload">Retry</Button>
      </div>
      <div
        v-else-if="products.length === 0"
        class="flex min-h-52 flex-col items-center justify-center gap-3 py-6 text-center"
      >
        <div class="rounded-2xl bg-primary-soft p-4 text-primary">
          <Package :size="28" />
        </div>
        <h2 class="text-xl font-semibold">Your portfolio starts here</h2>
        <p class="max-w-sm text-sm text-muted-foreground">
          Register a product and connect its source repository.
        </p>
        <Button @click="emit('update:registering', true)"
          >Register product</Button
        >
      </div>
      <div v-else-if="visibleProducts.length === 0" class="py-8 text-center">
        <Search :size="24" class="mx-auto mb-3 text-muted-foreground" />
        <p class="font-medium">No matching products</p>
        <p class="mt-1 text-sm text-muted-foreground">
          Try a different name or repository.
        </p>
      </div>
      <div v-else class="max-h-[28rem] space-y-2 overflow-y-auto p-1 -m-1">
        <button
          v-for="product in visibleProducts"
          :key="product.id"
          type="button"
          :aria-pressed="selectedId === product.id"
          @click="selectedId = product.id"
          class="group flex w-full min-w-0 items-center gap-3 rounded-xl border p-3 text-left transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          :class="
            selectedId === product.id
              ? 'border-primary/40 bg-primary-soft'
              : 'border-transparent hover:border-border hover:bg-card'
          "
        >
          <span
            class="flex size-10 shrink-0 items-center justify-center rounded-lg border border-border bg-card text-sm font-semibold text-primary"
          >
            {{ product.name.slice(0, 1).toUpperCase() }}
          </span>
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-semibold">{{ product.name }}</p>
            <p class="mt-1 truncate text-xs text-muted-foreground">
              {{ product.slug }}
            </p>
          </div>
          <ChevronRight :size="16" class="shrink-0 text-muted-foreground" />
        </button>
      </div>
    </section>

    <section aria-label="Product details" class="min-w-0 bg-card p-5 sm:p-7">
      <template v-if="selected">
        <div
          class="flex flex-wrap items-start justify-between gap-4 border-b border-border pb-6"
        >
          <div class="min-w-0">
            <div class="mb-3 flex items-center gap-2">
              <Package :size="16" class="text-muted-foreground" />
              <span class="text-xs text-muted-foreground">Product details</span>
            </div>
            <h2 class="break-words text-xl font-semibold">
              {{ selected.name }}
            </h2>
            <p class="mt-1 break-all font-mono text-xs text-muted-foreground">
              {{ selected.slug }}
            </p>
          </div>
          <Button variant="outline" size="sm" @click="editProduct(selected)">
            <template #leading><Pencil :size="15" /></template>Edit product
          </Button>
        </div>
        <dl class="my-6 grid gap-x-8 gap-y-6 text-sm sm:grid-cols-2">
          <div class="min-w-0 sm:col-span-2">
            <dt class="mb-2 text-xs text-muted-foreground">
              Source repository
            </dt>
            <dd class="flex items-start gap-2 break-all font-medium">
              <GitBranch
                :size="16"
                class="mt-0.5 shrink-0 text-muted-foreground"
              />{{ selected.repo }}
            </dd>
          </div>
          <div>
            <dt class="mb-1 text-xs text-muted-foreground">Lifecycle</dt>
            <dd>
              <Badge :variant="statusVariant[selected.status]">{{
                selected.status
              }}</Badge>
            </dd>
          </div>
          <div>
            <dt class="mb-1 text-xs text-muted-foreground">Registered</dt>
            <dd>{{ new Date(selected.createdAtUtc).toLocaleString() }}</dd>
          </div>
        </dl>
        <details
          class="mb-6 rounded-lg border border-border px-3 py-2.5 text-xs"
        >
          <summary class="cursor-pointer text-muted-foreground">
            Registry identifiers
          </summary>
          <dl class="mt-3 space-y-1">
            <dt class="text-muted-foreground">Product ID</dt>
            <dd class="break-all font-mono">{{ selected.id }}</dd>
          </dl>
        </details>
        <div
          v-if="confirmingDelete"
          class="space-y-3 rounded-xl bg-destructive-soft p-4"
        >
          <p class="text-sm text-destructive-soft-foreground">
            Delete {{ selected.name }}? This cannot be undone.
          </p>
          <p
            v-if="deleteError"
            role="alert"
            class="text-sm text-destructive-soft-foreground"
          >
            {{ deleteError }}
          </p>
          <div class="flex gap-2">
            <Button
              size="sm"
              variant="ghost"
              tone="neutral"
              :is-disabled="deleting"
              @click="confirmingDelete = false"
              >Cancel</Button
            >
            <Button
              size="sm"
              tone="danger"
              :is-loading="deleting"
              @click="deleteProduct"
              >Delete product</Button
            >
          </div>
        </div>
        <div v-else class="border-t border-border pt-4">
          <Button
            variant="ghost"
            tone="danger"
            size="sm"
            aria-label="Delete product"
            @click="confirmingDelete = true"
          >
            <template #leading><Trash2 :size="14" /></template>Delete product
          </Button>
        </div>
      </template>
      <div
        v-else
        class="flex min-h-60 flex-col items-center justify-center gap-3 text-center text-muted-foreground"
      >
        <Package :size="24" />
        <p class="text-sm">
          {{
            search
              ? "No products match your search."
              : "Select a product to inspect its details."
          }}
        </p>
      </div>
    </section>
  </div>

  <Modal
    :open="formOpen"
    :dismiss-on-outside-click="!formPending"
    :dismiss-on-escape="!formPending"
    @update:open="
      (open) => {
        if (!open) closeForm();
      }
    "
  >
    <ModalContent class="w-[min(40rem,calc(100vw-2rem))]">
      <ModalHeader>
        <ModalTitle>{{
          editing ? "Edit product" : "Register product"
        }}</ModalTitle>
        <ModalDescription
          >Connect a portfolio product to its GitHub source
          repository.</ModalDescription
        >
      </ModalHeader>
      <RegisterProductForm
        v-if="formOpen"
        :key="editing?.id ?? 'new'"
        v-bind="editing ? { initialProduct: editing } : {}"
        :create="create"
        :update="update"
        @saved="saved"
        @cancel="closeForm"
        @pending="formPending = $event"
      />
    </ModalContent>
  </Modal>
</template>
