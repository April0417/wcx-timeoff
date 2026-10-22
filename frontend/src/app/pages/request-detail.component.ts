import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { readApiError } from '../core/api-error';
import { TimeOffRequest } from '../core/models';
import { formatDate, formatHours } from '../core/time-off-rules';

@Component({
  selector: 'app-request-detail',
  imports: [RouterLink],
  template: `
    <section class="page narrow">
      <p class="eyebrow">Request</p>
      <h1>Request details</h1>

      @if (loading()) {
        <p class="status" role="status">Loading request…</p>
      } @else if (error()) {
        <p class="banner error" role="alert">{{ error() }}</p>
      } @else if (request()) {
        <article class="card stack">
          <p><span class="badge" [attr.data-status]="request()!.status">{{ request()!.status }}</span></p>
          <dl class="details">
            <dt>Agent</dt><dd>{{ request()!.agentName }}</dd>
            <dt>Dates</dt><dd>{{ date(request()!.startDate) }} – {{ date(request()!.endDate) }}</dd>
            <dt>Duration</dt><dd>{{ request()!.durationType === 'FullDay' ? 'Full day' : 'Partial day' }}</dd>
            <dt>Hours</dt><dd>{{ hours(request()!.requestedHours) }}</dd>
            <dt>Reason</dt><dd>{{ request()!.reason || '—' }}</dd>
            <dt>Submitted</dt><dd>{{ date(request()!.createdAt) }}</dd>
            <dt>Reviewed</dt><dd>{{ request()!.reviewedAt ? date(request()!.reviewedAt!) : 'Not reviewed' }}</dd>
          </dl>

          @if (notice()) {
            <p class="banner success" role="status">{{ notice() }}</p>
          }
          @if (actionError()) {
            <p class="banner error" role="alert">{{ actionError() }}</p>
          }

          <div class="actions">
            @if (isAdmin() && request()!.status === 'Pending') {
              <button type="button" class="btn" (click)="approve()">Approve</button>
              <button type="button" class="btn danger" (click)="deny()">Deny</button>
            }
            <a class="btn secondary" [routerLink]="backLink()">Back</a>
          </div>
        </article>
      }
    </section>
  `
})
export class RequestDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly request = signal<TimeOffRequest | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly actionError = signal('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.error.set('Request identifier is missing.');
      return;
    }

    this.api.getRequest(id).subscribe({
      next: (request) => {
        this.request.set(request);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  isAdmin(): boolean {
    return this.auth.user()?.role === 'Admin';
  }

  backLink(): string {
    return this.isAdmin() ? '/admin/requests' : '/requests/mine';
  }

  approve(): void {
    const current = this.request();
    if (!current || !confirm('Approve this pending request? Taken credits will increase once.')) {
      return;
    }
    this.api.approve(current.id).subscribe({
      next: (updated) => {
        this.request.set(updated);
        this.notice.set('Request approved.');
      },
      error: (err) => this.actionError.set(readApiError(err))
    });
  }

  deny(): void {
    const current = this.request();
    if (!current || !confirm('Deny this pending request? Taken credits will not change.')) {
      return;
    }
    this.api.deny(current.id).subscribe({
      next: (updated) => {
        this.request.set(updated);
        this.notice.set('Request denied.');
      },
      error: (err) => this.actionError.set(readApiError(err))
    });
  }

  date(value: string): string {
    return formatDate(value);
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
