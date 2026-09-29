<script setup lang="ts">
import { ref } from "vue";
import { Plus } from "lucide-vue-next";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import { PageActions } from "@/presentation/common/components";
import ProductsPanel from "../components/ProductsPanel.vue";

/** Renders the registry route with its toolbar action. */
defineOptions({ name: "ProductsPage" });
const registering = ref(false);
</script>

<template>
  <PageActions>
    <Button :is-disabled="registering" @click="registering = true">
      <template #leading><Plus :size="16" /></template>Register product
    </Button>
  </PageActions>
  <ProductsPanel v-model:registering="registering" />
</template>
