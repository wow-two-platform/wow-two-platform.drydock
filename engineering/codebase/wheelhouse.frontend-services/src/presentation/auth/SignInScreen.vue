<script lang="ts">
/** Optional failure from resolving or clearing the private session. */
export interface SignInScreenProps {
  readonly error?: { readonly message: string } | null;
}
</script>

<script setup lang="ts">
import { Github, Ship } from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import { Card } from "@wow-two-beta/ui-vue/presentation/display";
import { Alert } from "@wow-two-beta/ui-vue/presentation/feedback";

/** Renders the private operator sign-in gate. */
defineOptions({ name: "SignInScreen" });
const props = defineProps<SignInScreenProps>();
const emit = defineEmits<{ "sign-in": [] }>();
</script>

<template>
  <main
    class="flex min-h-dvh items-center justify-center bg-background px-5 py-16 text-foreground"
  >
    <Card class="w-full max-w-md rounded-[2rem] border border-border p-9">
      <div
        class="mb-8 flex size-14 items-center justify-center rounded-2xl bg-primary text-primary-foreground"
      >
        <Ship :size="27" />
      </div>
      <p
        class="mb-2 text-xs font-medium uppercase tracking-[0.2em] text-muted-foreground"
      >
        Your operations workspace
      </p>
      <h1 class="text-3xl font-semibold tracking-tight">Wheelhouse</h1>
      <p class="mb-8 mt-4 text-sm leading-6 text-muted-foreground">
        Products, deployments, and infrastructure in one place. Sign in with
        your authorized GitHub account.
      </p>
      <Alert
        v-if="props.error"
        class="mb-5"
        severity="danger"
        title="Session unavailable"
        :description="props.error.message"
      />
      <Button size="lg" is-full-width @click="emit('sign-in')">
        <template #leading><Github :size="18" /></template>Sign in with GitHub
      </Button>
      <p class="mt-5 text-center text-xs text-muted-foreground">
        Private control plane · Authorized operators only
      </p>
    </Card>
  </main>
</template>
