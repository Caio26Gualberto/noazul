import { EnvironmentProviders, Injectable, Provider, isDevMode } from '@angular/core';
import { Translation, TranslocoLoader, provideTransloco } from '@jsverse/transloco';
import { Observable, of } from 'rxjs';
import api from './pt-BR/api.json';
import auth from './pt-BR/auth.json';
import common from './pt-BR/common.json';
import perfil from './pt-BR/perfil.json';

export const DEFAULT_LANG = 'pt-BR';

const translations: Record<string, Translation> = {
  [DEFAULT_LANG]: { api, auth, common, perfil },
};

@Injectable()
class BundledTranslationLoader implements TranslocoLoader {
  getTranslation(lang: string): Observable<Translation> {
    return of(translations[lang] ?? {});
  }
}

export function provideAppTranslations(): (Provider | EnvironmentProviders)[] {
  return provideTransloco({
    config: {
      availableLangs: [DEFAULT_LANG],
      defaultLang: DEFAULT_LANG,
      reRenderOnLangChange: false,
      prodMode: !isDevMode(),
    },
    loader: BundledTranslationLoader,
  });
}
