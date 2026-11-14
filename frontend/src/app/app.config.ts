import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localePt from '@angular/common/locales/pt';
import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { provideRouter } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './api/auth.interceptor';
import { routes } from './app.routes';
import { AuthStore } from './auth/auth.store';
import { registerAppIcons } from './components/app-icons';
import { DEFAULT_LANG, provideAppTranslations } from './i18n';

registerLocaleData(localePt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppTranslations(),
    { provide: LOCALE_ID, useValue: DEFAULT_LANG },
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
    provideAppInitializer(async () => {
      registerAppIcons();
      const transloco = inject(TranslocoService);
      const auth = inject(AuthStore);
      await firstValueFrom(transloco.load(DEFAULT_LANG));
      await auth.restore();
    }),
  ],
};
