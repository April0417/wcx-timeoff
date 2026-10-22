import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth.guard';
import { ShellComponent } from './layout/shell.component';
import { ApprovalQueueComponent } from './pages/approval-queue.component';
import { CreditEditComponent } from './pages/credit-edit.component';
import { DashboardComponent } from './pages/dashboard.component';
import { HomeRedirectComponent } from './pages/home-redirect.component';
import { LoginComponent } from './pages/login.component';
import { RequestDetailComponent } from './pages/request-detail.component';
import { RequestFormComponent } from './pages/request-form.component';
import { RequestHistoryComponent } from './pages/request-history.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      { path: 'home', component: HomeRedirectComponent },
      { path: 'admin/agents', component: DashboardComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/agents/:id/credits', component: CreditEditComponent, canActivate: [roleGuard('Admin')] },
      { path: 'admin/requests', component: ApprovalQueueComponent, canActivate: [roleGuard('Admin')] },
      { path: 'requests/new', component: RequestFormComponent, canActivate: [roleGuard('Agent')] },
      { path: 'requests/mine', component: RequestHistoryComponent, canActivate: [roleGuard('Agent')] },
      { path: 'requests/:id', component: RequestDetailComponent }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
