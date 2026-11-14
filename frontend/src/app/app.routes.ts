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
      import('./pages/definir-senha/definir-senha-page').then((m) => m.DefinirSenhaPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./pages/inicio/inicio-page').then((m) => m.InicioPage),
      },
      {
        path: 'perfil',
        loadComponent: () => import('./pages/perfil/perfil-page').then((m) => m.PerfilPage),
      },
      {
        path: 'trocar-senha',
        loadComponent: () =>
          import('./pages/trocar-senha/trocar-senha-page').then((m) => m.TrocarSenhaPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
