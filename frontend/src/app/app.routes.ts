import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'definir-senha',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/set-password/set-password-page').then((m) => m.SetPasswordPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./pages/home/home-page').then((m) => m.HomePage),
      },
      {
        path: 'lancamentos',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/transactions/transactions-page').then((m) => m.TransactionsPage),
      },
      {
        path: 'cartoes',
        loadComponent: () => import('./pages/cards/cards-page').then((m) => m.CardsPage),
      },
      {
        path: 'metricas',
        loadComponent: () => import('./pages/metrics/metrics-page').then((m) => m.MetricsPage),
      },
      {
        path: 'perfil',
        loadComponent: () => import('./pages/profile/profile-page').then((m) => m.ProfilePage),
      },
      {
        path: 'trocar-senha',
        loadComponent: () =>
          import('./pages/change-password/change-password-page').then((m) => m.ChangePasswordPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
