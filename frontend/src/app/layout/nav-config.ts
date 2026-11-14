import { AppRoutes } from '../auth/routes';

export interface NavItem {
  path: string;
  labelKey: string;
  icon: string;
  exact: boolean;
}

export const navItems: NavItem[] = [
  { path: AppRoutes.home, labelKey: 'common.nav.home', icon: '@tui.house', exact: true },
  {
    path: AppRoutes.transactions,
    labelKey: 'common.nav.transactions',
    icon: '@tui.list',
    exact: true,
  },
  { path: AppRoutes.cards, labelKey: 'common.nav.cards', icon: '@tui.credit-card', exact: false },
  {
    path: AppRoutes.metrics,
    labelKey: 'common.nav.metrics',
    icon: '@tui.chart-column',
    exact: false,
  },
  { path: AppRoutes.profile, labelKey: 'common.nav.profile', icon: '@tui.user', exact: false },
];
