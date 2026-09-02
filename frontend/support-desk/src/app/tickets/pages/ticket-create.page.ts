import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiError } from '../../core/errors/api-error.model';
import {
  apiErrorMessage,
  applyFieldErrors,
  extractApiError,
} from '../../core/errors/api-error.util';
import { priorityLabel } from '../../shared/models/display';
import { PRIORITY_VALUES, Priority } from '../../shared/models/enums';
import { TicketService } from '../services/ticket.service';

function nonWhitespace(control: AbstractControl): ValidationErrors | null {
  const v = typeof control.value === 'string' ? control.value.trim() : '';
  return v.length > 0 ? null : { whitespace: true };
}

@Component({
  selector: 'app-ticket-create-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-create.page.html',
  styleUrl: './ticket-form.page.scss',
})
export class TicketCreatePage {
  private readonly tickets = inject(TicketService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly priorityValues = PRIORITY_VALUES;
  /** Display order for segmented control (urgency descending). */
  readonly priorityOptions: readonly Priority[] = ['Critical', 'High', 'Normal', 'Low'];
  readonly priorityLabel = priorityLabel;
  readonly submitting = signal(false);
  readonly error = signal<ApiError | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200), nonWhitespace]],
    description: ['', [Validators.required, Validators.maxLength(4000), nonWhitespace]],
    customerName: ['', [Validators.required, Validators.maxLength(200), nonWhitespace]],
    customerEmail: [
      '',
      [Validators.required, Validators.maxLength(320), Validators.email, nonWhitespace],
    ],
    priority: ['' as '' | Priority, Validators.required],
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    if (!raw.priority) {
      this.form.controls.priority.setErrors({ required: true });
      this.form.controls.priority.markAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.tickets
      .create({
        title: raw.title.trim(),
        description: raw.description.trim(),
        customerName: raw.customerName.trim(),
        customerEmail: raw.customerEmail.trim(),
        priority: raw.priority,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ticket) => {
          this.submitting.set(false);
          void this.router.navigate(['/tickets', ticket.id]);
        },
        error: (err) => {
          this.submitting.set(false);
          const apiErr = extractApiError(err);
          this.error.set(apiErr);
          applyFieldErrors((name, message) => {
            const control = this.form.get(name);
            if (control) {
              control.setErrors({ ...(control.errors ?? {}), server: message });
            }
          }, apiErr.fieldErrors);
        },
      });
  }

  selectPriority(priority: Priority): void {
    this.form.controls.priority.setValue(priority);
    this.form.controls.priority.markAsTouched();
  }

  errorMessage(): string {
    const err = this.error();
    return err ? apiErrorMessage(err) : '';
  }

  fieldError(name: string): string | null {
    const control = this.form.get(name);
    if (!control || !control.touched || !control.errors) {
      return null;
    }
    if (control.errors['server']) {
      return String(control.errors['server']);
    }
    if (control.errors['required'] || control.errors['whitespace']) {
      return 'This field is required.';
    }
    if (control.errors['email']) {
      return 'Enter a valid email address.';
    }
    if (control.errors['maxlength']) {
      return 'Value is too long.';
    }
    return 'Invalid value.';
  }
}
