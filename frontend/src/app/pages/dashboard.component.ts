import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { readApiError } from '../core/api-error';
import { AgentOverview } from '../core/models';
import { formatHours } from '../core/time-off-rules';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <section class="page">
      <header class="page-head">
        <div>
          <p class="eyebrow">Admin</p>
          <h1>Agent credits</h1>
          <p class="lede">Available balance is earned credits minus taken credits. The API calculates it.</p>
        </div>
        <button type="button" class="btn secondary" (click)="load()" [disabled]="loading()">Refresh</button>
      </header>

      @if (error()) {
        <p class="banner error" role="alert">{{ error() }}</p>
      }
      @if (loading()) {
        <p class="status" role="status">Loading agents…</p>
      } @else if (!error() && agents().length === 0) {
        <p class="status">No agents are available.</p>
      } @else if (!loading()) {
        <div class="table-wrap">
          <table>
            <caption class="sr-only">Agent credit balances</caption>
            <thead>
              <tr>
                <th scope="col">Agent</th>
                <th scope="col">Earned</th>
                <th scope="col">Taken</th>
                <th scope="col">Available</th>
                <th scope="col">Requestable</th>
                <th scope="col"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (agent of agents(); track agent.id) {
                <tr>
                  <th scope="row">
                    {{ agent.name }}
                    <span class="muted">{{ agent.email }}</span>
                    <span class="muted">ID {{ agent.id }}</span>
                  </th>
                  <td>{{ hours(agent.credits.earnedCredits) }}</td>
                  <td>{{ hours(agent.credits.takenCredits) }}</td>
                  <td>{{ hours(agent.credits.availableBalance) }}</td>
                  <td>{{ hours(agent.credits.requestableBalance) }}</td>
                  <td><a class="btn secondary" [routerLink]="['/admin/agents', agent.id, 'credits']">Edit earned</a></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly agents = signal<AgentOverview[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.listAgents().subscribe({
      next: (agents) => {
        this.agents.set(agents);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
