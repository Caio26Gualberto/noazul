import { EnvironmentProviders, Injectable, Provider, isDevMode } from '@angular/core';
import { Translation, TranslocoLoader, provideTransloco } from '@jsverse/transloco';
import { Observable, of } from 'rxjs';
import api from './pt-BR/api.json';
import auth from './pt-BR/auth.json';
import cards from './pt-BR/cards.json';
import common from './pt-BR/common.json';
import finance from './pt-BR/finance.json';
import home from './pt-BR/home.json';
import metrics from './pt-BR/metrics.json';
import newPix from './pt-BR/newPix.json';
import profile from './pt-BR/profile.json';
import transactions from './pt-BR/transactions.json';

export const DEFAULT_LANG = 'pt-BR';

const translations: Record<string, Translation> = {
  [DEFAULT_LANG]: {
    api,
    auth,
    cards,
    common,
    finance,
    home,
    metrics,
    newPix,
    profile,
    transactions,
  },
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
