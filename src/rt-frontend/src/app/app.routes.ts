import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ResidentProfileComponent } from './components/residents/resident-profile.component';
import { ContributionListComponent } from './components/contributions/contribution-list.component';
import { ContributionFormComponent } from './components/contributions/contribution-form.component';
import { CashExpenseListComponent } from './components/cash/cash-expense-list.component';
import { CashExpenseFormComponent } from './components/cash/cash-expense-form.component';
import { CashMonitoringComponent } from './components/cash/cash-monitoring.component';
import { UserManagementComponent } from './components/users/user-management.component';
import { ResidentsListComponent } from './components/residents/residents-list.component';
import { ResidentDetailComponent } from './components/residents/resident-detail.component';
import { ForgotPasswordComponent } from './components/login/forgot-password.component';
import { ResetPasswordComponent } from './components/login/reset-password.component';
import { AuthGuard, AuthChildGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';

export const appRoutes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  {
    path: '',
    canActivate: [AuthGuard],
    canActivateChild: [AuthChildGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'profile', component: ResidentProfileComponent },
      { path: 'contributions', component: ContributionListComponent },
      { path: 'contributions/new', component: ContributionFormComponent },
      { path: 'contributions/:id/edit', component: ContributionFormComponent },
      {
        path: 'cash-expenses',
        component: CashExpenseListComponent,
        canActivate: [RoleGuard],
        data: { roles: ['ADMIN'] }
      },
      {
        path: 'cash-monitoring',
        component: CashMonitoringComponent,
        canActivate: [RoleGuard],
        data: { roles: ['ADMIN'] }
      },
      {
        path: 'cash-expenses/new',
        component: CashExpenseFormComponent,
        canActivate: [RoleGuard],
        data: { roles: ['ADMIN'] }
      },
      {
        path: 'cash-expenses/:id/edit',
        component: CashExpenseFormComponent,
        canActivate: [RoleGuard],
        data: { roles: ['ADMIN'] }
      },
      {
        path: 'users',
        component: UserManagementComponent,
        canActivate: [RoleGuard],
        data: { roles: ['ADMIN'] }
      },
      { path: 'residents', component: ResidentsListComponent },
      { path: 'residents/:id', component: ResidentDetailComponent, canActivate: [RoleGuard], data: { roles: ['ADMIN'] } }
    ]
  },
  { path: '**', redirectTo: '' }
];
