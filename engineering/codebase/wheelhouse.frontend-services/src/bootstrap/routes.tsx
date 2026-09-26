import type { ComponentType } from 'react';
import { Boxes, KeyRound, LayoutDashboard, Package, Rocket } from 'lucide-react';
import { OverviewPage } from '@/presentation/common';
import { DeploymentsPage } from '@/presentation/deployments';
import { FleetPage } from '@/presentation/fleet';
import { ProductsPage } from '@/presentation/products';
import { SecretsPage } from '@/presentation/secrets';

/** A top-level dashboard place. */
export interface AppRoute {
  path: string;
  label: string;
  /** One line under the page title. */
  description: string;
  icon: ComponentType<{ size?: number; className?: string }>;
  page: ComponentType;
}

/** The dashboard's places, in navigation order. */
export const APP_ROUTES: readonly AppRoute[] = [
  { path: '/', label: 'Overview', description: 'What needs attention, host capacity and deployment health.',
    icon: LayoutDashboard, page: OverviewPage },
  { path: '/deployments', label: 'Deployments', description: 'Deploy releases, follow outcomes and recover locked targets.',
    icon: Rocket, page: DeploymentsPage },
  { path: '/fleet', label: 'Fleet', description: 'Hosts and environments defined in code, with live vitals.',
    icon: Boxes, page: FleetPage },
  { path: '/secrets', label: 'Secrets', description: 'Vault namespaces, write-only secrets and product tokens.',
    icon: KeyRound, page: SecretsPage },
  { path: '/products', label: 'Products', description: 'Portfolio products and their source repositories.',
    icon: Package, page: ProductsPage },
];
