import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { readApiError } from '../core/api-error';
import { TimeOffRequest } from '../core/models';
import { formatDate, formatHours } from '../core/time-off-rules';

@Component({
  selector: 'app-approval-queue',
  imports: [RouterLink],
  template: `
    <section class="page">
      <header class="page-head">
        <div>
          <p class="eyebrow">Admin</p>
          <h1>Pending requests</h1>
          <p class="lede">Approve only after the API rechecks remaining available credits. Denial does not change taken credits.</p>
        </div>
        <button type="button" class="btn secondary" (click)="load()" [disabled]="loading()">Refresh</button>
      </header>

      @if (error()) {
        <p class="banner error" role="alert">{{ error() }}</p>
      }
      @if (notice()) {
        <p class="banner success" role="status">{{ notice() }}</p>
      }
      @if (loading()) {
        <p class="status" role="status">Loading pending requests…</p>
      } @else if (!error() && requests().length === 0) {
        <p class="status">There are no pending requests.</p>
      } @else if (!loading()) {
        <div class="table-wrap">
          <table>
            <caption class="sr-only">Pending time-off requests</caption>
            <thead>
              <tr>
                <th scope="col">Agent</th>
                <th scope="col">Dates</th>
                <th scope="col">Type</th>
                <th scope="col">Hours</th>
                <th scope="col">Reason</th>
                <th scope="col">Submitted</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (request of requests(); track request.id) {
                <tr>
                  <th scope="row">{{ request.agentName }}</th>
                  <td>{{ date(request.startDate) }} – {{ date(request.endDate) }}</td>
                  <td>{{ request.durationType === 'FullDay' ? 'Full day' : 'Partial day' }}</td>
                  <td>{{ hours(request.requestedHours) }}</td>
                  <td>{{ request.reason || '—' }}</td>
                  <td>{{ date(request.createdAt) }}</td>
                  <td><span class="badge" [attr.data-status]="request.status">{{ request.status }}</span></td>
                  <td class="actions">
                    <a class="btn secondary" [routerLink]="['/requests', request.id]">Details</a>
                    <button type="button" class="btn" (click)="confirmApprove(request)">Approve</button>
                    <button type="button" class="btn danger" (click)="confirmDeny(request)">Deny</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class ApprovalQueueComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly requests = signal<TimeOffRequest[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.pendingRequests().subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  confirmApprove(request: TimeOffRequest): void {
    if (!confirm(`Approve ${request.agentName}'s request for ${formatHours(request.requestedHours)} hours?`)) {
      return;
    }
    this.api.approve(request.id).subscribe({
      next: () => {
        this.notice.set(`Approved ${request.agentName}'s request. Taken credits increased once.`);
        this.load();
      },
      error: (err) => this.error.set(readApiError(err))
    });
  }

  confirmDeny(request: TimeOffRequest): void {
    if (!confirm(`Deny ${request.agentName}'s request? Taken credits will not change.`)) {
      return;
    }
    this.api.deny(request.id).subscribe({
      next: () => {
        this.notice.set(`Denied ${request.agentName}'s request.`);
        this.load();
      },
      error: (err) => this.error.set(readApiError(err))
    });
  }

  date(value: string): string {
    return formatDate(value);
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
