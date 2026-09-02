import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { ApiError } from '../../core/errors/api-error.model';
import {
  apiErrorMessage,
  applyFieldErrors,
  extractApiError,
} from '../../core/errors/api-error.util';
import { priorityLabel, statusLabel } from '../../shared/models/display';
import { PRIORITY_VALUES, Priority } from '../../shared/models/enums';
import { isGuidFormat } from '../../shared/models/guid';
import { TicketDetail } from '../models/ticket-detail.model';
import { TicketService } from '../services/ticket.service';

function nonWhitespace(control: AbstractControl): ValidationErrors | null {
  const v = typeof control.value === 'string' ? control.value.trim() : '';
  return v.length > 0 ? null : { whitespace: true };
}

@Component({
  selector: 'app-ticket-edit-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-edit.page.html',
  styleUrl: './ticket-form.page.scss',
})
export class TicketEditPage implements OnInit {
  private readonly tickets = inject(TicketService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly priorityValues = PRIORITY_VALUES;
  readonly priorityLabel = priorityLabel;
  readonly statusLabel = statusLabel;

  readonly ticket = signal<TicketDetail | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<ApiError | null>(null);
  readonly loadError = signal<ApiError | null>(null);

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200), nonWhitespace]],
    description: ['', [Validators.required, Validators.maxLength(4000), nonWhitespace]],
    customerName: ['', [Validators.required, Validators.maxLength(200), nonWhitespace]],
    customerEmail: [
      '',
      [Validators.required, Validators.maxLength(320), Validators.email, nonWhitespace],
    ],
    priority: ['Normal' as Priority, Validators.required],
  });

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id') ?? '';
          this.loading.set(true);
          this.loadError.set(null);
          this.ticket.set(null);
          if (!isGuidFormat(id)) {
            this.loading.set(false);
            this.loadError.set({
              status: 400,
              code: 'INVALID_ID',
              detail: 'The ticket id in the URL is not valid.',
            });
            return [];
          }
          return this.tickets.get(id);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detail) => {
          this.loading.set(false);
          this.ticket.set(detail);
          this.form.patchValue({
            title: detail.title,
            description: detail.description,
            customerName: detail.customerName,
            customerEmail: detail.customerEmail,
            priority: detail.priority,
          });
          if (!detail.canEditFields) {
            this.form.disable();
          } else {
            this.form.enable();
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.loadError.set(extractApiError(err));
        },
      });
  }

  submit(): void {
    const current = this.ticket();
    if (!current || !current.canEditFields || this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set(null);

    this.tickets
      .update(current.id, {
        title: raw.title.trim(),
        description: raw.description.trim(),
        customerName: raw.customerName.trim(),
        customerEmail: raw.customerEmail.trim(),
        priority: raw.priority,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.submitting.set(false);
          void this.router.navigate(['/tickets', updated.id]);
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

  errorMessage(err: ApiError | null = this.error()): string {
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
