import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { readApiError } from '../core/api-error';
import { AgentOverview, DurationType, TimeOffRequest } from '../core/models';
import { formatHours, resolveRequestedHours, validateRequest } from '../core/time-off-rules';

@Component({
  selector: 'app-request-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="page narrow">
      <p class="eyebrow">Agent</p>
      <h1>Request time off</h1>
      <p class="lede">A full day is 8 hours for each calendar day in the inclusive range. A partial day is one date and the hours you enter.</p>

      @if (loading()) {
        <p class="status" role="status">Loading your balance…</p>
      } @else if (balance()) {
        <dl class="stats">
          <div><dt>Available</dt><dd>{{ hours(balance()!.credits.availableBalance) }}</dd></div>
          <div><dt>Pending reserved</dt><dd>{{ hours(balance()!.credits.pendingReservedHours) }}</dd></div>
          <div><dt>Requestable</dt><dd>{{ hours(balance()!.credits.requestableBalance) }}</dd></div>
        </dl>

        <form class="card stack" [formGroup]="form" (ngSubmit)="submit()">
          <div class="split">
            <div>
              <label for="startDate">Start date</label>
              <input id="startDate" type="date" formControlName="startDate" />
            </div>
            <div>
              <label for="endDate">End date</label>
              <input id="endDate" type="date" formControlName="endDate" />
            </div>
          </div>

          <fieldset>
            <legend>Duration type</legend>
            <label class="choice"><input type="radio" formControlName="durationType" value="FullDay" /> Full day</label>
            <label class="choice"><input type="radio" formControlName="durationType" value="PartialDay" /> Partial day</label>
          </fieldset>

          @if (form.controls.durationType.value === 'PartialDay') {
            <label for="requestedHours">Requested hours</label>
            <input id="requestedHours" type="number" min="0.25" max="8" step="0.25" formControlName="requestedHours" />
          } @else {
            <p class="hint">This full-day request uses {{ hours(previewHours()) }} hours.</p>
          }

          <label for="reason">Reason (optional)</label>
          <textarea id="reason" rows="3" maxlength="500" formControlName="reason"></textarea>

          @if (error()) {
            <p class="banner error" role="alert">{{ error() }}</p>
          }

          <div class="actions">
            <button class="btn" type="submit" [disabled]="submitting()">{{ submitting() ? 'Submitting…' : 'Submit request' }}</button>
            <a class="btn secondary" routerLink="/requests/mine">View history</a>
          </div>
        </form>
      } @else if (error()) {
        <p class="banner error" role="alert">{{ error() }}</p>
      }
    </section>
  `
})
export class RequestFormComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly balance = signal<AgentOverview | null>(null);
  readonly existing = signal<TimeOffRequest[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    durationType: this.fb.nonNullable.control<DurationType>('FullDay', Validators.required),
    requestedHours: [4, [Validators.min(0.25), Validators.max(8)]],
    reason: ['', Validators.maxLength(500)]
  });

  ngOnInit(): void {
    this.api.myCredits().subscribe({
      next: (balance) => {
        this.balance.set(balance);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(readApiError(err));
      }
    });
    this.api.myRequests().subscribe({
      next: (requests) => this.existing.set(requests),
      error: () => this.existing.set([])
    });
  }

  previewHours(): number {
    const value = this.form.getRawValue();
    if (!value.startDate || !value.endDate || value.endDate < value.startDate) {
      return 0;
    }
    return resolveRequestedHours(value.durationType, value.startDate, value.endDate, value.requestedHours);
  }

  submit(): void {
    const current = this.balance();
    const value = this.form.getRawValue();
    if (!current) {
      return;
    }

    const message = validateRequest({
      startDate: value.startDate,
      endDate: value.endDate,
      durationType: value.durationType,
      requestedHours: value.durationType === 'PartialDay' ? Number(value.requestedHours) : null,
      requestableBalance: current.credits.requestableBalance,
      existing: this.existing()
    });
    if (message) {
      this.error.set(message);
      return;
    }

    this.submitting.set(true);
    this.error.set('');
    this.api.createRequest({
      startDate: value.startDate,
      endDate: value.endDate,
      durationType: value.durationType,
      requestedHours: value.durationType === 'PartialDay' ? Number(value.requestedHours) : null,
      reason: value.reason.trim() || null
    }).subscribe({
      next: (created) => {
        this.submitting.set(false);
        void this.router.navigate(['/requests', created.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
