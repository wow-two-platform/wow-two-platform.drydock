<script lang="ts">
import type { PublishedSite, ServiceVersion } from "@/domain/deployments";

/** Defines the sites and service versions of a target's verified release. */
export interface TargetSitesProps {
  /** The sites the verified release answers on. */
  readonly sites?: readonly PublishedSite[] | undefined;
  /** Each service's version in the verified release. */
  readonly versions?: Readonly<Record<string, ServiceVersion>> | undefined;
}
</script>

<script setup lang="ts">
import { computed } from "vue";
import { AlertTriangle, ExternalLink, Lock } from "lucide-vue-next";

/** Renders Open site links and each service's version; an older version means no change since that release. */
defineOptions({ name: "TargetSites" });
const props = defineProps<TargetSitesProps>();
const services = computed(() =>
  Object.entries(props.versions ?? {}).sort(([left], [right]) =>
    left.localeCompare(right),
  ),
);
</script>

<template>
  <div
    v-if="props.sites?.length || services.length"
    class="flex flex-col gap-2"
  >
    <ul
      v-if="props.sites?.length"
      class="flex flex-wrap gap-2"
      aria-label="Sites"
    >
      <li v-for="site in props.sites" :key="site.name + '/' + site.service">
        <a
          :href="site.url"
          target="_blank"
          rel="noopener noreferrer"
          :title="
            site.probe && !site.probe.ok
              ? `${site.url} — did not answer after the deploy: ${site.probe.detail}`
              : site.url
          "
          :class="[
            'inline-flex h-9 items-center gap-1.5 rounded-lg border bg-card px-3 text-sm font-medium transition-colors hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring',
            site.probe && !site.probe.ok ? 'border-warning' : 'border-border',
          ]"
        >
          <AlertTriangle
            v-if="site.probe && !site.probe.ok"
            :size="14"
            class="text-warning"
            aria-hidden="true"
          /><Lock
            v-else-if="site.exposure === 'private'"
            :size="14"
            aria-hidden="true"
          />Open {{ site.name
          }}<ExternalLink :size="14" aria-hidden="true" /><span
            class="sr-only"
            >, {{ site.url }}, opens in a new tab{{
              site.probe && !site.probe.ok
                ? `; it did not answer after the deploy: ${site.probe.detail}`
                : ""
            }}</span
          >
        </a>
      </li>
    </ul>
    <ul
      v-if="services.length"
      class="flex flex-wrap gap-1.5 text-xs"
      aria-label="Service versions"
    >
      <li
        v-for="[name, entry] in services"
        :key="name"
        class="rounded-md bg-muted px-2 py-1 font-mono text-muted-foreground"
        :title="`Last changed in ${entry.changedIn}`"
      >
        <span class="text-foreground">{{ name }}</span> {{ entry.version }}
      </li>
    </ul>
  </div>
</template>
