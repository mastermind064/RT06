import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CashExpenseService } from '../../services/cash-expense.service';
import { CashExpense } from '../../models/cash-expense.model';

@Component({
  standalone: true,
  selector: 'app-cash-expense-list',
  imports: [CommonModule, RouterLink, DatePipe, CurrencyPipe, NgClass],
  templateUrl: './cash-expense-list.component.html'
})
export class CashExpenseListComponent implements OnInit {
  private readonly cashExpenseService = inject(CashExpenseService);

  expenses: CashExpense[] = [];
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.cashExpenseService.list().subscribe({
      next: (data) => {
        this.expenses = data;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat pengeluaran kas.';
        this.loading = false;
      }
    });
  }
}
