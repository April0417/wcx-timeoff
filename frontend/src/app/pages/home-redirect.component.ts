import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-home-redirect',
  template: '<p class="status">Opening your workspace…</p>'
})
export class HomeRedirectComponent {
  constructor() {
    const auth = inject(AuthService);
    const router = inject(Router);
    void router.navigateByUrl(auth.homeRoute());
  }
}
