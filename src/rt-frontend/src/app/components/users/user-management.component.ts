import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UserService, UserSummary } from '../../services/user.service';

@Component({
  standalone: true,
  selector: 'app-user-management',
  imports: [CommonModule, ReactiveFormsModule, NgClass],
  templateUrl: './user-management.component.html'
})
export class UserManagementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  users: UserSummary[] = [];
  loading = false;
  error: string | null = null;
  success: string | null = null;

  form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['WARGA' as const, Validators.required]
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.userService.list().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat data user.';
        this.loading = false;
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.userService.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.success = 'User berhasil dibuat.';
        this.error = null;
        this.form.reset({ username: '', password: '', role: 'WARGA' });
        this.loadUsers();
      },
      error: () => {
        this.error = 'Gagal membuat user.';
      }
    });
  }

  toggleActive(user: UserSummary): void {
    this.userService.setActive(user.userId, !user.isActive).subscribe({
      next: () => this.loadUsers()
    });
  }
}
