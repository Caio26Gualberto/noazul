import { AppRoutes } from '../auth/routes';
import { AppIconName } from '../components/app-icons';

export interface NavItem {
  path: string;
  labelKey: string;
  icon: AppIconName;
  exact: boolean;
}

export const navItems: NavItem[] = [
  { path: AppRoutes.home, labelKey: 'common.nav.home', icon: 'home', exact: true },
  { path: AppRoutes.profile, labelKey: 'common.nav.profile', icon: 'user', exact: false },
];
