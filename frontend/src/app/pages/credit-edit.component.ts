import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { readApiError } from '../core/api-error';
import { AgentOverview } from '../core/models';
import { formatHours } from '../core/time-off-rules';

@Component({
  selector: 'app-credit-edit',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="page narrow">
      <p class="eyebrow">Admin</p>
      <h1>Edit earned credits</h1>
      @if (loading()) {
        <p class="status" role="status">Loading balance…</p>
      } @else if (agent()) {
        <form class="card stack" [formGroup]="form" (ngSubmit)="save()">
          <p><strong>{{ agent()!.name }}</strong> <span class="muted">{{ agent()!.email }}</span></p>
          <p class="muted">Identifier {{ agent()!.id }}</p>
          <dl class="stats">
            <div><dt>Taken credits</dt><dd>{{ hours(agent()!.credits.takenCredits) }}</dd></div>
            <div><dt>Available now</dt><dd>{{ hours(agent()!.credits.availableBalance) }}</dd></div>
            <div><dt>Pending reserved</dt><dd>{{ hours(agent()!.credits.pendingReservedHours) }}</dd></div>
          </dl>

          <label for="earned">Earned credits (hours)</label>
          <input id="earned" type="number" min="0" step="0.25" formControlName="earnedCredits" />
          <p class="hint">Earned credits cannot be set below taken credits ({{ hours(agent()!.credits.takenCredits) }}).</p>

          @if (error()) {
            <p class="banner error" role="alert">{{ error() }}</p>
          }
          @if (saved()) {
            <p class="banner success" role="status">Earned credits updated. Available balance is now {{ hours(agent()!.credits.availableBalance) }}.</p>
          }

          <div class="actions">
            <button class="btn" type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save earned credits' }}</button>
            <a class="btn secondary" routerLink="/admin/agents">Back to overview</a>
          </div>
        </form>
      }
    </section>
  `
})
export class CreditEditComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  readonly agent = signal<AgentOverview | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({
    earnedCredits: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.error.set('Agent identifier is missing.');
      return;
    }

    this.api.getAgentCredits(id).subscribe({
      next: (agent) => {
        this.agent.set(agent);
        this.form.patchValue({ earnedCredits: agent.credits.earnedCredits });
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  save(): void {
    const current = this.agent();
    if (!current || this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Enter earned credits of zero or more.');
      return;
    }

    const earned = Number(this.form.getRawValue().earnedCredits);
    if (earned < current.credits.takenCredits) {
      this.error.set(`Earned credits cannot be reduced below taken credits (${formatHours(current.credits.takenCredits)}).`);
      return;
    }

    this.saving.set(true);
    this.saved.set(false);
    this.error.set('');
    this.api.updateEarnedCredits(current.id, earned).subscribe({
      next: (updated) => {
        this.agent.set(updated);
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(readApiError(err));
      }
    });
  }

  hours(value: number): string {
    return formatHours(value);
  }
}
