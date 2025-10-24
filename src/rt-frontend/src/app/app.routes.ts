import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ResidentProfileComponent } from './components/residents/resident-profile.component';
import { ContributionListComponent } from './components/contributions/contribution-list.component';
import { ContributionFormComponent } from './components/contributions/contribution-form.component';
import { CashExpenseListComponent } from './components/cash/cash-expense-list.component';
import { CashExpenseFormComponent } from './components/cash/cash-expense-form.component';
import { UserManagementComponent } from './components/users/user-management.component';
import { AuthGuard, AuthChildGuard } from './guards/auth.guard';
import { RoleGuard } from './guards/role.guard';

export const appRoutes: Routes = [
  { path: 'login', component: LoginComponent },
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
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
