import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiButton, TuiDataList, TuiDropdown, TuiIcon } from '@taiga-ui/core';
import { TuiAvatar } from '@taiga-ui/kit';
import { AuthStore } from '../auth/auth.store';
import { AppRoutes } from '../auth/routes';
import { AppBrand } from '../components/app-brand';
import { AppScopeSelect } from '../components/app-scope-select';
import { navItems } from './nav-config';

@Component({
  selector: 'app-shell',
  imports: [
    AppBrand,
    AppScopeSelect,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    TranslocoDirective,
    TuiAvatar,
    TuiButton,
    TuiDataList,
    TuiDropdown,
    TuiIcon,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {
  protected readonly auth = inject(AuthStore);
  protected readonly navItems = navItems;
  protected readonly routes = AppRoutes;
  protected readonly menuOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  protected initial(name: string | undefined): string {
    return name?.trim().charAt(0).toUpperCase() ?? '';
  }

  protected closeMenus(): void {
    this.userMenuOpen.set(false);
    this.menuOpen.set(false);
  }

  protected logout(): void {
    this.closeMenus();
    void this.auth.logout();
  }
}
