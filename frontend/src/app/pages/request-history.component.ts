import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { readApiError } from '../core/api-error';
import { TimeOffRequest } from '../core/models';
import { formatDate, formatHours } from '../core/time-off-rules';

@Component({
  selector: 'app-request-history',
  imports: [RouterLink],
  template: `
    <section class="page">
      <header class="page-head">
        <div>
          <p class="eyebrow">Agent</p>
          <h1>My requests</h1>
        </div>
        <a class="btn" routerLink="/requests/new">New request</a>
      </header>

      @if (error()) {
        <p class="banner error" role="alert">{{ error() }}</p>
      }
      @if (loading()) {
        <p class="status" role="status">Loading requests…</p>
      } @else if (!error() && requests().length === 0) {
        <p class="status">You have not submitted any time-off requests.</p>
      } @else if (!loading()) {
        <div class="table-wrap">
          <table>
            <caption class="sr-only">Your time-off requests</caption>
            <thead>
              <tr>
                <th scope="col">Dates</th>
                <th scope="col">Type</th>
                <th scope="col">Hours</th>
                <th scope="col">Status</th>
                <th scope="col">Submitted</th>
              </tr>
            </thead>
            <tbody>
              @for (request of requests(); track request.id) {
                <tr>
                  <th scope="row"><a [routerLink]="['/requests', request.id]">{{ date(request.startDate) }} – {{ date(request.endDate) }}</a></th>
                  <td>{{ request.durationType === 'FullDay' ? 'Full day' : 'Partial day' }}</td>
                  <td>{{ hours(request.requestedHours) }}</td>
                  <td><span class="badge" [attr.data-status]="request.status">{{ request.status }}</span></td>
                  <td>{{ date(request.createdAt) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class RequestHistoryComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly requests = signal<TimeOffRequest[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.myRequests().subscribe({
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

  date(value: string): string {
    return formatDate(value);
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
