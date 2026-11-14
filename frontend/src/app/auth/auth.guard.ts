import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';
import { AppRoutes } from './routes';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree([AppRoutes.login], {
      queryParams: state.url === AppRoutes.home ? {} : { voltar: state.url },
    });
  }

  const isSettingPassword = state.url.startsWith(AppRoutes.setPassword);
  if (auth.needsPasswordChange() && !isSettingPassword) {
    return router.parseUrl(AppRoutes.setPassword);
  }
  if (!auth.needsPasswordChange() && isSettingPassword) return router.parseUrl(AppRoutes.home);

  return true;
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  return auth.isAuthenticated() ? inject(Router).parseUrl(AppRoutes.home) : true;
};
