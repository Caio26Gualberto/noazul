import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-inicio-page',
  imports: [TranslocoDirective],
  template: `
    <section *transloco="let t" class="app-page">
      <header class="app-page__header">
        <h1 class="app-title">{{ t('common.home.title', { name: auth.profile()?.name }) }}</h1>
        <p class="app-subtitle">{{ t('common.home.subtitle') }}</p>
      </header>

      <div class="app-card app-page__header">
        <h2>{{ t('common.home.emptyTitle') }}</h2>
        <p class="app-subtitle">{{ t('common.home.emptyText') }}</p>
      </div>
    </section>
  `,
  styles: `
    h2 {
      font: var(--mat-sys-title-large);
      font-weight: 600;
    }
  `,
})
export class InicioPage {
  protected readonly auth = inject(AuthStore);
}
