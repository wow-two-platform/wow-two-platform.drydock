<script setup lang="ts">
import { RouterView } from 'vue-router';
import { Spinner } from '@wow-two-beta/ui-vue/presentation/feedback';
import { useAuth } from '@/application/auth';
import SignInScreen from '@/presentation/auth/SignInScreen.vue';
import AppLayout from './AppLayout.vue';

/** Gates every private workspace route on the operator's GitHub session. */
defineOptions({ name: 'App' });
const auth = useAuth();
</script>
<template>
  <div v-if="auth.loading.value" class="flex min-h-svh items-center justify-center bg-background">
    <Spinner label="Loading Wheelhouse" />
  </div>
  <AppLayout v-else-if="auth.user.value" :user="auth.user.value" @sign-out="auth.signOut">
    <RouterView />
  </AppLayout>
  <SignInScreen v-else :error="auth.error.value" @sign-in="auth.signIn" />
</template>
