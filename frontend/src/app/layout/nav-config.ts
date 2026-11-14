import { AppRoutes } from '../auth/routes';

export interface NavItem {
  path: string;
  labelKey: string;
  icon: string;
  exact: boolean;
}

export const navItems: NavItem[] = [
  { path: AppRoutes.home, labelKey: 'common.nav.home', icon: '@tui.house', exact: true },
  { path: AppRoutes.profile, labelKey: 'common.nav.profile', icon: '@tui.user', exact: false },
];
