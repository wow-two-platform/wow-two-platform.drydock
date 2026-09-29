<script setup lang="ts">
/** Places the active route's primary actions beside its page identity. */
defineOptions({ name: 'PageActions' });
defineSlots<{ default(): unknown }>();
</script>
<template>
  <Teleport to="#page-actions" defer><slot /></Teleport>
</template>
