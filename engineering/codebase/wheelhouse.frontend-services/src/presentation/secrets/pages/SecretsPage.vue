<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { KeyRound, RefreshCw } from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import {
  Badge,
  EmptyState,
  TabsGroup,
  TabsGroupList,
  TabsGroupTab,
  TabsGroupPanel,
} from "@wow-two-beta/ui-vue/presentation/display";
import {
  Alert,
  SkeletonState,
} from "@wow-two-beta/ui-vue/presentation/feedback";
import {
  SelectPicker,
  SelectPickerTrigger,
  SelectPickerValue,
  SelectPickerContent,
  SelectPickerItem,
} from "@wow-two-beta/ui-vue/presentation/forms";
import { SecretKeys, useVaultHygiene, useVaults } from "@/application/secrets";
import { useInvalidate } from "@/bootstrap/query";
import { useRefresh } from "@/application/common";
import { VaultStatus } from "@/domain/secrets";
import LoadState from "@/presentation/common/components/LoadState.vue";
import Panel from "@/presentation/common/components/Panel.vue";
import PageActions from "@/presentation/common/components/PageActions.vue";
import NamespaceList from "../components/NamespaceList.vue";
import SecretsTable from "../components/SecretsTable.vue";
import TokensTable from "../components/TokensTable.vue";

/** Presents vault availability, namespace selection, write-only secrets, and token administration. */
defineOptions({ name: "SecretsPage" });
defineSlots<{}>();

/** @internal Maps live vault availability to its visible status. */
const VaultTone = {
  unsealed: "success",
  sealed: "warning",
  unreachable: "danger",
} as const;
const { data: vaults, loading, error } = useVaults();
const invalidate = useInvalidate();
const { refresh, refreshing } = useRefresh(() => invalidate(SecretKeys.vaults));
const vault = ref("");
const namespace = ref("");
const selected = computed(() =>
  vaults.value?.find((item) => item.id === vault.value),
);
const hygiene = useVaultHygiene(() =>
  selected.value?.status === VaultStatus.Unsealed ? vault.value : "",
);
const hygieneData = hygiene.data;
const hygieneError = hygiene.error;
const hygieneLoading = hygiene.loading;
/** The keys overdue for rotation in the selected namespace. */
const overdueSecrets = computed(
  () =>
    new Set(
      (hygieneData.value?.overdueSecrets ?? [])
        .filter((secret) => secret.namespace === namespace.value)
        .map((secret) => secret.key),
    ),
);
/** The token warnings scoped to the selected namespace. */
const tokenFlags = computed(
  () =>
    new Map(
      (hygieneData.value?.overdueTokens ?? [])
        .filter((token) => token.namespace === namespace.value)
        .map((token) => [token.id, token.reason]),
    ),
);
const dueCount = computed(
  () =>
    (hygieneData.value?.overdueSecrets.length ?? 0) +
    (hygieneData.value?.overdueTokens.length ?? 0),
);

/** Chooses an available vault when the catalog loads or removes the current vault. */
watch(
  vaults,
  (items) => {
    if (items && !items.some((item) => item.id === vault.value))
      vault.value = items[0]?.id ?? "";
  },
  { immediate: true },
);
/** Resets the namespace immediately when its owning vault changes. */
watch(
  vault,
  () => {
    namespace.value = "";
  },
  { flush: "sync" },
);

/** Changes the authoritative vault selection. */
function selectVault(value: unknown): void {
  vault.value = typeof value === "string" ? value : "";
}
</script>

