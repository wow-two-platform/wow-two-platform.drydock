import { createRouter, createWebHistory } from 'vue-router';

/** Keeps primary destinations stable while route content loads on demand. */
export const AppPlaces = [
  { path: '/', label: 'Workspace', description: 'Your products, environments, and the details that matter.' },
  {
    path: '/deployments',
    label: 'Deployments',
    description: 'Published releases, readiness checks, and rollout outcomes.',
  },
  { path: '/servers', label: 'Servers', description: 'Your servers and their services, with current resource readings.' },
  { path: '/secrets', label: 'Secrets', description: 'Vault namespaces, write-only secrets, and product tokens.' },
  { path: '/products', label: 'Products', description: 'Your portfolio, as the product catalog defines it.' },
  { path: '/activity', label: 'Activity', description: 'Every operator action, chained so an edited entry shows.' },
] as const;

/** Keeps account-level destinations out of the primary navigation; the profile menu opens them. */
export const SettingsPlaces = [
  { path: '/settings/keys', label: 'Integration keys', description: 'Scoped keys other programs present to read Wheelhouse.' },
] as const;

/** Resolves the existing public URLs to Vue route components. */
export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: () => import('@/presentation/workspace/WorkspacePage.vue') },
    { path: '/deployments', component: () => import('@/presentation/deployments/pages/DeploymentsPage.vue') },
    { path: '/servers', component: () => import('@/presentation/servers/pages/ServersPage.vue') },
    { path: '/fleet', redirect: (to) => ({ path: '/servers', query: to.query }) },
    { path: '/secrets', component: () => import('@/presentation/secrets/pages/SecretsPage.vue') },
    { path: '/products', component: () => import('@/presentation/products/pages/ProductsPage.vue') },
    { path: '/activity', component: () => import('@/presentation/audit/pages/ActivityPage.vue') },
    { path: '/settings/keys', component: () => import('@/presentation/integrations/pages/IntegrationKeysPage.vue') },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
});
