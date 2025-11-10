import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, NgIf],
  templateUrl: './forgot-password.component.html'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  message: string | null = null;
  error: string | null = null;
  devResetPath?: string;

  form = this.fb.nonNullable.group({
    usernameOrEmail: ['', Validators.required],
    blok: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.message = null;
    this.error = null;
    this.devResetPath = undefined;
    this.authService.forgotPassword(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.message = res.Message || 'Jika email terdaftar, tautan reset telah dikirim.';
        this.devResetPath = res.DevResetPath;
      },
      error: (err) => {
        this.error = err?.error ?? 'Gagal memproses permintaan.';
      }
    });
  }
}