<template>
  <PageActions>
    <Button
      variant="outline"
      tone="neutral"
      size="sm"
      :is-loading="refreshing"
      @click="refresh"
    >
      <template #leading><RefreshCw :size="14" /></template>Refresh
    </Button>
  </PageActions>
  <LoadState
    :loading="loading || refreshing"
    :error="error"
    :has-data="vaults !== undefined"
    :empty="!vaults?.length"
    empty-title="No vaults configured"
    empty-description="Configured vaults appear here when they are available."
    @retry="refresh"
  >
    <template #skeleton>
      <div class="min-h-96 space-y-5">
        <SkeletonState class="h-8 w-48" />
        <SkeletonState class="h-6 w-80 max-w-full" />
        <div class="grid gap-5 lg:grid-cols-[15rem_minmax(0,1fr)]">
          <SkeletonState class="h-64 w-full" />
          <SkeletonState class="h-64 w-full" />
        </div>
      </div>
    </template>
    <Panel
      :title="selected?.name ?? 'Vault'"
      description="Select a namespace to manage its write-only secrets and product access tokens."
    >
      <template #actions>
        <Badge v-if="selected" :variant="VaultTone[selected.status]">{{
          selected.status
        }}</Badge>
        <SelectPicker
          v-if="(vaults?.length ?? 0) > 1"
          :model-value="vault || null"
          :get-option-label="
            (id) => vaults?.find((item) => item.id === id)?.name ?? String(id)
          "
          @update:model-value="selectVault"
        >
          <SelectPickerTrigger size="sm" aria-label="Vault">
            <SelectPickerValue placeholder="Select a vault" />
          </SelectPickerTrigger>
          <SelectPickerContent>
            <SelectPickerItem
              v-for="item in vaults ?? []"
              :key="item.id"
              :item-key="item.id"
              :label="item.name"
            />
          </SelectPickerContent>
        </SelectPicker>
      </template>
      <EmptyState
        v-if="selected && selected.status !== VaultStatus.Unsealed"
        size="sm"
        :title="`This vault is ${selected.status}`"
        description="Unseal the vault or restore its connection to manage its namespaces."
      >
        <template #icon><KeyRound :size="24" /></template>
      </EmptyState>
      <div v-else-if="vault" :key="vault" class="flex flex-col gap-5">
        <Alert
          v-if="hygieneError"
          severity="warning"
          title="Rotation status unavailable"
          :description="hygieneError.message"
        >
          <template #actions>
            <Button
              size="sm"
              variant="ghost"
              tone="neutral"
              @click="hygiene.refetch()"
              >Retry</Button
            >
          </template>
        </Alert>
        <div
          v-else-if="hygieneData"
          class="flex flex-wrap items-center gap-2 text-xs text-muted-foreground"
        >
          <Badge :variant="dueCount > 0 ? 'warning' : 'success'">
            {{
              dueCount > 0
                ? `${dueCount} rotation issue${dueCount === 1 ? "" : "s"}`
                : "Rotation up to date"
            }}
          </Badge>
          <span
            >{{ hygieneData.secrets }} secrets · {{ hygieneData.tokens }} tokens
            across this vault</span
          >
        </div>
        <p
          v-else-if="hygieneLoading"
          class="text-xs text-muted-foreground"
          role="status"
        >
          Checking rotation status…
        </p>
        <div
          class="grid min-h-96 overflow-hidden rounded-2xl border border-border lg:grid-cols-[15rem_minmax(0,1fr)]"
        >
          <aside
            class="border-b border-border bg-muted/40 p-4 lg:border-b-0 lg:border-r"
          >
            <NamespaceList
              :vault="vault"
              :selected="namespace"
              @select="namespace = $event"
            />
          </aside>
          <section class="min-w-0 bg-card p-4 lg:p-5">
            <template v-if="namespace">
              <div class="mb-5 flex flex-wrap items-center gap-2">
                <KeyRound :size="16" class="text-primary" />
                <h2 class="break-all font-mono text-sm font-semibold">
                  {{ namespace }}
                </h2>
              </div>
              <TabsGroup
                :key="`${vault}/${namespace}`"
                default-value="secrets"
                class="min-w-0"
              >
                <TabsGroupList aria-label="Namespace administration">
                  <TabsGroupTab value="secrets">Secrets</TabsGroupTab>
                  <TabsGroupTab value="tokens">Tokens</TabsGroupTab>
                </TabsGroupList>
                <TabsGroupPanel value="secrets" class="pt-5">
                  <SecretsTable
                    :vault="vault"
                    :ns="namespace"
                    :overdue="overdueSecrets"
                  />
                </TabsGroupPanel>
                <TabsGroupPanel value="tokens" class="pt-5">
                  <TokensTable
                    :vault="vault"
                    :ns="namespace"
                    :flags="tokenFlags"
                  />
                </TabsGroupPanel>
              </TabsGroup>
            </template>
            <EmptyState
              v-else
              size="sm"
              title="Select a namespace"
              description="Its secrets and product tokens open here."
            >
              <template #icon><KeyRound :size="24" /></template>
            </EmptyState>
          </section>
        </div>
      </div>
    </Panel>
  </LoadState>
</template>
