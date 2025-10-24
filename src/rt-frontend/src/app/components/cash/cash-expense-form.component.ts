import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CashExpenseService } from '../../services/cash-expense.service';

@Component({
  standalone: true,
  selector: 'app-cash-expense-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './cash-expense-form.component.html'
})
export class CashExpenseFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cashExpenseService = inject(CashExpenseService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  form = this.fb.nonNullable.group({
    expenseId: '',
    expenseDate: ['', Validators.required],
    description: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  isEdit = false;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.loadExpense(id);
    }
  }

  private loadExpense(id: string): void {
    this.loading = true;
    this.cashExpenseService.get(id).subscribe({
      next: (expense) => {
        this.form.patchValue(expense);
        this.loading = false;
      },
      error: () => {
        this.error = 'Tidak dapat memuat pengeluaran.';
        this.loading = false;
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    const payload = this.form.getRawValue();
    const request = this.isEdit && payload.expenseId
      ? this.cashExpenseService.update(payload.expenseId, payload)
      : this.cashExpenseService.create(payload);

    request.subscribe({
      next: () => this.router.navigate(['/cash-expenses']),
      error: () => {
        this.error = 'Gagal menyimpan pengeluaran.';
        this.loading = false;
      }
    });
  }
}
