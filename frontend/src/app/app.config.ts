import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localePt from '@angular/common/locales/pt';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  signal,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { provideTaiga } from '@taiga-ui/core';
import { TUI_LANGUAGE } from '@taiga-ui/i18n';
import { TUI_PORTUGUESE_LANGUAGE } from '@taiga-ui/i18n/languages/portuguese';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './api/auth.interceptor';
import { routes } from './app.routes';
import { AuthStore } from './auth/auth.store';
import { DEFAULT_LANG, provideAppTranslations } from './i18n';

registerLocaleData(localePt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppTranslations(),
    { provide: LOCALE_ID, useValue: DEFAULT_LANG },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'BRL' },
    provideTaiga({ mode: 'light' }),
    { provide: TUI_LANGUAGE, useValue: signal(TUI_PORTUGUESE_LANGUAGE) },
    provideAppInitializer(async () => {
      const transloco = inject(TranslocoService);
      const auth = inject(AuthStore);
      await firstValueFrom(transloco.load(DEFAULT_LANG));
      await auth.restore();
    }),
  ],
};
