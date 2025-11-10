import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink, Router } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, NgIf],
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  token: string | null = this.route.snapshot.queryParamMap.get('token');
  success = false;
  error: string | null = null;

  form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  submit(): void {
    if (!this.token) {
      this.error = 'Token tidak ditemukan';
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { newPassword, confirmPassword } = this.form.getRawValue();
    if (newPassword !== confirmPassword) {
      this.error = 'Konfirmasi password tidak sama';
      return;
    }
    this.error = null;
    this.authService.resetPassword({ token: this.token, newPassword }).subscribe({
      next: () => {
        this.success = true;
        setTimeout(() => this.router.navigate(['/login']), 1200);
      },
      error: (err) => this.error = err?.error ?? 'Gagal reset password'
    });
  }
}

