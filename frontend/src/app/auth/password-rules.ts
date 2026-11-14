import { AbstractControl, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

export const passwordRule: ValidatorFn = Validators.pattern(/^(?=.*\d).{6,}$/);

export function matchesControl(otherControlName: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const other = control.parent?.get(otherControlName);
    return other && other.value !== control.value ? { mismatch: true } : null;
  };
}
