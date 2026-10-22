import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { readApiError } from '../core/api-error';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-page">
      <form class="card login-card" [formGroup]="form" (ngSubmit)="submit()">
        <p class="eyebrow">WCX Time Off</p>
        <h1>Sign in</h1>
        <p class="lede">Use a development account to review credits or request time off.</p>

        @if (error()) {
          <p class="banner error" role="alert">{{ error() }}</p>
        }

        <label for="email">Email</label>
        <input id="email" type="email" formControlName="email" autocomplete="username" />

        <label for="password">Password</label>
        <input id="password" type="password" formControlName="password" autocomplete="current-password" />

        <button class="btn" type="submit" [disabled]="submitting()">
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>

        <p class="hint">Development-only accounts are listed in the README. Do not reuse these passwords outside this demo.</p>
      </form>
    </main>
  `
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly error = signal('');
  readonly submitting = signal(false);
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  submit(): void {
    this.error.set('');
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Enter a valid email and password.');
      return;
    }

    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email.trim(), password).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl(this.auth.homeRoute());
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(readApiError(err));
      }
    });
  }
}
