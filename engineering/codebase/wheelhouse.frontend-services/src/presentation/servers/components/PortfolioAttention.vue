<script setup lang="ts">
import { computed } from "vue";
import { RouterLink, useRoute } from "vue-router";
import { CheckCircle2 } from "lucide-vue-next";
import { SkeletonState } from "@wow-two-beta/ui-vue/presentation/feedback";
import { useDeploymentStats } from "@/application/deployments";
import { useVaults, useVaultsHygiene } from "@/application/secrets";
import { useServerVitals } from "@/application/servers";
import { AttentionRules } from "@/domain/overview";

/** Lists what needs the operator across every product: unreachable hosts, failing containers, rotation debt. */
defineOptions({ name: "PortfolioAttention" });
defineSlots<{}>();

const route = useRoute();
const vitals = useServerVitals();
const stats = useDeploymentStats();
const vaults = useVaults();
const hygiene = useVaultsHygiene(() => vaults.data.value ?? []);
const attention = computed(() =>
  AttentionRules.sort([
    ...(vitals.data.value ? AttentionRules.fromVitals(vitals.data.value) : []),
    ...(stats.data.value ? AttentionRules.fromStats(stats.data.value) : []),
    ...(vaults.data.value ?? []).flatMap((vault) =>
      AttentionRules.fromVault(vault, hygiene.byVault.value.get(vault.id)),
    ),
  ]),
);
const incomplete = computed(() =>
  Boolean(
    vitals.error.value ||
    stats.error.value ||
    vaults.error.value ||
    hygiene.errors.value.length,
  ),
);
const loading = computed(
  () =>
    vitals.loading.value ||
    stats.loading.value ||
    vaults.loading.value ||
    hygiene.loading.value,
);
</script>
<template>
  <section
    class="rounded-2xl border border-border bg-card p-5"
    aria-label="Portfolio attention"
  >
    <div class="mb-3 flex items-center justify-between">
      <h2 class="text-sm font-semibold">Needs attention</h2>
      <span class="text-xs text-muted-foreground">All products</span>
    </div>
    <div
      v-if="loading && !attention.length"
      role="status"
      aria-label="Reading portfolio attention"
    >
      <SkeletonState class="h-14 w-full" />
    </div>
    <div
      v-for="item in attention"
      :key="item.id"
      class="flex items-start justify-between gap-3 border-t border-border py-3"
    >
      <div>
        <p
          class="text-sm font-medium"
          :class="item.tone === 'danger' ? 'text-destructive' : 'text-warning'"
        >
          {{ item.title }}
        </p>
        <p class="mt-1 text-xs text-muted-foreground">{{ item.detail }}</p>
      </div>
      <RouterLink
        :to="{ path: item.href, query: route.query }"
        class="shrink-0 text-xs font-medium text-primary"
        >Open</RouterLink
      >
    </div>
    <p
      v-if="!attention.length && !loading && !incomplete"
      class="flex items-center gap-2 text-sm text-muted-foreground"
    >
      <CheckCircle2 :size="16" class="text-success" />No issues in the available
      readings.
    </p>
    <p v-if="incomplete" role="status" class="text-xs text-warning">
      Some readings are unavailable; this list may be incomplete.
    </p>
  </section>
</template>
