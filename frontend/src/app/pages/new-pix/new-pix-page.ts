import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormGroupDirective,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TuiDay } from '@taiga-ui/cdk';
import {
  TuiButton,
  TuiDateFormat,
  TuiError,
  TuiInput,
  TuiLink,
  TuiLoader,
  TuiNotification,
  TuiNumberFormat,
} from '@taiga-ui/core';
import {
  TuiButtonLoading,
  TuiChip,
  TuiInputDate,
  TuiInputNumber,
  TuiTextarea,
} from '@taiga-ui/kit';
import { firstValueFrom } from 'rxjs';
import { ignoreApiError } from '../../api/api-error';
import { AppRoutes } from '../../auth/routes';
import { sumAmounts } from '../../finance/finance.calc';
import { currentMonth, today } from '../../finance/finance.dates';
import { CATEGORIES, Category, NewPix, Transaction } from '../../finance/finance.models';
import { FinanceService } from '../../finance/finance.service';

type SaveMode = 'leave' | 'stay';

const DESCRIPTION_MIN_LENGTH = 2;
const DESCRIPTION_MAX_LENGTH = 100;
const NOTE_MAX_LENGTH = 200;

function notAfter(limit: TuiDay): ValidatorFn {
  return (control: AbstractControl<TuiDay | null>): ValidationErrors | null =>
    control.value?.dayAfter(limit) ? { future: true } : null;
}

@Component({
  selector: 'app-new-pix-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    TranslocoDirective,
    TuiButton,
    TuiButtonLoading,
    TuiChip,
    TuiDateFormat,
    TuiError,
    TuiInput,
    TuiInputDate,
    TuiInputNumber,
    TuiLink,
    TuiLoader,
    TuiNotification,
    TuiNumberFormat,
    TuiTextarea,
  ],
  templateUrl: './new-pix-page.html',
  styleUrl: './new-pix-page.scss',
})
export class NewPixPage {
  private readonly finance = inject(FinanceService);
  private readonly router = inject(Router);

  protected readonly routes = AppRoutes;
  protected readonly categories = CATEGORIES;
  protected readonly today = TuiDay.jsonParse(today());
  protected readonly monthStart = `${currentMonth()}-01`;
  protected readonly noteMaxLength = NOTE_MAX_LENGTH;
  protected readonly moneyFormat = { decimalSeparator: ',', thousandSeparator: '.' };
  protected readonly dateFormat = { mode: 'dd/mm/yyyy', separator: '/' } as const;

  protected readonly submitting = signal<SaveMode | null>(null);
  protected readonly lastSaved = signal<Transaction | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    date: [this.today as TuiDay | null, [Validators.required, notAfter(this.today)]],
    description: [
      '',
      [
        Validators.required,
        Validators.minLength(DESCRIPTION_MIN_LENGTH),
        Validators.maxLength(DESCRIPTION_MAX_LENGTH),
      ],
    ],
    category: [null as Category | null, [Validators.required]],
    note: ['', [Validators.maxLength(NOTE_MAX_LENGTH)]],
  });

  protected readonly monthTransactions = rxResource({
    stream: () => this.finance.transactions(currentMonth()),
  });

  protected readonly monthPix = computed(() =>
    (this.monthTransactions.value() ?? []).filter((transaction) => transaction.origin === 'pix'),
  );

  protected readonly monthPixTotal = computed(() => sumAmounts(this.monthPix()));

  protected async saveAndLeave(): Promise<void> {
    const created = await this.save('leave');
    if (created) {
      await this.router.navigateByUrl(AppRoutes.transactions);
    }
  }

  protected async saveAndStay(formDirective: FormGroupDirective): Promise<void> {
    const created = await this.save('stay');
    if (created) {
      formDirective.resetForm();
      this.lastSaved.set(created);
    }
  }

  private async save(mode: SaveMode): Promise<Transaction | null> {
    const pix = this.readForm();
    if (!pix) {
      this.form.markAllAsTouched();
      return null;
    }
    this.submitting.set(mode);
    try {
      const created = await firstValueFrom(this.finance.createPix(pix));
      this.monthTransactions.reload();
      return created;
    } catch (error) {
      ignoreApiError(error);
      return null;
    } finally {
      this.submitting.set(null);
    }
  }

  private readForm(): NewPix | null {
    const { amount, date, description, category, note } = this.form.getRawValue();
    if (this.form.invalid || amount === null || date === null || category === null) {
      return null;
    }
    return {
      amount,
      date: date.toJSON(),
      description: description.trim(),
      category,
      note: note.trim() || null,
    };
  }
}
