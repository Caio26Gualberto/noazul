import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-brand',
  imports: [TranslocoPipe],
  template: `
    <svg class="mark" viewBox="0 0 32 32" aria-hidden="true">
      <rect class="mark__ground" width="32" height="32" rx="9" />
      <path class="mark__line" d="M8 20l5-6 4 4 7-9" />
    </svg>
    <span class="name">{{ 'common.appName' | transloco }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 10px;
    }

    .mark {
      width: 32px;
      height: 32px;
    }

    .mark__ground {
      fill: var(--mat-sys-primary);
    }

    .mark__line {
      fill: none;
      stroke: var(--mat-sys-on-primary);
      stroke-width: 2.6;
      stroke-linecap: round;
      stroke-linejoin: round;
    }

    .name {
      font: var(--mat-sys-title-large);
      font-weight: 700;
    }
  `,
})
export class AppBrand {}
