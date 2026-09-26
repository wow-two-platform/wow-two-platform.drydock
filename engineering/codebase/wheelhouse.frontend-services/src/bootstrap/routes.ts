import { createRouter, createWebHistory } from 'vue-router';

/** Keeps primary destinations stable while route content loads on demand. */
export const AppPlaces = [
  { path: '/', label: 'Workspace', description: 'Your products, environments, and the details that matter.' },
  {
    path: '/deployments',
    label: 'Deployments',
    description: 'Published releases, readiness checks, and rollout outcomes.',
  },
  { path: '/fleet', label: 'Fleet', description: 'Your hosts and services, with current resource readings.' },
  { path: '/secrets', label: 'Secrets', description: 'Vault namespaces, write-only secrets, and product tokens.' },
  { path: '/products', label: 'Products', description: 'The registry behind your portfolio.' },
] as const;

/** Resolves the existing public URLs to Vue route components. */
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: () => import('@/presentation/workspace/WorkspacePage.vue') },
    { path: '/deployments', component: () => import('@/presentation/deployments/pages/DeploymentsPage.vue') },
    { path: '/fleet', component: () => import('@/presentation/fleet/pages/FleetPage.vue') },
    { path: '/secrets', component: () => import('@/presentation/secrets/pages/SecretsPage.vue') },
    { path: '/products', component: () => import('@/presentation/products/pages/ProductsPage.vue') },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
});
