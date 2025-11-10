import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CashExpenseService, CashExpensesQuery, PagedResult } from '../../services/cash-expense.service';
import { CashExpense } from '../../models/cash-expense.model';

@Component({
  standalone: true,
  selector: 'app-cash-expense-list',
  imports: [CommonModule, FormsModule, RouterLink, DatePipe, CurrencyPipe, NgClass],
  templateUrl: './cash-expense-list.component.html'
})
export class CashExpenseListComponent implements OnInit {
  private readonly cashExpenseService = inject(CashExpenseService);

  expenses: CashExpense[] = [];
  total = 0;
  loading = false;
  error: string | null = null;

  // pagination
  query: CashExpensesQuery = { page: 1, pageSize: 10 };
  pageSizes = [10, 20];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.cashExpenseService.list(this.query).subscribe({
      next: (data: PagedResult<CashExpense>) => {
        this.expenses = data.Items;
        this.total = data.Total;
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.error = 'Tidak dapat memuat pengeluaran kas.';
        this.loading = false;
      }
    });
  }

  changePageSize(size: number): void {
    this.query.pageSize = size;
    this.query.page = 1;
    this.load();
  }

  prevPage(): void {
    if ((this.query.page || 1) > 1) {
      this.query.page = (this.query.page || 1) - 1;
      this.load();
    }
  }

  nextPage(): void {
    const totalPages = this.totalPages;
    if ((this.query.page || 1) < totalPages) {
      this.query.page = (this.query.page || 1) + 1;
      this.load();
    }
  }

  get totalPages(): number {
    const size = this.query.pageSize || 10;
    return Math.max(1, Math.ceil(this.total / size));
  }
}
