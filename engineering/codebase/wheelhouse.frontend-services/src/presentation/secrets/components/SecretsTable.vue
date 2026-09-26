<script lang="ts">
/** Secret metadata and rotation signals for the selected namespace. */
export interface SecretsTableProps {
  vault: string;
  ns: string;
  overdue: ReadonlySet<string>;
}
/** @internal Identifies an add or rotation editor without storing a secret value. */
interface SecretEditor {
  key?: string;
}
</script>

<script setup lang="ts">
import { shallowRef, watch } from "vue";
import { KeyRound, Plus } from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import {
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@wow-two-beta/ui-vue/presentation/display";
import {
  Alert,
  StatusIndicator,
} from "@wow-two-beta/ui-vue/presentation/feedback";
import { useSecretChanges, useVaultSecrets } from "@/application/secrets";
import { Measures } from "@/domain/common";
import { SecretState } from "@/domain/secrets";
import LoadState from "@/presentation/common/components/LoadState.vue";
import SetSecretModal from "./SetSecretModal.vue";

/** Shows metadata and controls serving state without exposing stored values. */
defineOptions({ name: "SecretsTable" });
const props = defineProps<SecretsTableProps>();
defineSlots<{}>();

const { data, loading, error, refetch } = useVaultSecrets(
  () => props.vault,
  () => props.ns,
);
const changes = useSecretChanges(
  () => props.vault,
  () => props.ns,
);
const changeError = changes.error;
const changing = changes.loading;
const editing = shallowRef<SecretEditor | null>(null);

/** Discards an editor when its vault or namespace changes. */
watch([() => props.vault, () => props.ns], () => {
  editing.value = null;
});

/** Dismisses the local add or rotation editor. */
function closeEditor(open: boolean): void {
  if (!open) editing.value = null;
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-muted-foreground">
        Only metadata is readable. Rotate a key to write its next version.
      </p>
      <Button size="sm" @click="editing = {}">
        <template #leading><Plus :size="14" /></template>Add secret
      </Button>
    </div>
    <Alert
      v-if="changeError"
      severity="danger"
      :description="changeError.message"
    />
    <LoadState
      :loading="loading"
      :error="error"
      :has-data="data !== undefined"
      :empty="!data?.length"
      empty-title="No secrets yet"
      empty-description="Add the values this product environment needs."
      @retry="refetch"
    >
      <Table
        density="compact"
        is-hoverable
        container-class-name="overflow-x-auto rounded-xl border border-border"
      >
        <TableHead
          ><TableRow>
            <TableHeaderCell>Key</TableHeaderCell
            ><TableHeaderCell>State</TableHeaderCell>
            <TableHeaderCell>Version</TableHeaderCell
            ><TableHeaderCell>Updated</TableHeaderCell>
            <TableHeaderCell
              ><span class="sr-only">Actions</span></TableHeaderCell
            >
          </TableRow></TableHead
        >
        <TableBody>
          <TableRow v-for="secret in data ?? []" :key="secret.key">
            <TableCell>
              <span class="block font-mono text-xs">{{ secret.key }}</span>
              <span
                v-if="secret.description"
                class="block text-xs text-muted-foreground"
                >{{ secret.description }}</span
              >
            </TableCell>
            <TableCell>
              <StatusIndicator
                :tone="
                  secret.state === SecretState.Disabled ? 'warning' : 'success'
                "
                :label="secret.state"
              />
            </TableCell>
            <TableCell>v{{ secret.version }}</TableCell>
            <TableCell class="whitespace-nowrap text-xs text-muted-foreground">
              <span class="flex flex-col items-start gap-1">
                <span :title="new Date(secret.updatedAtUtc).toLocaleString()">{{
                  Measures.age(secret.updatedAtUtc)
                }}</span>
                <Badge v-if="overdue.has(secret.key)" variant="warning"
                  >rotation due</Badge
                >
              </span>
            </TableCell>
            <TableCell>
              <div class="flex justify-end gap-1">
                <Button
                  variant="ghost"
                  tone="neutral"
                  size="sm"
                  :aria-label="`Rotate ${secret.key}`"
                  title="Write a new version"
                  @click="editing = { key: secret.key }"
                >
                  <KeyRound :size="14" />
                </Button>
                <Button
                  variant="ghost"
                  :tone="
                    secret.state === SecretState.Disabled ? 'neutral' : 'danger'
                  "
                  size="sm"
                  :is-disabled="changing"
                  @click="
                    changes.setDisabled(
                      secret.key,
                      secret.state !== SecretState.Disabled,
                    )
                  "
                >
                  {{
                    secret.state === SecretState.Disabled ? "Enable" : "Disable"
                  }}
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </LoadState>
    <SetSecretModal
      :vault="vault"
      :ns="ns"
      :secret-key="editing?.key"
      :open="editing !== null"
      @update:open="closeEditor"
    />
  </div>
</template>
