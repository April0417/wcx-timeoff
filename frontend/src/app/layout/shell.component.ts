import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <p class="brand">WCX Time Off</p>
        <p class="brand-sub">Credit and request desk</p>
        <nav aria-label="Primary">
          @if (auth.user()?.role === 'Admin') {
            <a routerLink="/admin/agents" routerLinkActive="active">Agent credits</a>
            <a routerLink="/admin/requests" routerLinkActive="active">Approval queue</a>
          } @else {
            <a routerLink="/requests/new" routerLinkActive="active">Request time off</a>
            <a routerLink="/requests/mine" routerLinkActive="active">My requests</a>
          }
        </nav>
      </aside>
      <div class="workspace">
        <header class="topbar">
          <div>
            <p class="eyebrow">Signed in</p>
            <p class="who">{{ auth.user()?.name }} <span class="role">{{ auth.user()?.role }}</span></p>
          </div>
          <button type="button" class="btn secondary" (click)="auth.logout()">Sign out</button>
        </header>
        <main class="content">
          <router-outlet />
        </main>
      </div>
    </div>
  `
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}
