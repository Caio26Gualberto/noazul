import { Component, input, signal } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-password-toggle',
  imports: [MatIconButton, MatIcon, TranslocoPipe],
  template: `
    <button
      matIconButton
      type="button"
      [attr.aria-label]="(visible() ? 'auth.hidePassword' : 'auth.showPassword') | transloco"
      [attr.aria-pressed]="visible()"
      (click)="toggle()"
    >
      <mat-icon [svgIcon]="visible() ? 'eye-off' : 'eye'" />
    </button>
  `,
})
export class AppPasswordToggle {
  readonly field = input.required<HTMLInputElement>();

  protected readonly visible = signal(false);

  protected toggle(): void {
    this.visible.update((visible) => !visible);
    this.field().type = this.visible() ? 'text' : 'password';
  }
}
