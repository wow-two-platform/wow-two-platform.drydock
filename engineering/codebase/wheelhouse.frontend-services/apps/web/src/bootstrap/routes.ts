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
  { path: '/products', label: 'Products', description: 'The registry behind your portfolio.' },
  { path: '/activity', label: 'Activity', description: 'Every operator action, chained so an edited entry shows.' },
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
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
});
